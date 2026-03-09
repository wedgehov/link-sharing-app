module ProfileEditorPage

open System
open Feliz
open Elmish
open Shared.SharedModels
open Shared.Api
open ClientShared

type ProfileForm = {
  FirstName: string
  LastName: string
  DisplayEmail: string
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
  | SetFirstName of string
  | SetLastName of string
  | SetDisplayEmail of string
  | SetAvatarUrl of string
  | Save
  | SaveResult of Result<unit, AppError>

let private toForm (p: UserProfile) : ProfileForm = {
  FirstName = p.FirstName
  LastName = p.LastName
  DisplayEmail = defaultArg p.DisplayEmail ""
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

  | SetFirstName v, Loaded form -> {model with State = Loaded {form with FirstName = v; Saved = false}}, Cmd.none
  | SetLastName v, Loaded form -> {model with State = Loaded {form with LastName = v; Saved = false}}, Cmd.none
  | SetDisplayEmail v, Loaded form -> {model with State = Loaded {form with DisplayEmail = v; Saved = false}}, Cmd.none
  | SetAvatarUrl v, Loaded form -> {model with State = Loaded {form with AvatarUrl = v; Saved = false}}, Cmd.none

  | Save, Loaded form ->
    let dto = toDto form
    let save () =
      ApiClient.ProfileApi.SaveProfile model.UserId dto
    let saving = {form with IsSaving = true; Error = None; Saved = false}
    {model with State = Loaded saving}, Cmd.OfAsync.either save () SaveResult (asUnexpected SaveResult)

  | SaveResult (Result.Ok ()), Loaded form ->
    {model with State = Loaded {form with IsSaving = false; Saved = true}}, Cmd.none

  | SaveResult (Result.Error err), Loaded form ->
    {model with State = Loaded {form with IsSaving = false; Error = Some (appErrorToMessage err); Saved = false}},
    Cmd.none

  | _, _ -> model, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) =
  match model.State with
  | Loading -> Html.p "Loading profile..."
  | Error msg ->
    Html.p [
      prop.style [style.color "red"]
      prop.text msg
    ]
  | Loaded form ->
    Html.div [
      prop.className "max-w-5xl mx-auto p-6 lg:p-8 lg:grid lg:grid-cols-[380px_1fr] lg:gap-10"
      prop.children [
        // Left: phone mockup (desktop only)
        Html.div [
          prop.className "hidden lg:block"
          prop.children [
            // Show avatar (from current form state) and saved links in the phone mockup
            let avatarOpt =
              if String.IsNullOrWhiteSpace form.AvatarUrl then
                None
              else
                Some form.AvatarUrl
            Ui.PhoneMockup.view {Links = model.PreviewLinks; AvatarUrl = avatarOpt}
          ]
        ]

        // Right: content
        Html.div [
          prop.className "max-w-2xl flex flex-col gap-6"
          prop.children [
            Html.div [
              prop.className "flex flex-col gap-2"
              prop.children [
                Html.h1 [
                  prop.className "text-preset-1"
                  prop.text "Profile details"
                ]
                Html.p [
                  prop.className "text-preset-3-regular text-gray-500"
                  prop.text "Add your details to create a personal touch to your profile."
                ]
              ]
            ]

            Html.div [
              prop.className "bg-white rounded-[var(--radius-lg)] shadow-[var(--shadow-md)] p-4"
              prop.children [
                // Profile picture row (inline label)
                Html.div [
                  prop.className "flex flex-col gap-1"
                  prop.children [
                    Html.label [
                      prop.className "text-sm text-gray-700"
                      prop.text "Profile picture"
                    ]
                    Ui.ImageUpload.view {
                      ImageUrl =
                        if String.IsNullOrWhiteSpace form.AvatarUrl then
                          None
                        else
                          Some form.AvatarUrl
                      OnSelected = (SetAvatarUrl >> dispatch)
                    }
                  ]
                ]

                Ui.TextField.view {
                  Id = "first-name"
                  Label = "First Name"
                  Value = form.FirstName
                  Placeholder = "e.g. John"
                  HelpText = None
                  Error = None
                  AutoFocus = false
                  InputType = "text"
                  LeftIcon = None
                  OnChange = (SetFirstName >> dispatch)
                }

                Ui.TextField.view {
                  Id = "last-name"
                  Label = "Last Name"
                  Value = form.LastName
                  Placeholder = "e.g. Appleseed"
                  HelpText = None
                  Error = None
                  AutoFocus = false
                  InputType = "text"
                  LeftIcon = None
                  OnChange = (SetLastName >> dispatch)
                }

                Ui.TextField.view {
                  Id = "email"
                  Label = "Email"
                  Value = form.DisplayEmail
                  Placeholder = "e.g. john.appleseed@example.com"
                  HelpText = None
                  Error = None
                  AutoFocus = false
                  InputType = "email"
                  LeftIcon = Some Ui.Icon.Name.Email
                  OnChange = (SetDisplayEmail >> dispatch)
                }

                match form.Error with
                | Some e ->
                  Html.p [
                    prop.className "text-sm text-red-600"
                    prop.text e
                  ]
                | None -> Html.none
              ]
            ]

            Html.div [
              prop.className "p-4 border-t sticky bottom-0 bg-white"
              prop.children [
                Html.div [
                  prop.className "flex justify-end items-center gap-4"
                  prop.children [
                    if form.Saved then
                      Html.p [
                        prop.className "text-green-700"
                        prop.text "Saved!"
                      ]
                    else
                      Html.none
                    Ui.Button.view {|
                      variant = Ui.Button.Variant.Primary
                      size = Ui.Button.Size.Md
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
