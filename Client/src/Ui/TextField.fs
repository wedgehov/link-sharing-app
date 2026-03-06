module Ui.TextField

open Feliz

type Props = {
  Id: string
  Label: string
  Value: string
  Placeholder: string
  HelpText: string option
  Error: string option
  AutoFocus: bool
  InputType: string
  LeftIcon: Ui.Icon.Name option
  OnChange: string -> unit
}

let view (p: Props) =
  Html.div [
    prop.className "flex flex-col gap-1"
    prop.children [
      Html.label [
        prop.htmlFor p.Id
        prop.className "text-sm text-gray-700"
        prop.text p.Label
      ]
      Html.div [
        prop.className "relative"
        prop.children [
          // Optional left icon
          match p.LeftIcon with
          | Some icon -> Ui.Icon.view icon p.Label (Some "absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4")
          | None -> Html.none

          // The input itself
          Html.input [
            prop.id p.Id
            prop.value p.Value
            prop.placeholder p.Placeholder
            prop.type' p.InputType
            prop.className (
              let leftPad =
                match p.LeftIcon with
                | Some _ -> " pl-9"
                | None -> ""
              let errorPad =
                match p.Error with
                | Some _ -> " md:pr-28"
                | None -> ""
              "w-full border rounded-[var(--radius-md)] p-2 outline-none transition-all"
              + leftPad
              + errorPad
              + " "
              + match p.Error with
                | Some _ -> "border-red-500"
                | None -> "border-gray-300 focus:ring-2 focus:ring-purple-600 focus:border-purple-600"
            )
            if p.AutoFocus then
              prop.autoFocus true
            prop.onChange p.OnChange
          ]

          // Inline (inside input) error for md and up
          match p.Error with
          | Some e ->
            Html.span [
              prop.className "hidden md:block absolute right-3 top-1/2 -translate-y-1/2 text-xs text-red-600 text-right"
              prop.text e
            ]
          | None -> Html.none
        ]
      ]

      // Below-input error on mobile only
      match p.Error with
      | Some e ->
        Html.p [
          prop.className "block md:hidden text-xs text-red-600 text-right"
          prop.text e
        ]
      | None ->
        match p.HelpText with
        | Some h ->
          Html.p [
            prop.className "text-xs text-gray-600"
            prop.text h
          ]
        | None -> Html.none
    ]
  ]
