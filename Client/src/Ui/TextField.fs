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

let private render (showLabel: bool) (p: Props) =
  let hasError = p.Error |> Option.isSome
  let iconColorClass =
    match hasError with
    | true ->
      "[filter:brightness(0)_saturate(100%)_invert(39%)_sepia(87%)_saturate(2520%)_hue-rotate(333deg)_brightness(103%)_contrast(102%)]"
    | false -> ""

  let leftPad =
    match p.LeftIcon with
    | Some _ -> " pl-11"
    | None -> ""
  let errorPad =
    match p.Error with
    | Some _ -> " md:pr-72"
    | None -> ""

  Html.div [
    prop.className "w-full flex flex-col gap-1"
    prop.children [
      if showLabel then
        Html.label [
          prop.htmlFor p.Id
          prop.className "text-preset-4 text-gray-500"
          prop.text p.Label
        ]
      Html.div [
        prop.className "relative"
        prop.children [
          match p.LeftIcon with
          | Some icon ->
            Ui.Icon.view
              icon
              p.Label
              (Some (
                "absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 "
                + iconColorClass
              ))
          | None -> Html.none
          Html.input [
            prop.id p.Id
            prop.value p.Value
            prop.placeholder p.Placeholder
            prop.type' p.InputType
            prop.className (
              "w-full border rounded-[var(--radius-md)] h-14 px-4 text-preset-3-regular outline-none transition-all bg-white"
              + leftPad
              + errorPad
              + " "
              + match p.Error with
                | Some _ -> "border-red-500"
                | None -> "border-gray-200 focus:border-purple-600 focus:shadow-[0_0_32px_rgba(99,60,255,0.25)]"
            )
            if p.AutoFocus then
              prop.autoFocus true
            prop.onChange p.OnChange
          ]
          match p.Error with
          | Some e ->
            Html.span [
              prop.className
                "hidden md:block absolute right-4 top-1/2 -translate-y-1/2 max-w-[220px] text-preset-4 text-red-500 text-right leading-[1.15] whitespace-normal"
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
        | Some h ->
          Html.p [
            prop.className "text-preset-4 text-gray-500"
            prop.text h
          ]
        | None -> Html.none
    ]
  ]

let view (p: Props) = render true p

let viewWithoutLabel (p: Props) = render false p
