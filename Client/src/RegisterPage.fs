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
    let emailError =
      if String.IsNullOrWhiteSpace model.Email then
        Some "Can't be empty"
      else
        None
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
      Auth.register {Email = model.Email; Password = model.Password} RegisterResult
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
      | Conflict -> Some "Please check again"
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
  let hasError = p.Error |> Option.isSome
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
        prop.className "text-preset-4 text-gray-900"
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
      | None ->
        match p.HelpText with
        | Some help ->
          Html.p [
            prop.className "text-preset-4 text-gray-500"
            prop.text help
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
