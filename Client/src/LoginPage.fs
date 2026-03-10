module LoginPage

open System
open Elmish
open Feliz
open Shared.Api
open Routing

// Model
type Model = {
  Email: string
  Password: string
  IsLoading: bool
  FormError: string option
  EmailError: string option
  PasswordError: string option
}

let init () : Model = {
  Email = ""
  Password = ""
  IsLoading = false
  FormError = None
  EmailError = None
  PasswordError = None
}

// Msg
type Msg =
  | SetEmail of string
  | SetPassword of string
  | AttemptLogin
  | LoginResult of Result<User, AppError>

// Update
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | SetEmail email -> {model with Email = email; FormError = None; EmailError = None}, Cmd.none
  | SetPassword pass -> {model with Password = pass; FormError = None; PasswordError = None}, Cmd.none
  | AttemptLogin ->
    let emailError =
      if String.IsNullOrWhiteSpace model.Email then
        Some "Can't be empty"
      else
        None
    let passwordError =
      if String.IsNullOrWhiteSpace model.Password then
        Some "Please check again"
      else
        None

    match emailError, passwordError with
    | None, None ->
      {
        model with
            IsLoading = true
            FormError = None
            EmailError = None
            PasswordError = None
      },
      Auth.login {Email = model.Email; Password = model.Password} LoginResult
    | _ ->
      {
        model with
            IsLoading = false
            FormError = None
            EmailError = emailError
            PasswordError = passwordError
      },
      Cmd.none
  | LoginResult (Ok user) ->
    // This message should be bubbled up to the main update function
    // to navigate to the todos page and store the user.
    {model with IsLoading = false}, Cmd.none
  | LoginResult (Error err) ->
    let message = Auth.appErrorToMessage err
    {model with IsLoading = false; FormError = Some message}, Cmd.none

type private FieldProps = {
  Id: string
  Label: string
  Value: string
  Placeholder: string
  InputType: string
  LeftIcon: Ui.Icon.Name
  AutoFocus: bool
  Error: string option
  ErrorLabel: bool
  OnChange: string -> unit
}

let private fieldView (p: FieldProps) =
  let hasError = p.Error |> Option.isSome
  let labelClass =
    if hasError && p.ErrorLabel then
      "text-preset-4 text-red-500"
    else
      "text-preset-4 text-gray-900"
  let borderClass =
    if hasError then
      "border-red-500"
    else
      "border-gray-200 focus:border-purple-600 focus:shadow-[0_0_32px_rgba(99,60,255,0.25)]"
  let rightPadClass = if hasError then "md:pr-36" else ""

  Html.div [
    prop.className "flex flex-col gap-2 w-full"
    prop.children [
      Html.label [
        prop.htmlFor p.Id
        prop.className labelClass
        prop.text p.Label
      ]
      Html.div [
        prop.className "relative"
        prop.children [
          Ui.Icon.view p.LeftIcon p.Label (Some "absolute left-4 top-1/2 -translate-y-1/2 size-4")
          Html.input [
            prop.id p.Id
            prop.type' p.InputType
            prop.value p.Value
            prop.placeholder p.Placeholder
            prop.autoFocus p.AutoFocus
            prop.className (
              String.concat " " [
                "w-full h-14 rounded-[var(--radius-md)] bg-white border px-4 pl-11 text-preset-3-regular outline-none transition-all"
                borderClass
                rightPadClass
              ]
            )
            prop.onChange p.OnChange
          ]
          match p.Error with
          | Some e ->
            Html.span [
              prop.className
                "hidden md:block absolute right-4 top-1/2 -translate-y-1/2 text-preset-4 text-red-500 text-right"
              prop.text e
            ]
          | None -> Html.none
        ]
      ]
      match p.Error with
      | Some e ->
        Html.p [
          prop.className "block md:hidden text-preset-4 text-red-500 text-right"
          prop.text e
        ]
      | None -> Html.none
    ]
  ]

// View
let view (model: Model) (dispatch: Msg -> unit) =
  Html.main [
    prop.className "min-h-screen bg-white px-8 py-8 md:bg-gray-50 md:px-0 md:py-0 md:grid md:place-items-center"
    prop.children [
      Html.div [
        prop.className "mx-auto w-full max-w-[311px] flex flex-col gap-16 md:w-[476px] md:max-w-none md:gap-12"
        prop.children [
          Html.div [
            prop.className "h-10 w-[182.5px]"
            prop.children [
              Html.img [
                prop.className "h-full w-full"
                prop.src "/images/logo-devlinks-large.svg"
                prop.alt "devlinks"
              ]
            ]
          ]
          Html.div [
            prop.className "w-full flex flex-col gap-10 md:bg-white md:rounded-[var(--radius-lg)] md:p-10"
            prop.children [
              Html.div [
                prop.className "flex flex-col gap-2"
                prop.children [
                  Html.h1 [
                    prop.className "text-preset-2 md:text-preset-1 text-gray-900"
                    prop.text "Login"
                  ]
                  Html.p [
                    prop.className "text-preset-3-regular text-gray-500"
                    prop.text "Add your details below to get back into the app"
                  ]
                ]
              ]
              Html.form [
                prop.className "flex flex-col gap-6"
                prop.onSubmit (fun ev ->
                  ev.preventDefault ()
                  dispatch AttemptLogin
                )
                prop.children [
                  fieldView {
                    Id = "email"
                    Label = "Email address"
                    Value = model.Email
                    Placeholder = "e.g. alex@email.com"
                    AutoFocus = true
                    InputType = "email"
                    LeftIcon = Ui.Icon.Name.Email
                    Error = model.EmailError
                    ErrorLabel = true
                    OnChange = (SetEmail >> dispatch)
                  }
                  fieldView {
                    Id = "password"
                    Label = "Password"
                    Value = model.Password
                    Placeholder = "Enter your password"
                    AutoFocus = false
                    InputType = "password"
                    LeftIcon = Ui.Icon.Name.Password
                    Error = model.PasswordError
                    ErrorLabel = true
                    OnChange = (SetPassword >> dispatch)
                  }
                  match model.FormError with
                  | Some error ->
                    Html.p [
                      prop.className "text-preset-4 text-red-500 text-right"
                      prop.text error
                    ]
                  | None -> Html.none

                  Ui.Button.view {|
                    variant = Ui.Button.Variant.Primary
                    size = Ui.Button.Size.MdFull
                    active = false
                    disabled = model.IsLoading
                    onClick = (fun () -> ())
                    text = if model.IsLoading then "Logging in..." else "Login"
                  |}
                  Html.p [
                    prop.className "text-center text-preset-3-regular text-gray-500"
                    prop.children [
                      Html.span [
                        prop.className "block md:inline"
                        prop.text "Don't have an account? "
                      ]
                      Html.a [
                        prop.className "block md:inline text-purple-600 hover:text-purple-500"
                        prop.href (href RegisterPage)
                        prop.text "Create account"
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
