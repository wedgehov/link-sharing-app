module LoginPage

open Elmish
open Feliz
open Shared.SharedModels

// Model
type Model = {
  Email: string
  Password: string
  IsLoading: bool
  Error: string option
}

let init () : Model = {
  Email = ""
  Password = ""
  IsLoading = false
  Error = None
}

// Msg
type Msg =
  | SetEmail of string
  | SetPassword of string
  | AttemptLogin
  | LoginResult of Result<Auth.User, exn>

// Update
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | SetEmail email -> {model with Email = email}, Cmd.none
  | SetPassword pass -> {model with Password = pass}, Cmd.none
  | AttemptLogin ->
    {model with IsLoading = true; Error = None}, Auth.login {email = model.Email; password = model.Password} LoginResult
  | LoginResult (Ok user) ->
    // This message should be bubbled up to the main update function
    // to navigate to the todos page and store the user.
    {model with IsLoading = false}, Cmd.none
  | LoginResult (Error ex) -> {model with IsLoading = false; Error = Some ex.Message}, Cmd.none

// View
let view (model: Model) (dispatch: Msg -> unit) =
  Html.div [
    prop.className "min-h-screen bg-gray-50"
    prop.children [
      Html.main [
        prop.className "px-6 md:px-0 md:max-w-xl mx-auto py-16"
        prop.children [
          Html.div [
            prop.className "flex justify-center items-center mb-8"
            prop.children [
              Html.h1 [
                prop.className "text-preset-1"
                prop.text "Log in"
              ]
            ]
          ]
          Html.div [
            prop.className "bg-white rounded-[var(--radius-lg)] shadow-[var(--shadow-md)] p-4"
            prop.children [
              Html.form [
                prop.className "flex flex-col gap-6"
                prop.onSubmit (fun ev ->
                  ev.preventDefault ()
                  dispatch AttemptLogin
                )
                prop.children [
                  Ui.TextField.view {
                    Id = "email"
                    Label = "Email Address"
                    Value = model.Email
                    Placeholder = "Enter your email"
                    HelpText = None
                    Error = None
                    AutoFocus = true
                    InputType = "email"
                    LeftIcon = Some Ui.Icon.Name.Email
                    OnChange = (SetEmail >> dispatch)
                  }
                  Ui.TextField.view {
                    Id = "password"
                    Label = "Password"
                    Value = model.Password
                    Placeholder = "Enter your password"
                    HelpText = None
                    Error = None
                    AutoFocus = false
                    InputType = "password"
                    LeftIcon = Some Ui.Icon.Name.Password
                    OnChange = (SetPassword >> dispatch)
                  }
                  match model.Error with
                  | Some error ->
                    Html.p [
                      prop.className "text-sm text-red-600"
                      prop.text error
                    ]
                  | None -> Html.none

                  Ui.Button.view {|
                    variant = Ui.Button.Variant.Primary
                    size = Ui.Button.Size.Md
                    active = false
                    disabled = model.IsLoading
                    onClick = (fun () -> ())
                    text = if model.IsLoading then "Logging in..." else "Login"
                  |}
                  Html.div [
                    prop.className "text-center"
                    prop.children [
                      Html.a [
                        prop.className "text-blue-500 hover:text-blue-600 text-sm"
                        prop.href "#/register"
                        prop.text "Don't have an account? Create one"
                      ]
                    ]
                  ]
                ]
              ]
            ]
          ]
        ]
      ]
    ]
  ]
