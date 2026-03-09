module App

open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open Feliz
open Shared.SharedModels
open Shared.Api
open Routing // Import the new module

// Main model - The Page DU is now removed from here.
type Model = {
  Page: Page // This now refers to Routing.Page
  User: User option
  Login: LoginPage.Model
  Register: RegisterPage.Model
  Links: LinkEditorPage.Model
  Profile: ProfileEditorPage.Model
  Preview: PreviewPage.Model
}

// Main messages
type Msg =
  | LoginMsg of LoginPage.Msg
  | RegisterMsg of RegisterPage.Msg
  | LinksMsg of LinkEditorPage.Msg
  | ProfileMsg of ProfileEditorPage.Msg
  | PreviewMsg of PreviewPage.Msg
  | LoggedIn of User
  | LoggedOut
  | RequestLogout

// URL parsing and routing - The old urlParser is removed from here.

// Router: define a proper UrlParser for Page and feed it to parseHash
let pageParser: Parser<Page -> Page, _> =
  oneOf [
    map LinksPage top
    map LinksPage (s "links")
    map ProfilePage (s "profile")
    map (fun slug -> PreviewPage slug) (s "preview" </> str)
    map LoginPage (s "login")
    map RegisterPage (s "register")
  ]

let urlUpdate (result: Page option) (model: Model) : Model * Cmd<Msg> =
  match result with
  | Some requestedPage ->
    let needsAuth =
      match requestedPage with
      | LoginPage
      | RegisterPage
      | PreviewPage _ -> false
      | _ -> true

    let isAuthenticated = model.User.IsSome

    // If user needs auth for the requested page but isn't authenticated,
    // force the page to be LoginPage. Otherwise, use the requested page.
    let actualPage =
      if needsAuth && not isAuthenticated then
        LoginPage
      else
        requestedPage

    // When navigating to a page, reset its specific state to clear old form data.
    let (newModel, cmd) =
      match actualPage with
      | LoginPage -> {model with Login = LoginPage.init ()}, Cmd.none
      | RegisterPage -> {model with Register = RegisterPage.init ()}, Cmd.none
      | LinksPage ->
        let (linksModel, linksCmd) = LinkEditorPage.init ()
        {model with Links = linksModel}, Cmd.map LinksMsg linksCmd
      | ProfilePage ->
        let (profileModel, profileCmd) = ProfileEditorPage.init ()
        {model with Profile = profileModel}, Cmd.map ProfileMsg profileCmd
      | PreviewPage slug ->
        let (previewModel, previewCmd) = PreviewPage.init slug
        {model with Preview = previewModel}, Cmd.map PreviewMsg previewCmd

    {newModel with Page = actualPage}, cmd

  | None ->
    // This case should ideally not be hit if the parser is exhaustive.
    // We can navigate to a default page or show a "Not Found" message.
    {model with Page = LinksPage}, Navigation.newUrl "#/links"

// Init
let init (result: Option<Page>) : Model * Cmd<Msg> =
  let page = result |> Option.defaultValue LinksPage
  let (linksModel, linksCmd) = LinkEditorPage.init ()
  let (profileModel, profileCmd) = ProfileEditorPage.init ()

  let model = {
    Page = page
    User = None
    Login = LoginPage.init ()
    Register = RegisterPage.init ()
    Links = linksModel
    Profile = profileModel
    Preview = PreviewPage.init "" |> fst
  }

  let (newModel, urlUpdateCmd) = urlUpdate (Some page) model

  newModel,
  Cmd.batch [
    Cmd.map LinksMsg linksCmd
    Cmd.map ProfileMsg profileCmd
    urlUpdateCmd
  ]

// Update
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | LoginMsg loginMsg ->
    let (newLoginModel, newLoginCmd) = LoginPage.update loginMsg model.Login
    // Check if login was successful
    match loginMsg with
    | LoginPage.LoginResult (Ok user) ->
      let (newModel, newCmd) =
        urlUpdate (Some LinksPage) {model with User = Some user; Login = newLoginModel}
      newModel, Cmd.batch [newCmd; Cmd.map LoginMsg newLoginCmd]
    | _ -> {model with Login = newLoginModel}, Cmd.map LoginMsg newLoginCmd

  | RegisterMsg registerMsg ->
    let (newRegisterModel, newRegisterCmd) =
      RegisterPage.update registerMsg model.Register
    // Check if registration was successful
    match registerMsg with
    | RegisterPage.RegisterResult (Ok user) ->
      let (newModel, newCmd) =
        urlUpdate (Some LinksPage) {model with User = Some user; Register = newRegisterModel}
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

  | LoggedIn user -> {model with User = Some user}, Cmd.none

  | LoggedOut ->
    // When logged out, clear the user and navigate to the login page
    let (newModel, navCmd) = urlUpdate (Some LoginPage) {model with User = None}
    newModel, navCmd

  | RequestLogout ->
    model,
    Auth.logout (
      function
      | Ok () -> LoggedOut
      | Error _ -> LoggedOut
    )

// View
let view (model: Model) (dispatch: Msg -> unit) =
  let headerSlug: string option =
    match model.Profile.State with
    | ProfileEditorPage.Loaded form when not (System.String.IsNullOrWhiteSpace form.ProfileSlug) ->
      Some form.ProfileSlug
    | _ ->
      match model.Preview.State with
      | PreviewPage.Loaded (profile, _) when not (System.String.IsNullOrWhiteSpace profile.ProfileSlug) ->
        Some profile.ProfileSlug
      | _ -> None
  Html.div [
    Header.view headerSlug
    match model.Page with
    | LinksPage -> LinkEditorPage.view model.Links (LinksMsg >> dispatch)
    | ProfilePage -> ProfileEditorPage.view model.Profile (ProfileMsg >> dispatch)
    | PreviewPage _ -> PreviewPage.view model.Preview (PreviewMsg >> dispatch)
    | LoginPage -> LoginPage.view model.Login (LoginMsg >> dispatch)
    | RegisterPage -> RegisterPage.view model.Register (RegisterMsg >> dispatch)
  ]

// Program
let start () =
  Program.mkProgram init update view
  |> Program.toNavigable (parseHash pageParser) urlUpdate
  |> Program.withReactBatched "root"
  |> Program.run

do start ()
