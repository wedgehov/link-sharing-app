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
  LastSavedLinks: Link list
  NextClientId: int
  IsSaving: bool
  Saved: bool
  Error: string option
  LinkErrors: Map<int, string>
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
  | ClearSaveToast
  | DragStart of clientId: int
  | DragOver of clientId: int
  | Drop
  | ToggleDropdown of clientId: int
  | SelectPlatform of clientId: int * platform: Platform

let init (userId: int) : Model * Cmd<Msg> =
  {UserId = userId; State = Loading}, Cmd.ofMsg LoadLinks

let private hostForPlatform =
  function
  | Platform.GitHub -> "github.com"
  | Platform.Twitter -> "twitter.com"
  | Platform.LinkedIn -> "linkedin.com"
  | Platform.YouTube -> "youtube.com"
  | Platform.Facebook -> "facebook.com"
  | Platform.Twitch -> "twitch.tv"
  | Platform.DevTo -> "dev.to"
  | Platform.CodeWars -> "codewars.com"
  | Platform.FreeCodeCamp -> "freecodecamp.org"
  | Platform.GitLab -> "gitlab.com"
  | Platform.Hashnode -> "hashnode.com"
  | Platform.StackOverflow -> "stackoverflow.com"
  | Platform.FrontendMentor -> "frontendmentor.io"

let private linkPlaceholderForPlatform (platform: Platform) =
  match platform with
  | Platform.GitHub -> "e.g. https://www.github.com/john-appleseed"
  | Platform.Twitter -> "e.g. https://www.twitter.com/john-appleseed"
  | Platform.LinkedIn -> "e.g. https://www.linkedin.com/in/john-appleseed"
  | Platform.YouTube -> "e.g. https://www.youtube.com/@john-appleseed"
  | Platform.Facebook -> "e.g. https://www.facebook.com/john-appleseed"
  | Platform.Twitch -> "e.g. https://www.twitch.tv/john-appleseed"
  | Platform.DevTo -> "e.g. https://www.dev.to/john-appleseed"
  | Platform.CodeWars -> "e.g. https://www.codewars.com/users/john-appleseed"
  | Platform.FreeCodeCamp -> "e.g. https://www.freecodecamp.org/john-appleseed"
  | Platform.GitLab -> "e.g. https://www.gitlab.com/john-appleseed"
  | Platform.Hashnode -> "e.g. https://www.hashnode.com/@john-appleseed"
  | Platform.StackOverflow -> "e.g. https://www.stackoverflow.com/users/john-appleseed"
  | Platform.FrontendMentor -> "e.g. https://www.frontendmentor.io/profile/john-appleseed"

let private normalizeLinkUrl (platform: Platform) (rawUrl: string) : Result<string, string> =
  if String.IsNullOrWhiteSpace rawUrl then
    Result.Error "Can't be empty"
  else
    let expectedHost = hostForPlatform platform
    let trimmed = rawUrl.Trim ()
    let withoutScheme =
      let lower = trimmed.ToLowerInvariant ()
      if lower.StartsWith "https://" then
        trimmed.Substring ("https://".Length)
      elif lower.StartsWith "http://" then
        trimmed.Substring ("http://".Length)
      elif lower.StartsWith "//" then
        trimmed.Substring (2)
      else
        trimmed
    let withoutWww =
      if withoutScheme.StartsWith ("www.", StringComparison.OrdinalIgnoreCase) then
        withoutScheme.Substring ("www.".Length)
      else
        withoutScheme
    let candidateLower = withoutWww.ToLowerInvariant ()
    let expectedLower = expectedHost.ToLowerInvariant ()
    let suffixOpt =
      if candidateLower = expectedLower then
        Some ""
      elif
        candidateLower.StartsWith (expectedLower + "/", StringComparison.Ordinal)
        || candidateLower.StartsWith (expectedLower + "?", StringComparison.Ordinal)
        || candidateLower.StartsWith (expectedLower + "#", StringComparison.Ordinal)
      then
        Some (withoutWww.Substring (expectedHost.Length))
      else
        None
    match suffixOpt with
    | Some suffix -> Result.Ok ("https://www." + expectedHost + suffix)
    | None -> Result.Error "Please check URL"

let private validateAndNormalizeLinks (links: LinkItem list) : Result<LinkItem list, Map<int, string>> =
  let normalizedLinks, errors =
    links
    |> List.fold
      (fun (validAcc, errorAcc) item ->
        match normalizeLinkUrl item.Link.Platform item.Link.Url with
        | Result.Ok normalizedUrl ->
          let normalizedItem = {item with Link = {item.Link with Url = normalizedUrl}}
          normalizedItem :: validAcc, errorAcc
        | Result.Error errorMessage -> validAcc, errorAcc |> Map.add item.ClientId errorMessage
      )
      ([], Map.empty)
  if errors |> Map.isEmpty then
    normalizedLinks |> List.rev |> Result.Ok
  else
    Result.Error errors

let private clearSaveToastCmd =
  Cmd.OfAsync.perform (fun () -> async {do! Async.Sleep 2500}) () (fun _ -> ClearSaveToast)

let private linkSnapshot (links: Link list) =
  links |> List.map (fun l -> l.Platform, l.Url, l.SortOrder)

let private currentLinks (loadedState: LoadedState) =
  loadedState.Links |> List.map (fun item -> item.Link)

let private hasUnsavedChanges (loadedState: LoadedState) =
  linkSnapshot (currentLinks loadedState)
  <> linkSnapshot loadedState.LastSavedLinks

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
        LastSavedLinks = links
        NextClientId = nextId
        IsSaving = false
        Saved = false
        Error = None
        LinkErrors = Map.empty
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

    let newState =
      Loaded {
        loadedState with
            Links = updatedLinks
            LinkErrors = loadedState.LinkErrors |> Map.remove clientId
            Saved = false
      }
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

    let newState =
      Loaded {
        loadedState with
            Links = updatedLinks
            LinkErrors = loadedState.LinkErrors |> Map.remove clientId
            Saved = false
      }
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

    let newState =
      Loaded {
        loadedState with
            Links = updatedLinks
            LinkErrors = loadedState.LinkErrors |> Map.remove clientId
            Saved = false
      }
    {model with State = newState}, Cmd.none

  | SaveLinks, Loaded loadedState ->
    if not (hasUnsavedChanges loadedState) then
      model, Cmd.none
    else
      match validateAndNormalizeLinks loadedState.Links with
      | Result.Error linkErrors ->
        let invalidState = {
          loadedState with
              IsSaving = false
              Saved = false
              Error = None
              LinkErrors = linkErrors
        }
        {model with State = Loaded invalidState}, Cmd.none
      | Result.Ok normalizedLinks ->
        let linksToSave = normalizedLinks |> List.map (fun item -> item.Link)
        let save () =
          ApiClient.LinkApi.SaveLinks model.UserId linksToSave
        let savingState = {
          loadedState with
              Links = normalizedLinks
              IsSaving = true
              Saved = false
              Error = None
              LinkErrors = Map.empty
        }
        {model with State = Loaded savingState},
        Cmd.OfAsync.either save () SaveLinksResult (asUnexpected SaveLinksResult)

  | SaveLinksResult (Result.Ok ()), Loaded loadedState ->
    let newState = {
      loadedState with
          IsSaving = false
          Saved = true
          LinkErrors = Map.empty
          LastSavedLinks = currentLinks loadedState
    }
    {model with State = Loaded newState}, clearSaveToastCmd

  | ClearSaveToast, Loaded loadedState -> {model with State = Loaded {loadedState with Saved = false}}, Cmd.none

  | SaveLinksResult (Result.Error err), Loaded loadedState ->
    let newState = {
      loadedState with
          IsSaving = false
          Error = Some (appErrorToMessage err)
          Saved = false
          LinkErrors = Map.empty
    }
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
    {
      model with
          State =
            Loaded {
              loadedState with
                  Links = updatedLinks
                  OpenDropdownForClientId = None
                  LinkErrors = loadedState.LinkErrors |> Map.remove clientId
                  Saved = false
            }
    },
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
  | Loading ->
    Html.p [
      prop.className "bg-gray-50 px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6 text-preset-3-regular text-gray-500"
      prop.text "Loading links..."
    ]
  | Error msg ->
    Html.p [
      prop.className "bg-gray-50 px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6 text-preset-3-regular text-red-600"
      prop.text msg
    ]
  | Loaded loadedState ->
    let previewLinks = loadedState.Links |> List.map (fun i -> i.Link)
    let disableSave = loadedState.IsSaving || not (hasUnsavedChanges loadedState)
    Html.div [
      prop.className "bg-gray-50 px-4 pt-4 pb-4 md:px-6 md:pt-6 md:pb-6"
      prop.children [
        Html.div [
          prop.className "flex items-start gap-4 md:gap-6 min-h-[calc(100vh-84px)] md:min-h-[calc(100vh-128px)]"
          prop.children [
            Html.div [
              prop.className
                "hidden lg:flex self-start w-[560px] bg-white rounded-[var(--radius-lg)] p-6 items-start justify-center"
              prop.children [
                Ui.PhoneMockup.view {Links = previewLinks; AvatarUrl = loadedState.ProfileAvatarUrl}
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
                          prop.text "Customize your links"
                        ]
                        Html.p [
                          prop.className "text-preset-3-regular text-gray-500"
                          prop.text "Add/edit/remove links below and then share all your profiles with the world!"
                        ]
                      ]
                    ]

                    Html.div [
                      prop.className "flex flex-col gap-6"
                      prop.children [
                        Ui.Button.view {|
                          variant = Ui.Button.Variant.Secondary
                          size = Ui.Button.Size.MdFull
                          active = false
                          disabled = false
                          onClick = (fun () -> dispatch AddNewLink)
                          text = "+ Add new link"
                        |}

                        if loadedState.Links.IsEmpty then
                          Html.div [
                            prop.className
                              "px-5 py-16 bg-gray-50 rounded-[var(--radius-lg)] flex flex-col items-center justify-center text-center"
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
                          Html.div [
                            prop.className "flex flex-col gap-6"
                            prop.children [
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
                                    ev.preventDefault ()
                                    ev.dataTransfer.dropEffect <- "move"
                                    dispatch (DragOver item.ClientId)
                                  )
                                  prop.onDrop (fun (ev: DragEvent) ->
                                    ev.preventDefault ()
                                    ev.dataTransfer.getData ("text/plain") |> ignore
                                    dispatch Drop
                                  )
                                  prop.className (
                                    "bg-gray-50 rounded-[var(--radius-lg)] p-5 flex flex-col gap-3 "
                                    + if isDraggingOver then "ring-2 ring-purple-300" else ""
                                  )
                                  prop.key item.ClientId
                                ]

                                Html.div (
                                  cardProps
                                  @ [
                                    prop.children [
                                      Html.div [
                                        prop.className "flex justify-between items-center"
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
                                            prop.className "text-preset-3-regular text-gray-500 hover:text-gray-900"
                                            prop.onClick (fun _ -> dispatch (RemoveLink item.ClientId))
                                            prop.text "Remove"
                                          ]
                                        ]
                                      ]

                                      Html.div [
                                        prop.className "flex flex-col gap-1"
                                        prop.children [
                                          Html.label [
                                            prop.className "text-preset-4 text-gray-500"
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
                                               (fun id ->
                                                 dispatch (SelectPlatform (item.ClientId, platformFromString id))
                                               )
                                           })
                                        ]
                                      ]

                                      Ui.TextField.view {
                                        Id = $"link-url-{item.ClientId}"
                                        Label = "Link"
                                        Value = item.Link.Url
                                        Placeholder = linkPlaceholderForPlatform item.Link.Platform
                                        HelpText = None
                                        Error = loadedState.LinkErrors |> Map.tryFind item.ClientId
                                        AutoFocus = false
                                        InputType = "text"
                                        LeftIcon = Some Ui.Icon.Name.Link
                                        OnChange = (fun url -> dispatch (UpdateLinkUrl (item.ClientId, url)))
                                      }
                                    ]
                                  ]
                                )
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
                        match loadedState.Error with
                        | Some err ->
                          Html.p [
                            prop.className "text-preset-4 text-red-600"
                            prop.text err
                          ]
                        | None -> Html.none
                        Ui.Button.view {|
                          variant = Ui.Button.Variant.Primary
                          size = Ui.Button.Size.MdMobileFull
                          active = false
                          disabled = disableSave
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
        if loadedState.Saved then
          Ui.Toast.view {
            Message = "Your changes have been successfully saved!"
            Variant = Ui.Toast.Variant.Success
            Icon = Some Ui.Icon.Name.ChangesSaved
            Uppercase = true
          }
        else
          Html.none
      ]
    ]
