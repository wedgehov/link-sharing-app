module Ui.Dropdown

open Feliz

type Item = {Id: string; Label: string; Icon: Ui.Icon.Name}

type Props = {
  Items: Item list
  SelectedId: string option
  Open: bool
  Placeholder: string
  Inline: bool
  OnToggle: unit -> unit
  OnSelect: string -> unit
}

let private selectedLabel (items: Item list) (selectedId: string option) (placeholder: string) =
  match selectedId with
  | Some sid ->
    items
    |> List.tryFind (fun i -> i.Id = sid)
    |> Option.map (fun i -> i.Label)
    |> Option.defaultValue placeholder
  | None -> placeholder

let view (p: Props) =
  let label = selectedLabel p.Items p.SelectedId p.Placeholder
  let isActive = p.Open
  let selectedItemOpt =
    match p.SelectedId with
    | Some sid -> p.Items |> List.tryFind (fun i -> i.Id = sid)
    | None -> None
  let triggerIcon =
    match selectedItemOpt with
    | Some i -> i.Icon
    | None -> Ui.Icon.Name.Link

  Html.div [
    prop.className "relative w-full"
    prop.children [
      Html.button [
        prop.className (
          "w-full text-left border rounded-[var(--radius-md)] px-3 py-2 bg-white flex items-center justify-between "
          + (if isActive then
               "ring-2 ring-purple-600 border-purple-600"
             else
               "border-gray-300 hover:border-gray-400")
        )
        prop.onClick (fun _ -> p.OnToggle ())
        prop.children [
          Html.div [
            prop.className "flex items-center gap-2"
            prop.children [
              Ui.Icon.view triggerIcon label (Some "w-4 h-4")
              Html.span [
                prop.className "text-sm text-gray-900"
                prop.text label
              ]
            ]
          ]
          Ui.Icon.view
            Ui.Icon.Name.ChevronDown
            "Open"
            (Some (
              if isActive then
                "w-4 h-4 rotate-180 transition-transform"
              else
                "w-4 h-4 transition-transform"
            ))
        ]
      ]

      if p.Open then
        Html.div [
          prop.className (
            (if p.Inline then
               "mt-1 "
             else
               "absolute left-0 right-0 mt-1 z-10 ")
            + "bg-white border border-gray-200 rounded-[var(--radius-md)] shadow-[var(--shadow-md)] max-h-60 overflow-auto"
          )
          prop.children (
            let elements =
              p.Items
              |> List.mapi (fun idx i ->
                let sep =
                  if idx = 0 then
                    Html.none
                  else
                    Html.div [
                      prop.key ("sep-" + i.Id)
                      prop.className "h-px bg-gray-200 my-2 mx-4"
                    ]
                let btn =
                  Html.button [
                    prop.key i.Id
                    prop.className (
                      "w-full flex items-center gap-3 px-3 py-2 text-sm hover:bg-gray-100 "
                      + (
                        match p.SelectedId with
                        | Some sid when sid = i.Id -> "text-purple-600"
                        | _ -> "text-gray-900"
                      )
                    )
                    prop.onClick (fun _ -> p.OnSelect i.Id)
                    prop.children [
                      Ui.Icon.view i.Icon i.Label (Some "w-4 h-4")
                      Html.span [prop.text i.Label]
                    ]
                  ]
                [sep; btn]
              )
              |> List.concat
            elements
          )
        ]
      else
        Html.none
    ]
  ]
