module App

open System
open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open Feliz
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

let private isValidGuid (value: string) =
  match Guid.TryParse value with
  | true, _ -> true
  | _ -> false

let private pageForUser (userId: int) (page: Page) =
  match page with
  | UserLinksPage _ -> UserLinksPage userId
  | UserProfilePage _ -> UserProfilePage userId
  | UserPreviewPage _ -> UserPreviewPage userId
  | _ -> page

let pageParser: Parser<Page -> Page, _> =
  oneOf [
    map LoginPage top
    map LoginPage (s "login")
    map RegisterPage (s "register")
    map (fun userId -> UserLinksPage userId) (s "user" </> i32 </> s "links")
    map (fun userId -> UserProfilePage userId) (s "user" </> i32 </> s "profile")
    map (fun userId -> UserPreviewPage userId) (s "user" </> i32 </> s "preview")
    map (fun publicId -> PublicPreviewPage publicId) str
  ]

let urlUpdate (result: Page option) (model: Model) : Model * Cmd<Msg> =
  match result with
  | Some requestedPage ->
    let requestedPage =
      match requestedPage with
      | PublicPreviewPage publicId when not (isValidGuid publicId) -> LoginPage
      | _ -> requestedPage

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
      | UserLinksPage _ ->
        let (linksModel, linksCmd) = LinkEditorPage.init ()
        {model with Links = linksModel}, Cmd.map LinksMsg linksCmd
      | UserProfilePage _ ->
        let (profileModel, profileCmd) = ProfileEditorPage.init ()
        {model with Profile = profileModel}, Cmd.map ProfileMsg profileCmd
      | UserPreviewPage _ ->
        let (previewModel, previewCmd) = PreviewPage.init ()
        {model with Preview = previewModel}, Cmd.map PreviewMsg previewCmd
      | PublicPreviewPage publicId ->
        let (publicModel, publicCmd) = PublicPreviewPage.init publicId
        {model with PublicPreview = publicModel}, Cmd.map PublicPreviewMsg publicCmd

    {newModel with Page = actualPage}, cmd

  | None -> {model with Page = LoginPage}, Navigation.newUrl "#/login"

let init (result: Option<Page>) : Model * Cmd<Msg> =
  let page = result |> Option.defaultValue LoginPage
  let model = {
    Page = page
    User = None
    Login = LoginPage.init ()
    Register = RegisterPage.init ()
    Links = LinkEditorPage.init () |> fst
    Profile = ProfileEditorPage.init () |> fst
    Preview = PreviewPage.init () |> fst
    PublicPreview =
      PublicPreviewPage.init "00000000-0000-0000-0000-000000000000"
      |> fst
  }

  let (newModel, urlUpdateCmd) = urlUpdate (Some page) model

  newModel,
  Cmd.batch [
    urlUpdateCmd
    Cmd.ofMsg LoadCurrentUser
  ]

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | LoadCurrentUser -> model, Auth.getCurrentUser CurrentUserLoaded

  | CurrentUserLoaded (Ok user) ->
    let withUser = {model with User = Some user}
    match model.Page with
    | LoginPage
    | RegisterPage -> urlUpdate (Some (UserLinksPage user.Id)) withUser
    | UserLinksPage _
    | UserProfilePage _
    | UserPreviewPage _ -> urlUpdate (Some model.Page) withUser
    | PublicPreviewPage _ -> withUser, Cmd.none

  | CurrentUserLoaded (Error _) ->
    let withNoUser = {model with User = None}
    match model.Page with
    | UserLinksPage _
    | UserProfilePage _
    | UserPreviewPage _ -> urlUpdate (Some model.Page) withNoUser
    | _ -> withNoUser, Cmd.none

  | LoginMsg loginMsg ->
    let (newLoginModel, newLoginCmd) = LoginPage.update loginMsg model.Login
    match loginMsg with
    | LoginPage.LoginResult (Ok user) ->
      let (newModel, newCmd) =
        urlUpdate (Some (UserLinksPage user.Id)) {model with User = Some user; Login = newLoginModel}
      newModel, Cmd.batch [newCmd; Cmd.map LoginMsg newLoginCmd]
    | _ -> {model with Login = newLoginModel}, Cmd.map LoginMsg newLoginCmd

  | RegisterMsg registerMsg ->
    let (newRegisterModel, newRegisterCmd) =
      RegisterPage.update registerMsg model.Register
    match registerMsg with
    | RegisterPage.RegisterResult (Ok user) ->
      let (newModel, newCmd) =
        urlUpdate (Some (UserLinksPage user.Id)) {model with User = Some user; Register = newRegisterModel}
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
    let (newModel, navCmd) = urlUpdate (Some LoginPage) {model with User = None}
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
    | UserProfilePage _, Some user
    | UserPreviewPage _, Some user -> Some (Header.view user.Id)
    | _ -> None

  Html.div [
    match header with
    | Some h -> h
    | None -> Html.none

    match model.Page with
    | LoginPage -> LoginPage.view model.Login (LoginMsg >> dispatch)
    | RegisterPage -> RegisterPage.view model.Register (RegisterMsg >> dispatch)
    | UserLinksPage _ -> LinkEditorPage.view model.Links (LinksMsg >> dispatch)
    | UserProfilePage _ -> ProfileEditorPage.view model.Profile (ProfileMsg >> dispatch)
    | UserPreviewPage _ -> PreviewPage.view model.Preview (PreviewMsg >> dispatch)
    | PublicPreviewPage _ -> PublicPreviewPage.view model.PublicPreview (PublicPreviewMsg >> dispatch)
  ]

let start () =
  Program.mkProgram init update view
  |> Program.toNavigable (parseHash pageParser) urlUpdate
  |> Program.withReactBatched "root"
  |> Program.run

do start ()
