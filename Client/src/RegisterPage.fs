module RegisterPage

open System
open Elmish
open Feliz
open Shared.Api
open Routing

// Model
type Model = {
  Email: string
  Password: string
  ConfirmPassword: string
  IsLoading: bool
  FormError: string option
  EmailError: string option
  PasswordError: string option
  ConfirmPasswordError: string option
}

let init () : Model = {
  Email = ""
  Password = ""
  ConfirmPassword = ""
  IsLoading = false
  FormError = None
  EmailError = None
  PasswordError = None
  ConfirmPasswordError = None
}

// Msg
type Msg =
  | SetEmail of string
  | SetPassword of string
  | SetConfirmPassword of string
  | AttemptRegister
  | RegisterResult of Result<User, AppError>

// Update
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg with
  | SetEmail email -> {model with Email = email; FormError = None; EmailError = None}, Cmd.none
  | SetPassword pass -> {model with Password = pass; FormError = None; PasswordError = None}, Cmd.none
  | SetConfirmPassword pass ->
    {model with ConfirmPassword = pass; FormError = None; ConfirmPasswordError = None}, Cmd.none
  | AttemptRegister ->
    let normalizedEmailResult = Validation.validateRequiredEmail model.Email
    let emailError =
      match normalizedEmailResult with
      | Result.Ok _ -> None
      | Result.Error err -> Some err
    let passwordError =
      if
        String.IsNullOrWhiteSpace model.Password
        || model.Password.Length < 8
      then
        Some "Please check again"
      else
        None
    let confirmPasswordError =
      if String.IsNullOrWhiteSpace model.ConfirmPassword then
        Some "Please check again"
      elif model.Password <> model.ConfirmPassword then
        Some "Please check again"
      else
        None

    match emailError, passwordError, confirmPasswordError with
    | None, None, None ->
      {
        model with
            IsLoading = true
            FormError = None
            EmailError = None
            PasswordError = None
            ConfirmPasswordError = None
      },
      let normalizedEmail =
        match normalizedEmailResult with
        | Result.Ok email -> email
        | Result.Error _ -> model.Email
      Auth.register {Email = normalizedEmail; Password = model.Password} RegisterResult
    | _ ->
      {
        model with
            IsLoading = false
            FormError = None
            EmailError = emailError
            PasswordError = passwordError
            ConfirmPasswordError = confirmPasswordError
      },
      Cmd.none
  | RegisterResult (Ok user) ->
    // This message should be bubbled up to the main update function
    // to navigate to the todos page and store the user.
    {model with IsLoading = false}, Cmd.none
  | RegisterResult (Error err) ->
    let message = Auth.appErrorToMessage err
    let emailError =
      match err with
      | Conflict -> Some message
      | _ -> None
    let formError =
      match err with
      | Conflict -> None
      | _ -> Some message
    {model with IsLoading = false; FormError = formError; EmailError = emailError}, Cmd.none

type private FieldProps = {
  Id: string
  Label: string
  Value: string
  Placeholder: string
  InputType: string
  LeftIcon: Ui.Icon.Name
  AutoFocus: bool
  Error: string option
  HelpText: string option
  OnChange: string -> unit
}

let private fieldView (p: FieldProps) =
  Html.div [
    prop.className "flex flex-col gap-2 w-full"
    prop.children [
      Html.label [
        prop.htmlFor p.Id
        prop.className "text-preset-4 text-gray-900"
        prop.text p.Label
      ]
      Ui.TextField.viewWithoutLabel {
        Id = p.Id
        Label = p.Label
        Value = p.Value
        Placeholder = p.Placeholder
        HelpText = p.HelpText
        Error = p.Error
        AutoFocus = p.AutoFocus
        InputType = p.InputType
        LeftIcon = Some p.LeftIcon
        OnChange = p.OnChange
      }
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
                    prop.text "Create account"
                  ]
                  Html.p [
                    prop.className "text-preset-3-regular text-gray-500"
                    prop.text "Let's get you started sharing your links!"
                  ]
                ]
              ]
              Html.form [
                prop.className "flex flex-col gap-6"
                prop.onSubmit (fun ev ->
                  ev.preventDefault ()
                  dispatch AttemptRegister
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
                    HelpText = None
                    OnChange = (SetEmail >> dispatch)
                  }
                  fieldView {
                    Id = "password"
                    Label = "Create password"
                    Value = model.Password
                    Placeholder = "At least 8 characters"
                    AutoFocus = false
                    InputType = "password"
                    LeftIcon = Ui.Icon.Name.Password
                    Error = model.PasswordError
                    HelpText = None
                    OnChange = (SetPassword >> dispatch)
                  }
                  fieldView {
                    Id = "confirm-password"
                    Label = "Confirm password"
                    Value = model.ConfirmPassword
                    Placeholder = "At least 8 characters"
                    AutoFocus = false
                    InputType = "password"
                    LeftIcon = Ui.Icon.Name.Password
                    Error = model.ConfirmPasswordError
                    HelpText = Some "Password must contain at least 8 characters"
                    OnChange = (SetConfirmPassword >> dispatch)
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
                    text =
                      if model.IsLoading then
                        "Creating account..."
                      else
                        "Create new account"
                  |}
                  Html.p [
                    prop.className "text-center text-preset-3-regular text-gray-500"
                    prop.children [
                      Html.span [
                        prop.className "block md:inline"
                        prop.text "Already have an account? "
                      ]
                      Html.a [
                        prop.className "block md:inline text-purple-600 hover:text-purple-500"
                        prop.href (href LoginPage)
                        prop.text "Login"
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
