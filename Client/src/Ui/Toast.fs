module Ui.Toast

open Feliz

[<RequireQualifiedAccess>]
type Variant =
  | Success
  | Error

type Props = {
  Message: string
  Variant: Variant
  Icon: Ui.Icon.Name option
  Uppercase: bool
}

let view (p: Props) =
  let containerClass =
    let toneClass =
      match p.Variant with
      | Variant.Success -> "bg-gray-900 text-white"
      | Variant.Error -> "bg-red-550 text-white"

    String.concat " " [
      "fixed bottom-6 left-1/2 -translate-x-1/2 z-[9999]"
      "rounded-[var(--radius-md)] px-4 py-3 shadow-[var(--shadow-lg)]"
      "flex items-center gap-3 max-w-[calc(100vw-2rem)] pointer-events-none"
      toneClass
    ]

  let messageClass =
    if p.Uppercase then
      "text-preset-4 uppercase tracking-[0.03em]"
    else
      "text-preset-4"

  Html.div [
    prop.className containerClass
    prop.role "status"
    prop.children [
      match p.Icon with
      | Some icon -> Ui.Icon.view icon "Toast icon" None
      | None -> Html.none
      Html.p [
        prop.className messageClass
        prop.text p.Message
      ]
    ]
  ]
