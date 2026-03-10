module Ui.ImageUpload

open System
open Feliz
open Browser.Types

type Props = {ImageUrl: string option; OnSelected: File -> unit}

let private fileInput (onSelected: File -> unit) (inputId: string) =
  Html.input [
    prop.id inputId
    prop.type' "file"
    prop.accept "image/*"
    prop.className "hidden"
    prop.onChange (fun (ev: Event) ->
      let target = ev.target :?> HTMLInputElement
      if not (isNull target.files) && target.files.length > 0 then
        let file = target.files.item 0
        if not (isNull file) then
          onSelected file
    )
  ]

let view (p: Props) =
  let inputId = "image-upload-" + Guid.NewGuid().ToString ("N")
  let hasImage =
    p.ImageUrl
    |> Option.exists (fun u -> not (String.IsNullOrWhiteSpace u))

  Html.div [
    prop.className "flex items-center gap-4"
    prop.children [
      Html.div [
        prop.className
          "relative w-40 h-40 rounded-[var(--radius-lg)] bg-gray-100 overflow-hidden border-2 border-dashed border-gray-300 grid place-items-center"
        prop.children [
          match p.ImageUrl with
          | Some url when not (String.IsNullOrWhiteSpace url) ->
            Html.img [
              prop.src url
              prop.alt "Avatar preview"
              prop.className "absolute inset-0 w-full h-full object-cover"
            ]
          | _ ->
            Html.div [
              prop.className "flex flex-col items-center justify-center text-center px-2"
              prop.children [
                Ui.Icon.view Ui.Icon.Name.UploadImage "Upload" (Some "w-6 h-6 mb-2")
                Html.span [
                  prop.className "text-preset-4 text-gray-600"
                  prop.text "Upload Image"
                ]
              ]
            ]

          // Clickable overlay/button
          Html.label [
            prop.htmlFor inputId
            prop.className (
              "absolute inset-x-0 bottom-0 m-2 rounded-[var(--radius-md)] px-3 py-2 text-center cursor-pointer select-none "
              + (if hasImage then
                   "bg-white/90 text-purple-600 hover:bg-white"
                 else
                   "bg-purple-600 text-white hover:bg-purple-700")
            )
            prop.text (if hasImage then "Change Image" else "Upload Image")
          ]

          fileInput p.OnSelected inputId
        ]
      ]

      Html.div [
        prop.className "text-preset-4 text-gray-600"
        prop.children [
          Html.p "Image must be JPG, PNG, or GIF."
          Html.p "Recommended size 1024x1024 or smaller."
        ]
      ]
    ]
  ]
