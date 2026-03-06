module Ui.Button

open Feliz

[<RequireQualifiedAccess>]
type Variant =
  | Primary
  | Secondary

[<RequireQualifiedAccess>]
type Size =
  | Md
  | Sm

let private classes (variant: Variant) (size: Size) (disabled: bool) (active: bool) =
  let baseC =
    "inline-flex items-center justify-center font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-purple-600 disabled:opacity-50 disabled:cursor-not-allowed rounded"
  let v =
    match variant, active with
    | Variant.Primary, true -> "bg-purple-950 text-white"
    | Variant.Primary, false -> "bg-purple-600 text-white hover:bg-purple-700"
    | Variant.Secondary, true -> "bg-purple-300 text-purple-950 border border-purple-300"
    | Variant.Secondary, false -> "bg-transparent text-purple-600 border border-purple-600 hover:bg-purple-300/20"
  let s =
    match size with
    | Size.Md -> "px-4 py-2 text-sm rounded-[var(--radius-md)] shadow-[var(--shadow-sm)]"
    | Size.Sm -> "px-3 py-1.5 text-xs rounded-[var(--radius-sm)]"
  String.concat " " [
    baseC
    v
    s
    (if disabled then "" else "cursor-pointer")
  ]

let view
  (props:
    {|
      variant: Variant
      size: Size
      active: bool
      disabled: bool
      onClick: unit -> unit
      text: string
    |})
  =
  Html.button [
    prop.className (classes props.variant props.size props.disabled props.active)
    prop.disabled props.disabled
    prop.onClick (fun _ ->
      if not props.disabled then
        props.onClick ()
    )
    prop.text props.text
  ]
