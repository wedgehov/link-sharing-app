module Ui.Button

open Feliz

[<RequireQualifiedAccess>]
type Variant =
  | Primary
  | Secondary

[<RequireQualifiedAccess>]
type Size =
  | Md
  | MdFull
  | MdMobileFull
  | Sm

let private classes (variant: Variant) (size: Size) (disabled: bool) (active: bool) =
  let baseC =
    "inline-flex items-center justify-center font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-purple-600 disabled:opacity-50 disabled:cursor-not-allowed"
  let v =
    match variant, active with
    | Variant.Primary, true -> "bg-purple-300 text-white shadow-[0_0_32px_rgba(99,60,255,0.25)]"
    | Variant.Primary, false ->
      "bg-purple-600 text-white hover:bg-purple-300 hover:shadow-[0_0_32px_rgba(99,60,255,0.25)]"
    | Variant.Secondary, true -> "bg-[#efebff] text-purple-600 border border-purple-600"
    | Variant.Secondary, false -> "bg-transparent text-purple-600 border border-purple-600 hover:bg-[#efebff]"
  let s =
    match size with
    | Size.Md -> "px-6 py-4 text-preset-3-semibold rounded-[var(--radius-md)]"
    | Size.MdFull -> "w-full px-6 py-4 text-preset-3-semibold rounded-[var(--radius-md)]"
    | Size.MdMobileFull -> "w-full md:w-auto px-6 py-4 text-preset-3-semibold rounded-[var(--radius-md)]"
    | Size.Sm -> "px-3 py-1.5 text-preset-4 rounded-[var(--radius-sm)]"
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
