module App

open Elmish
open Elmish.React
open Feliz
open Feliz.Router
open Shared.Api
open Routing

type Model = {
  Page: Page
  User: User option
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

type private RouteSource =
  | BrowserUrl
  | InternalNavigation

let private pageForUser (userId: int) (page: Page) =
  match page with
  | UserLinksPage _ -> UserLinksPage userId
  | UserProfilePage _ -> UserProfilePage userId
  | UserPreviewPage _ -> UserPreviewPage userId
  | _ -> page

let private setPage (source: RouteSource) (requestedPage: Page) (model: Model) : Model * Cmd<Msg> =
  let needsAuth =
    match requestedPage with
    | UserLinksPage _
    | UserProfilePage _
    | UserPreviewPage _ -> true
    | _ -> false

  let actualPage =
    if needsAuth then
      match model.User with
      | Some user -> pageForUser user.Id requestedPage
      | None -> LoginPage
    else
      requestedPage

  let (newModel, cmd) =
    match actualPage with
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

  {newModel with Page = actualPage}, Cmd.batch [cmd; navCmd]

let init () : Model * Cmd<Msg> =
  let model = {
    Page = LoginPage
    User = None
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
    let withUser = {model with User = Some user}
    match model.Page with
    | LoginPage
    | RegisterPage -> setPage InternalNavigation (UserLinksPage user.Id) withUser
    | UserLinksPage _
    | UserProfilePage _
    | UserPreviewPage _ -> setPage InternalNavigation model.Page withUser
    | PublicPreviewPage _
    | NotFoundPage -> withUser, Cmd.none

  | CurrentUserLoaded (Error _) ->
    let withNoUser = {model with User = None}
    match model.Page with
    | UserLinksPage _
    | UserProfilePage _
    | UserPreviewPage _ -> setPage InternalNavigation model.Page withNoUser
    | _ -> withNoUser, Cmd.none

  | LoginMsg loginMsg ->
    let (newLoginModel, newLoginCmd) = LoginPage.update loginMsg model.Login
    match loginMsg with
    | LoginPage.LoginResult (Ok user) ->
      let (newModel, newCmd) =
        setPage InternalNavigation (UserLinksPage user.Id) {model with User = Some user; Login = newLoginModel}
      newModel, Cmd.batch [newCmd; Cmd.map LoginMsg newLoginCmd]
    | _ -> {model with Login = newLoginModel}, Cmd.map LoginMsg newLoginCmd

  | RegisterMsg registerMsg ->
    let (newRegisterModel, newRegisterCmd) =
      RegisterPage.update registerMsg model.Register
    match registerMsg with
    | RegisterPage.RegisterResult (Ok user) ->
      let (newModel, newCmd) =
        setPage InternalNavigation (UserLinksPage user.Id) {model with User = Some user; Register = newRegisterModel}
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

  | LoggedIn user -> {model with User = Some user}, Cmd.none

  | LoggedOut ->
    let (newModel, navCmd) =
      setPage InternalNavigation LoginPage {model with User = None}
    newModel, navCmd

  | RequestLogout ->
    model,
    Auth.logout (
      function
      | Ok () -> LoggedOut
      | Error _ -> LoggedOut
    )

let view (model: Model) (dispatch: Msg -> unit) =
  let header =
    match model.Page, model.User with
    | UserLinksPage _, Some user
    | UserProfilePage _, Some user -> Some (Header.view user.Id (NavigateTo >> dispatch))
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
      ]
    ]
  ]

let start () =
  Program.mkProgram init update view
  |> Program.withReactBatched "root"
  |> Program.run

do start ()
