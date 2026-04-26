module App

open Elmish
open Elmish.React
open Feliz
open Feliz.Router
open Shared.Api
open Routing

type AuthState =
  | Checking
  | Authenticated
  | Unauthenticated

type Model = {
  Page: Page
  User: User option
  AuthState: AuthState
  PendingPrivateRoute: Page option
  GlobalToast: Ui.Toast.Props option
  Login: LoginPage.Model
  Register: RegisterPage.Model
  Links: LinkEditorPage.Model
  Profile: ProfileEditorPage.Model
  Preview: PreviewPage.Model
  PublicPreview: PublicPreviewPage.Model
}

type Msg =
  | UrlChanged of string list
  | NavigateTo of Page
  | LoginMsg of LoginPage.Msg
  | RegisterMsg of RegisterPage.Msg
  | LinksMsg of LinkEditorPage.Msg
  | ProfileMsg of ProfileEditorPage.Msg
  | PreviewMsg of PreviewPage.Msg
  | PublicPreviewMsg of PublicPreviewPage.Msg
  | LoadCurrentUser
  | CurrentUserLoaded of Result<User, AppError>
  | LoggedIn of User
  | LoggedOut
  | RequestLogout
  | ClearGlobalToast

type private RouteSource =
  | BrowserUrl
  | InternalNavigation

let private requiresAuthentication (page: Page) =
  match page with
  | UserLinksPage _
  | UserProfilePage _
  | UserPreviewPage _ -> true
  | _ -> false

let private routeUserId (page: Page) =
  match page with
  | UserLinksPage userId
  | UserProfilePage userId
  | UserPreviewPage userId -> Some userId
  | _ -> None

let private isAdmin (user: User) = user.Role = UserRole.Admin

let private clearGlobalToastCmd =
  Cmd.OfAsync.perform (fun () -> async {do! Async.Sleep 2500}) () (fun _ -> ClearGlobalToast)

let private errorToast (message: string) : Ui.Toast.Props = {
  Message = message
  Variant = Ui.Toast.Variant.Error
  Icon = None
  Uppercase = false
}

let private initPage (page: Page) (model: Model) : Model * Cmd<Msg> =
  match page with
  | LoginPage -> {model with Login = LoginPage.init ()}, Cmd.none
  | RegisterPage -> {model with Register = RegisterPage.init ()}, Cmd.none
  | UserLinksPage userId ->
    let (linksModel, linksCmd) = LinkEditorPage.init userId
    {model with Links = linksModel}, Cmd.map LinksMsg linksCmd
  | UserProfilePage userId ->
    let (profileModel, profileCmd) = ProfileEditorPage.init userId
    {model with Profile = profileModel}, Cmd.map ProfileMsg profileCmd
  | UserPreviewPage userId ->
    let previewPublicId = model.User |> Option.map (fun user -> user.PublicId)
    let (previewModel, previewCmd) = PreviewPage.init userId previewPublicId
    {model with Preview = previewModel}, Cmd.map PreviewMsg previewCmd
  | PublicPreviewPage publicId ->
    let (publicModel, publicCmd) = PublicPreviewPage.init publicId
    {model with PublicPreview = publicModel}, Cmd.map PublicPreviewMsg publicCmd
  | NotFoundPage -> model, Cmd.none

let private setPage (source: RouteSource) (requestedPage: Page) (model: Model) : Model * Cmd<Msg> =
  let needsAuth = requiresAuthentication requestedPage

  if needsAuth && model.AuthState = Checking then
    {model with PendingPrivateRoute = Some requestedPage}, Cmd.none
  else
    let (actualPage, globalToast) =
      if needsAuth then
        match model.User, routeUserId requestedPage with
        | None, _ -> LoginPage, Some (errorToast "Please log in to access this page.")
        | Some user, Some requestedUserId when requestedUserId = user.Id -> requestedPage, None
        | Some user, Some _ when isAdmin user -> requestedPage, None
        | Some user, Some _ ->
          let fallbackPage =
            match model.Page with
            | LoginPage
            | RegisterPage -> UserProfilePage user.Id
            | UserLinksPage id when id <> user.Id -> UserProfilePage user.Id
            | UserProfilePage id when id <> user.Id -> UserProfilePage user.Id
            | UserPreviewPage id when id <> user.Id -> UserProfilePage user.Id
            | _ -> model.Page
          fallbackPage, Some (errorToast "You are not authorized to view this page.")
        | Some _, None -> requestedPage, None
      else
        requestedPage, None

    let (newModel, cmd) = initPage actualPage model

    let navCmd =
      match source with
      | BrowserUrl ->
        if actualPage <> requestedPage then
          Routing.navigateCmd actualPage
        else
          Cmd.none
      | InternalNavigation ->
        if model.Page <> actualPage then
          Routing.navigateCmd actualPage
        else
          Cmd.none

    let toastCmd =
      match globalToast with
      | Some _ -> clearGlobalToastCmd
      | None -> Cmd.none

    {
      newModel with
          Page = actualPage
          PendingPrivateRoute = None
          GlobalToast =
            match globalToast with
            | Some toast -> Some toast
            | None -> newModel.GlobalToast
    },
    Cmd.batch [cmd; navCmd; toastCmd]

let init () : Model * Cmd<Msg> =
  let model = {
    Page = LoginPage
    User = None
    AuthState = Checking
    PendingPrivateRoute = None
    GlobalToast = None
    Login = LoginPage.init ()
    Register = RegisterPage.init ()
    Links = LinkEditorPage.init 0 |> fst
    Profile = ProfileEditorPage.init 0 |> fst
    Preview = PreviewPage.init 0 None |> fst
    PublicPreview =
      PublicPreviewPage.init "00000000-0000-0000-0000-000000000000"
      |> fst
  }

  let (newModel, routeCmd) = setPage BrowserUrl (tryParseCurrentUrl ()) model

  newModel, Cmd.batch [routeCmd; Cmd.ofMsg LoadCurrentUser]

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | NavigateTo page -> model, Routing.navigateCmd page

  | UrlChanged segments -> setPage BrowserUrl (tryParsePath segments) model

  | LoadCurrentUser -> model, Auth.getCurrentUser CurrentUserLoaded

  | CurrentUserLoaded (Ok user) ->
    let withUser = {model with User = Some user; AuthState = Authenticated}
    match withUser.PendingPrivateRoute with
    | Some pendingRoute -> setPage BrowserUrl pendingRoute {withUser with PendingPrivateRoute = None}
    | None ->
      match model.Page with
      | LoginPage
      | RegisterPage -> setPage InternalNavigation (UserLinksPage user.Id) withUser
      | UserLinksPage _
      | UserProfilePage _
      | UserPreviewPage _ -> setPage InternalNavigation model.Page withUser
      | PublicPreviewPage _
      | NotFoundPage -> {withUser with PendingPrivateRoute = None}, Cmd.none

  | CurrentUserLoaded (Error _) ->
    let withNoUser = {model with User = None; AuthState = Unauthenticated}
    match withNoUser.PendingPrivateRoute with
    | Some pendingRoute -> setPage BrowserUrl pendingRoute {withNoUser with PendingPrivateRoute = None}
    | None ->
      match model.Page with
      | UserLinksPage _
      | UserProfilePage _
      | UserPreviewPage _ -> setPage InternalNavigation model.Page withNoUser
      | _ -> {withNoUser with PendingPrivateRoute = None}, Cmd.none

  | LoginMsg loginMsg ->
    let (newLoginModel, newLoginCmd) = LoginPage.update loginMsg model.Login
    match loginMsg with
    | LoginPage.LoginResult (Ok user) ->
      let (newModel, newCmd) =
        setPage InternalNavigation (UserLinksPage user.Id) {
          model with
              User = Some user
              AuthState = Authenticated
              Login = newLoginModel
        }
      newModel, Cmd.batch [newCmd; Cmd.map LoginMsg newLoginCmd]
    | _ -> {model with Login = newLoginModel}, Cmd.map LoginMsg newLoginCmd

  | RegisterMsg registerMsg ->
    let (newRegisterModel, newRegisterCmd) =
      RegisterPage.update registerMsg model.Register
    match registerMsg with
    | RegisterPage.RegisterResult (Ok user) ->
      let (newModel, newCmd) =
        setPage InternalNavigation (UserLinksPage user.Id) {
          model with
              User = Some user
              AuthState = Authenticated
              Register = newRegisterModel
        }
      newModel,
      Cmd.batch [
        newCmd
        Cmd.map RegisterMsg newRegisterCmd
      ]
    | _ -> {model with Register = newRegisterModel}, Cmd.map RegisterMsg newRegisterCmd

  | LinksMsg msg ->
    let (linksModel, linksCmd) = LinkEditorPage.update msg model.Links
    {model with Links = linksModel}, Cmd.map LinksMsg linksCmd

  | ProfileMsg msg ->
    let (profileModel, profileCmd) = ProfileEditorPage.update msg model.Profile
    {model with Profile = profileModel}, Cmd.map ProfileMsg profileCmd

  | PreviewMsg msg ->
    let (previewModel, previewCmd) = PreviewPage.update msg model.Preview
    {model with Preview = previewModel}, Cmd.map PreviewMsg previewCmd

  | PublicPreviewMsg msg ->
    let (publicPreviewModel, publicPreviewCmd) =
      PublicPreviewPage.update msg model.PublicPreview
    {model with PublicPreview = publicPreviewModel}, Cmd.map PublicPreviewMsg publicPreviewCmd

  | LoggedIn user -> {model with User = Some user; AuthState = Authenticated}, Cmd.none

  | LoggedOut ->
    let (newModel, navCmd) =
      setPage InternalNavigation LoginPage {model with User = None; AuthState = Unauthenticated}
    newModel, navCmd

  | RequestLogout ->
    model,
    Auth.logout (
      function
      | Ok () -> LoggedOut
      | Error _ -> LoggedOut
    )

  | ClearGlobalToast -> {model with GlobalToast = None}, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) =
  let headerUserId =
    match model.Page with
    | UserLinksPage userId
    | UserProfilePage userId
    | UserPreviewPage userId -> Some userId
    | _ -> None

  let header =
    match model.Page, model.User, headerUserId with
    | UserLinksPage _, Some _, Some userId
    | UserProfilePage _, Some _, Some userId -> Some (Header.view userId (NavigateTo >> dispatch))
    | _ -> None

  React.router [
    router.onUrlChanged (UrlChanged >> dispatch)
    router.children [
      Html.div [
        match header with
        | Some h -> h
        | None -> Html.none

        match model.Page with
        | LoginPage -> LoginPage.view model.Login (LoginMsg >> dispatch)
        | RegisterPage -> RegisterPage.view model.Register (RegisterMsg >> dispatch)
        | UserLinksPage _ -> LinkEditorPage.view model.Links (LinksMsg >> dispatch)
        | UserProfilePage _ -> ProfileEditorPage.view model.Profile (ProfileMsg >> dispatch)
        | UserPreviewPage _ ->
          PreviewPage.view
            model.Preview
            (PreviewMsg >> dispatch)
            (fun () -> dispatch (NavigateTo (UserLinksPage model.Preview.UserId)))
        | PublicPreviewPage _ -> PublicPreviewPage.view model.PublicPreview (PublicPreviewMsg >> dispatch)
        | NotFoundPage ->
          let fallbackPage =
            model.User
            |> Option.map (fun user -> UserLinksPage user.Id)
            |> Option.defaultValue LoginPage
          NotFoundPage.view fallbackPage

        match model.GlobalToast with
        | Some toast -> Ui.Toast.view toast
        | None -> Html.none

        Html.footer [
          prop.className
            "w-full border-t border-gray-200 bg-white/80 px-6 py-5 text-center text-sm font-semibold text-gray-700"
          prop.children [
            Html.a [
              prop.href "https://github.com/wedgehov/link-sharing-app"
              prop.target "_blank"
              prop.rel "noopener noreferrer"
              prop.className
                "inline-flex items-center justify-center gap-2 rounded-lg px-3 py-2 transition-colors hover:bg-gray-100 hover:text-gray-900"
              prop.children [
                Ui.Icon.view Ui.Icon.Name.GitHub "GitHub" (Some "h-4 w-4")
                Html.span "View source on GitHub"
              ]
            ]
          ]
        ]
      ]
    ]
  ]

let start () =
  Program.mkProgram init update view
  |> Program.withReactBatched "root"
  |> Program.run

do start ()
