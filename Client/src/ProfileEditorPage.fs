module ProfileEditorPage

open System
open Browser.Types
open Fable.Core
open Feliz
open Elmish
open Shared.SharedModels
open Shared.Api
open ClientShared

type ProfileForm = {
  FirstName: string
  FirstNameError: string option
  LastName: string
  LastNameError: string option
  DisplayEmail: string
  DisplayEmailError: string option
  AvatarUrl: string
  IsSaving: bool
  Error: string option
  Saved: bool
}

type State =
  | Loading
  | Loaded of ProfileForm
  | Error of string

type Model = {UserId: int; State: State; PreviewLinks: Link list}

type Msg =
  | LoadProfile
  | ProfileLoaded of Result<UserProfile, AppError>
  | LoadLinks
  | LinksLoaded of Result<Link list, AppError>
  | SelectAvatarFile of File
  | AvatarFileLoaded of string
  | AvatarFileLoadFailed
  | SetFirstName of string
  | SetLastName of string
  | SetDisplayEmail of string
  | Save
  | SaveResult of Result<unit, AppError>

[<Emit("new Promise((resolve, reject) => { const reader = new FileReader(); reader.onload = () => resolve(reader.result); reader.onerror = () => reject(reader.error || new Error('Could not read file')); reader.readAsDataURL($0); })")>]
let private readFileAsDataUrl (_file: File) : JS.Promise<string> = jsNative

let private toForm (p: UserProfile) : ProfileForm = {
  FirstName = p.FirstName
  FirstNameError = None
  LastName = p.LastName
  LastNameError = None
  DisplayEmail = defaultArg p.DisplayEmail ""
  DisplayEmailError = None
  AvatarUrl = defaultArg p.AvatarUrl ""
  IsSaving = false
  Error = None
  Saved = false
}

let private toDto (f: ProfileForm) : UserProfile = {
  FirstName = f.FirstName
  LastName = f.LastName
  DisplayEmail =
    if String.IsNullOrWhiteSpace f.DisplayEmail then
      None
    else
      Some f.DisplayEmail
  AvatarUrl =
    if String.IsNullOrWhiteSpace f.AvatarUrl then
      None
    else
      Some f.AvatarUrl
}

let init (userId: int) : Model * Cmd<Msg> =
  {UserId = userId; State = Loading; PreviewLinks = []},
  Cmd.batch [
    Cmd.ofMsg LoadProfile
    Cmd.ofMsg LoadLinks
  ]

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg, model.State with
  | LoadProfile, _ ->
    let load () =
      ApiClient.ProfileApi.GetProfile model.UserId
    {model with State = Loading}, Cmd.OfAsync.either load () ProfileLoaded (asUnexpected ProfileLoaded)

  | ProfileLoaded (Result.Ok profile), _ -> {model with State = Loaded (toForm profile)}, Cmd.none

  | ProfileLoaded (Result.Error err), _ -> {model with State = Error (appErrorToMessage err)}, Cmd.none

  | LoadLinks, _ ->
    let load () = ApiClient.LinkApi.GetLinks model.UserId
    model, Cmd.OfAsync.either load () LinksLoaded (asUnexpected LinksLoaded)

  | LinksLoaded (Result.Ok links), _ -> {model with PreviewLinks = links}, Cmd.none

  | LinksLoaded (Result.Error _err), _ -> model, Cmd.none

  | SelectAvatarFile file, Loaded form ->
    let loadingForm = {form with Error = None; Saved = false}
    {model with State = Loaded loadingForm},
    Cmd.OfPromise.either readFileAsDataUrl file AvatarFileLoaded (fun _ -> AvatarFileLoadFailed)

  | AvatarFileLoaded avatarDataUrl, Loaded form ->
    {model with State = Loaded {form with AvatarUrl = avatarDataUrl; Saved = false; Error = None}}, Cmd.none

  | AvatarFileLoadFailed, Loaded form ->
    {
      model with
          State = Loaded {form with Error = Some "Could not read image file. Please try another image."; Saved = false}
    },
    Cmd.none

  | SetFirstName v, Loaded form ->
    {
      model with
          State =
            Loaded {
              form with
                  FirstName = v
                  FirstNameError = None
                  Error = None
                  Saved = false
            }
    },
    Cmd.none
  | SetLastName v, Loaded form ->
    {
      model with
          State =
            Loaded {
              form with
                  LastName = v
                  LastNameError = None
                  Error = None
                  Saved = false
            }
    },
    Cmd.none
  | SetDisplayEmail v, Loaded form ->
    {
      model with
          State =
            Loaded {
              form with
                  DisplayEmail = v
                  DisplayEmailError = None
                  Error = None
                  Saved = false
            }
    },
    Cmd.none

  | Save, Loaded form ->
    let normalizedFirstName = form.FirstName.Trim ()
    let normalizedLastName = form.LastName.Trim ()
    let normalizedEmail = form.DisplayEmail.Trim ()

    let firstNameError =
      match Validation.validateRequiredText normalizedFirstName with
      | Result.Ok _ -> None
      | Result.Error err -> Some err

    let lastNameError =
      match Validation.validateRequiredText normalizedLastName with
      | Result.Ok _ -> None
      | Result.Error err -> Some err

    let emailError =
      match Validation.validateRequiredEmail normalizedEmail with
      | Result.Ok _ -> None
      | Result.Error err -> Some err

    match firstNameError, lastNameError, emailError with
    | Some _, _, _
    | _, Some _, _
    | _, _, Some _ ->
      let invalidForm = {
        form with
            FirstName = normalizedFirstName
            FirstNameError = firstNameError
            LastName = normalizedLastName
            LastNameError = lastNameError
            DisplayEmail = normalizedEmail
            DisplayEmailError = emailError
            Error = None
            Saved = false
      }
      {model with State = Loaded invalidForm}, Cmd.none
    | None, None, None ->
      let normalizedForm = {
        form with
            FirstName = normalizedFirstName
            FirstNameError = None
            LastName = normalizedLastName
            LastNameError = None
            DisplayEmail = normalizedEmail
            DisplayEmailError = None
      }
      let dto = toDto normalizedForm
      let save () =
        ApiClient.ProfileApi.SaveProfile model.UserId dto
      let saving = {normalizedForm with IsSaving = true; Error = None; Saved = false}
      {model with State = Loaded saving}, Cmd.OfAsync.either save () SaveResult (asUnexpected SaveResult)

  | SaveResult (Result.Ok ()), Loaded form ->
    {
      model with
          State =
            Loaded {
              form with
                  IsSaving = false
                  Saved = true
                  FirstNameError = None
                  LastNameError = None
                  DisplayEmailError = None
            }
    },
    Cmd.none

  | SaveResult (Result.Error err), Loaded form ->
    let nextForm =
      match err with
      | Conflict -> {
          form with
              IsSaving = false
              Saved = false
              Error = None
              FirstNameError = None
              LastNameError = None
              DisplayEmailError = Some (appErrorToMessage Conflict)
        }
      | _ -> {
          form with
              IsSaving = false
              Saved = false
              Error = Some (appErrorToMessage err)
              FirstNameError = None
              LastNameError = None
              DisplayEmailError = None
        }
    {model with State = Loaded nextForm}, Cmd.none

  | _, _ -> model, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) =
  match model.State with
  | Loading ->
    Html.p [
      prop.className "bg-gray-50 px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6 text-preset-3-regular text-gray-500"
      prop.text "Loading profile..."
    ]
  | Error msg ->
    Html.p [
      prop.className "bg-gray-50 px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6 text-preset-3-regular text-red-600"
      prop.text msg
    ]
  | Loaded form ->
    let avatarOpt =
      if String.IsNullOrWhiteSpace form.AvatarUrl then
        None
      else
        Some form.AvatarUrl

    let inputRow
      (label: string)
      (id: string)
      (inputType: string)
      (value: string)
      (placeholder: string)
      (error: string option)
      (onChange: string -> unit)
      =
      let hasError = error |> Option.isSome
      let labelClass =
        if hasError then
          "w-full text-preset-4 text-red-500 md:w-[240px] md:shrink-0 md:text-preset-3-regular md:text-red-500"
        else
          "w-full text-preset-4 text-gray-900 md:w-[240px] md:shrink-0 md:text-preset-3-regular md:text-gray-500"

      Html.div [
        prop.className "w-full flex flex-col items-start gap-2 md:flex-row md:items-center md:gap-4"
        prop.children [
          Html.label [
            prop.htmlFor id
            prop.className labelClass
            prop.text label
          ]
          Html.div [
            prop.className "w-full flex-1 min-w-0"
            prop.children [
              Ui.TextField.viewWithoutLabel {
                Id = id
                Label = label
                Value = value
                Placeholder = placeholder
                HelpText = None
                Error = error
                AutoFocus = false
                InputType = inputType
                LeftIcon = None
                OnChange = onChange
              }
            ]
          ]
        ]
      ]

    Html.div [
      prop.className "bg-gray-50 px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6"
      prop.children [
        Html.div [
          prop.className "flex items-start gap-4 md:gap-6 min-h-[calc(100vh-84px)] md:min-h-[calc(100vh-128px)]"
          prop.children [
            Html.div [
              prop.className
                "hidden lg:flex w-[560px] bg-white rounded-[var(--radius-lg)] p-6 items-center justify-center"
              prop.children [
                Ui.PhoneMockup.view {Links = model.PreviewLinks; AvatarUrl = avatarOpt}
              ]
            ]

            Html.div [
              prop.className "bg-white rounded-[var(--radius-lg)] flex-1 min-w-0 flex flex-col overflow-hidden"
              prop.children [
                Html.div [
                  prop.className "flex-1 p-6 md:p-10 flex flex-col gap-10"
                  prop.children [
                    Html.div [
                      prop.className "flex flex-col gap-2"
                      prop.children [
                        Html.h1 [
                          prop.className "text-preset-2 md:text-preset-1 text-gray-900"
                          prop.text "Profile Details"
                        ]
                        Html.p [
                          prop.className "text-preset-3-regular text-gray-500"
                          prop.text "Add your details to create a personal touch to your profile."
                        ]
                      ]
                    ]

                    Html.div [
                      prop.className "flex flex-col gap-6"
                      prop.children [
                        Html.div [
                          prop.className "bg-gray-50 rounded-[var(--radius-lg)] p-6"
                          prop.children [
                            Html.div [
                              prop.className "flex flex-col items-start gap-4 w-full md:flex-row md:items-center"
                              prop.children [
                                Html.p [
                                  prop.className "w-full text-preset-3-regular text-gray-500 md:w-[240px] md:shrink-0"
                                  prop.text "Profile picture"
                                ]
                                Ui.ImageUpload.view {ImageUrl = avatarOpt; OnSelected = (SelectAvatarFile >> dispatch)}
                              ]
                            ]
                          ]
                        ]

                        Html.div [
                          prop.className "bg-gray-50 rounded-[var(--radius-lg)] p-6 flex flex-col gap-2 md:gap-4"
                          prop.children [
                            inputRow
                              "First name*"
                              "first-name"
                              "text"
                              form.FirstName
                              "e.g. John"
                              form.FirstNameError
                              (SetFirstName >> dispatch)
                            inputRow
                              "Last name*"
                              "last-name"
                              "text"
                              form.LastName
                              "e.g. Appleseed"
                              form.LastNameError
                              (SetLastName >> dispatch)
                            inputRow
                              "Email*"
                              "email"
                              "email"
                              form.DisplayEmail
                              "e.g. email@example.com"
                              form.DisplayEmailError
                              (SetDisplayEmail >> dispatch)
                          ]
                        ]
                      ]
                    ]
                  ]
                ]

                Html.div [
                  prop.className "border-t border-gray-200 p-4 md:px-10 md:py-6"
                  prop.children [
                    Html.div [
                      prop.className
                        "flex flex-col items-stretch gap-3 md:flex-row md:justify-end md:items-center md:gap-4"
                      prop.children [
                        match form.Error with
                        | Some e ->
                          Html.p [
                            prop.className "text-preset-4 text-red-600"
                            prop.text e
                          ]
                        | None -> Html.none
                        if form.Saved then
                          Html.p [
                            prop.className "text-preset-4 text-green-700"
                            prop.text "Saved!"
                          ]
                        else
                          Html.none
                        Ui.Button.view {|
                          variant = Ui.Button.Variant.Primary
                          size = Ui.Button.Size.MdMobileFull
                          active = false
                          disabled = form.IsSaving
                          onClick = (fun () -> dispatch Save)
                          text = if form.IsSaving then "Saving..." else "Save"
                        |}
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
