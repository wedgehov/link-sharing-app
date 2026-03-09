module LinkEditorPage

open System
open Feliz
open Elmish
open Shared.SharedModels
open Shared.Api
open Browser.Types
open ClientShared

// To handle edits and new items, we wrap the Link DTO
// with a client-side ID for stable keys in the UI.
type LinkItem = {ClientId: int; Link: Link}

type DragState = {DraggingClientId: int; DropTargetClientId: int}

type LoadedState = {
  Links: LinkItem list
  NextClientId: int
  IsSaving: bool
  Saved: bool
  Error: string option
  DragState: DragState option
  OpenDropdownForClientId: int option
  ProfileAvatarUrl: string option
}

type State =
  | Loading
  | Loaded of LoadedState
  | Error of string

type Model = {UserId: int; State: State}

type Msg =
  | LoadLinks
  | LinksLoaded of Result<Link list, AppError>
  | LoadProfileForMockup
  | ProfileForMockupLoaded of Result<UserProfile, AppError>
  | AddNewLink
  | UpdateLinkPlatform of clientId: int * platform: Platform
  | UpdateLinkUrl of clientId: int * url: string
  | RemoveLink of clientId: int
  | SaveLinks
  | SaveLinksResult of Result<unit, AppError>
  | DragStart of clientId: int
  | DragOver of clientId: int
  | Drop
  | ToggleDropdown of clientId: int
  | SelectPlatform of clientId: int * platform: Platform

let init (userId: int) : Model * Cmd<Msg> =
  {UserId = userId; State = Loading}, Cmd.ofMsg LoadLinks

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
  match msg, model.State with
  | LoadLinks, _ ->
    let load () = ApiClient.LinkApi.GetLinks model.UserId
    {model with State = Loading}, Cmd.OfAsync.either load () LinksLoaded (asUnexpected LinksLoaded)

  | LinksLoaded (Result.Ok links), _ ->
    let nextId = links.Length + 1

    let linkItems = links |> List.mapi (fun i l -> {ClientId = i + 1; Link = l})

    let newState =
      Loaded {
        Links = linkItems
        NextClientId = nextId
        IsSaving = false
        Saved = false
        Error = None
        DragState = None
        OpenDropdownForClientId = None
        ProfileAvatarUrl = None
      }

    {model with State = newState}, Cmd.ofMsg LoadProfileForMockup

  | LinksLoaded (Result.Error err), _ -> {model with State = Error (appErrorToMessage err)}, Cmd.none

  | LoadProfileForMockup, _ ->
    let load () =
      ApiClient.ProfileApi.GetProfile model.UserId
    model, Cmd.OfAsync.either load () ProfileForMockupLoaded (asUnexpected ProfileForMockupLoaded)

  | ProfileForMockupLoaded (Result.Ok profile), Loaded loadedState ->
    let avatarOpt =
      match profile.AvatarUrl with
      | Some url when not (System.String.IsNullOrWhiteSpace url) -> Some url
      | _ -> None
    let newState = {loadedState with ProfileAvatarUrl = avatarOpt}
    {model with State = Loaded newState}, Cmd.none

  | ProfileForMockupLoaded (Result.Error _), _ -> model, Cmd.none

  | AddNewLink, Loaded loadedState ->
    let newLink = {
      ClientId = loadedState.NextClientId
      Link = {
        Id = None
        Platform = Platform.GitHub // Default platform
        Url = ""
        SortOrder = loadedState.Links.Length + 1
      }
    }

    let newState =
      Loaded {
        loadedState with
            Links = loadedState.Links @ [newLink]
            NextClientId = loadedState.NextClientId + 1
            Saved = false
      }

    {model with State = newState}, Cmd.none

  | RemoveLink clientId, Loaded loadedState ->
    let updatedLinks =
      loadedState.Links
      |> List.filter (fun item -> item.ClientId <> clientId)
      // Re-assign SortOrder based on the new list order
      |> List.mapi (fun i item -> {item with Link = {item.Link with SortOrder = i + 1}})

    let newState = Loaded {loadedState with Links = updatedLinks; Saved = false}
    {model with State = newState}, Cmd.none

  | UpdateLinkPlatform (clientId, platform), Loaded loadedState ->
    let updatedLinks =
      loadedState.Links
      |> List.map (fun item ->
        if item.ClientId = clientId then
          {item with Link = {item.Link with Platform = platform}}
        else
          item
      )

    let newState = Loaded {loadedState with Links = updatedLinks; Saved = false}
    {model with State = newState}, Cmd.none

  | UpdateLinkUrl (clientId, url), Loaded loadedState ->
    let updatedLinks =
      loadedState.Links
      |> List.map (fun item ->
        if item.ClientId = clientId then
          {item with Link = {item.Link with Url = url}}
        else
          item
      )

    let newState = Loaded {loadedState with Links = updatedLinks; Saved = false}
    {model with State = newState}, Cmd.none

  | SaveLinks, Loaded loadedState ->
    let linksToSave = loadedState.Links |> List.map (fun item -> item.Link)
    let save () =
      ApiClient.LinkApi.SaveLinks model.UserId linksToSave
    let savingState = {loadedState with IsSaving = true; Saved = false; Error = None}
    {model with State = Loaded savingState}, Cmd.OfAsync.either save () SaveLinksResult (asUnexpected SaveLinksResult)

  | SaveLinksResult (Result.Ok ()), Loaded loadedState ->
    let newState = {loadedState with IsSaving = false; Saved = true}
    {model with State = Loaded newState}, Cmd.none

  | SaveLinksResult (Result.Error err), Loaded loadedState ->
    let newState = {loadedState with IsSaving = false; Error = Some (appErrorToMessage err); Saved = false}
    {model with State = Loaded newState}, Cmd.none

  | DragStart clientId, Loaded loadedState ->
    let newState = {loadedState with DragState = Some {DraggingClientId = clientId; DropTargetClientId = clientId}}
    {model with State = Loaded newState}, Cmd.none

  | DragOver clientId, Loaded loadedState ->
    let newState =
      match loadedState.DragState with
      | Some ds when ds.DropTargetClientId <> clientId -> {
          loadedState with
              DragState = Some {ds with DropTargetClientId = clientId}
        }
      | _ -> loadedState

    {model with State = Loaded newState}, Cmd.none

  | Drop, Loaded loadedState ->
    let reorderedLinks =
      match loadedState.DragState with
      | Some ds ->
        let links = loadedState.Links
        let maybeSource =
          links
          |> List.tryFind (fun item -> item.ClientId = ds.DraggingClientId)
        let maybeTarget =
          links
          |> List.tryFind (fun item -> item.ClientId = ds.DropTargetClientId)

        match maybeSource, maybeTarget with
        | Some source, Some target when source.ClientId <> target.ClientId ->
          // Remove the source item
          let mutable tempList =
            links
            |> List.filter (fun item -> item.ClientId <> source.ClientId)
          // Find the index of the target item
          let targetIndex =
            tempList
            |> List.findIndex (fun item -> item.ClientId = target.ClientId)
          // Insert the source item at the target's position
          tempList <- tempList |> List.insertAt targetIndex source
          // Re-index the SortOrder property
          tempList
          |> List.mapi (fun i item -> {item with Link = {item.Link with SortOrder = i + 1}})
        | _ -> links // No change
      | None -> loadedState.Links

    let newState = {loadedState with Links = reorderedLinks; DragState = None; Saved = false}
    {model with State = Loaded newState}, Cmd.none

  | ToggleDropdown clientId, Loaded loadedState ->
    let nextOpen =
      match loadedState.OpenDropdownForClientId with
      | Some id when id = clientId -> None
      | _ -> Some clientId
    {model with State = Loaded {loadedState with OpenDropdownForClientId = nextOpen}}, Cmd.none

  | SelectPlatform (clientId, platform), Loaded loadedState ->
    let updatedLinks =
      loadedState.Links
      |> List.map (fun item ->
        if item.ClientId = clientId then
          {item with Link = {item.Link with Platform = platform}}
        else
          item
      )
    {model with State = Loaded {loadedState with Links = updatedLinks; OpenDropdownForClientId = None; Saved = false}},
    Cmd.none

  | _, _ -> model, Cmd.none

let private platformOptions =
  [
    Platform.GitHub
    Platform.Twitter
    Platform.LinkedIn
    Platform.YouTube
    Platform.Facebook
    Platform.Twitch
    Platform.DevTo
    Platform.CodeWars
    Platform.FreeCodeCamp
    Platform.GitLab
    Platform.Hashnode
    Platform.StackOverflow
    Platform.FrontendMentor
  ]
  |> List.map (fun p -> string p, p)

let private platformFromString (s: string) : Platform =
  platformOptions
  |> List.tryPick (fun (name, p) -> if name = s then Some p else None)
  |> Option.defaultValue Platform.GitHub

let view (model: Model) (dispatch: Msg -> unit) =
  match model.State with
  | Loading -> Html.p "Loading links..."
  | Error msg ->
    Html.p [
      prop.style [style.color "red"]
      prop.text msg
    ]
  | Loaded loadedState ->
    let previewLinks = loadedState.Links |> List.map (fun i -> i.Link)
    Html.div [
      prop.className "max-w-5xl mx-auto p-6 lg:p-8 lg:grid lg:grid-cols-[380px_1fr] lg:gap-10"
      prop.children [
        // Left: phone mockup (desktop only)
        Html.div [
          prop.className "hidden lg:block"
          prop.children [
            Ui.PhoneMockup.view {Links = previewLinks; AvatarUrl = loadedState.ProfileAvatarUrl}
          ]
        ]

        // Right: page content
        Html.div [
          prop.className "flex flex-col gap-6"
          prop.children [
            Html.div [
              prop.className "flex flex-col gap-2"
              prop.children [
                Html.h1 [
                  prop.className "text-preset-1"
                  prop.text "Customize your links"
                ]
                Html.p [
                  prop.className "text-preset-3-regular text-gray-500"
                  prop.text "Add/edit/remove links below and then share all your profiles with the world!"
                ]
              ]
            ]

            Ui.Button.view {|
              variant = Ui.Button.Variant.Secondary
              size = Ui.Button.Size.Md
              active = false
              disabled = false
              onClick = (fun () -> dispatch AddNewLink)
              text = "+ Add new link"
            |}

            Html.div [
              prop.className "flex flex-col gap-4"
              prop.children [
                if loadedState.Links.IsEmpty then
                  Html.div [
                    prop.className "p-10 bg-gray-50 rounded-lg flex flex-col items-center justify-center text-center"
                    prop.children [
                      Html.img [
                        prop.src "/images/illustration-empty.svg"
                      ]
                      Html.h2 [
                        prop.className "text-preset-2 mt-6"
                        prop.text "Let's get you started"
                      ]
                      Html.p [
                        prop.className "text-preset-3-regular text-gray-500 mt-2 max-w-sm"
                        prop.text
                          "Use the “Add new link” button to get started. Once you have more than one link, you can reorder and edit them. We’re here to help you share your profiles with everyone!"
                      ]
                    ]
                  ]
                else
                  for item in loadedState.Links do
                    let isDraggingOver =
                      match loadedState.DragState with
                      | Some ds -> ds.DropTargetClientId = item.ClientId
                      | None -> false

                    let cardProps = [
                      prop.draggable true
                      prop.onDragStart (fun (ev: DragEvent) ->
                        ev.dataTransfer.setData ("text/plain", string item.ClientId)
                        |> ignore
                        ev.dataTransfer.effectAllowed <- "move"
                        dispatch (DragStart item.ClientId)
                      )
                      prop.onDragOver (fun (ev: DragEvent) ->
                        ev.preventDefault () // This is necessary to allow a drop
                        ev.dataTransfer.dropEffect <- "move"
                        dispatch (DragOver item.ClientId)
                      )
                      prop.onDrop (fun (ev: DragEvent) ->
                        ev.preventDefault ()
                        // Access to data ensures consistency on Safari/Firefox even if we don't use it
                        ev.dataTransfer.getData ("text/plain") |> ignore
                        dispatch Drop
                      )
                      prop.className (if isDraggingOver then "bg-blue-50" else "")
                      prop.key item.ClientId
                    ]

                    Html.div (
                      cardProps
                      @ [
                        prop.children [
                          Html.div [
                            prop.className "bg-white rounded-[var(--radius-lg)] shadow-[var(--shadow-md)] p-4"
                            prop.children [
                              Html.div [
                                prop.className "flex justify-between items-center mb-3"
                                prop.children [
                                  Html.div [
                                    prop.className "flex items-center gap-2"
                                    prop.children [
                                      Html.img [
                                        prop.src "/images/icon-drag-and-drop.svg"
                                        prop.alt "Drag handle"
                                        prop.className "cursor-grab"
                                      ]
                                      Html.h3 [
                                        prop.className "text-preset-3-semibold text-gray-500"
                                        prop.text (sprintf "Link #%d" item.Link.SortOrder)
                                      ]
                                    ]
                                  ]
                                  Html.button [
                                    prop.className "text-preset-3-regular text-gray-500 hover:text-red-600"
                                    prop.onClick (fun _ -> dispatch (RemoveLink item.ClientId))
                                    prop.text "Remove"
                                  ]
                                ]
                              ]

                              // Platform row (inline label)
                              Html.div [
                                prop.className "flex flex-col gap-1"
                                prop.children [
                                  Html.label [
                                    prop.className "text-sm text-gray-700"
                                    prop.text "Platform"
                                  ]
                                  (let items: Ui.Dropdown.Item list =
                                    platformOptions
                                    |> List.map (fun (name, p) -> {
                                      Id = name
                                      Label = name
                                      Icon = Ui.PlatformLink.iconFor p
                                    })
                                   let openState =
                                     match loadedState.OpenDropdownForClientId with
                                     | Some id when id = item.ClientId -> true
                                     | _ -> false
                                   Ui.Dropdown.view {
                                     Items = items
                                     SelectedId = Some (string item.Link.Platform)
                                     Open = openState
                                     Placeholder = "Select platform"
                                     Inline = true
                                     OnToggle = (fun () -> dispatch (ToggleDropdown item.ClientId))
                                     OnSelect =
                                       (fun id -> dispatch (SelectPlatform (item.ClientId, platformFromString id)))
                                   })
                                ]
                              ]

                              // Link row (inline label)
                              Html.div [
                                prop.className "flex flex-col gap-1"
                                prop.children [
                                  Html.label [
                                    prop.className "text-sm text-gray-700"
                                    prop.text "Link"
                                  ]
                                  Html.input [
                                    prop.className "w-full p-2 border rounded"
                                    prop.placeholder "e.g. https://www.github.com/john-appleseed"
                                    prop.value item.Link.Url
                                    prop.onChange (fun url -> dispatch (UpdateLinkUrl (item.ClientId, url)))
                                  ]
                                ]
                              ]
                            ]
                          ]
                        ]
                      ]
                    )
              ]
            ]

            // Save Button Footer
            Html.div [
              prop.className "p-4 border-t sticky bottom-0 bg-white"
              prop.children [
                Html.div [
                  prop.className "flex justify-end"
                  prop.children [
                    if loadedState.Saved then
                      Html.p [
                        prop.className "text-green-700 mr-4 self-center"
                        prop.text "Saved!"
                      ]
                    else
                      Html.none
                    match loadedState.Error with
                    | Some err ->
                      Html.p [
                        prop.className "text-red-600 mr-4 self-center"
                        prop.text err
                      ]
                    | None -> Html.none
                    Ui.Button.view {|
                      variant = Ui.Button.Variant.Primary
                      size = Ui.Button.Size.Md
                      active = false
                      disabled = loadedState.IsSaving
                      onClick = (fun () -> dispatch SaveLinks)
                      text = if loadedState.IsSaving then "Saving..." else "Save"
                    |}
                  ]
                ]
              ]
            ]
          ]
        ]
      ]
    ]
