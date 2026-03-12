module Ui.ImageUpload

open System
open Feliz
open Browser.Types

type Props = {
  ImageUrl: string option
  OnSelected: File -> unit
  IsUploading: bool
  UploadProgress: int option
}

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
  let imageUrl =
    p.ImageUrl
    |> Option.filter (fun u -> not (String.IsNullOrWhiteSpace u))

  Html.div [
    prop.className "flex flex-1 flex-col items-start gap-4 md:flex-row md:items-center md:gap-6"
    prop.children [
      Html.div [
        prop.className
          "group relative shrink-0 size-[150px] md:size-[193px] rounded-[var(--radius-lg)] overflow-hidden bg-[#efebff]"
        prop.children [
          match imageUrl, p.IsUploading with
          | _, true ->
            Html.div [
              prop.className "absolute inset-0 bg-gray-900/55 flex flex-col items-center justify-center gap-2"
              prop.children [
                Html.div [
                  prop.className "w-3/4 h-2 bg-gray-200 rounded-full overflow-hidden"
                  prop.children [
                    Html.div [
                      prop.className "h-full bg-purple-600 transition-all duration-300"
                      prop.style [
                        style.width (length.perc (defaultArg p.UploadProgress 0))
                      ]
                    ]
                  ]
                ]
                Html.span [
                  prop.className "text-white text-preset-4"
                  prop.text $"Uploading... {defaultArg p.UploadProgress 0}%%"
                ]
              ]
            ]
          | Some url, false when not (String.IsNullOrWhiteSpace url) ->
            Html.img [
              prop.src url
              prop.alt "Avatar preview"
              prop.className "absolute inset-0 w-full h-full object-cover"
            ]
            Html.label [
              prop.htmlFor inputId
              prop.className
                "absolute inset-0 bg-gray-900/55 text-white text-preset-3-semibold flex items-center justify-center text-center px-4 cursor-pointer opacity-0 transition-opacity duration-200 group-hover:opacity-100 group-focus-within:opacity-100"
              prop.text "Change Image"
            ]
          | _ ->
            Html.label [
              prop.htmlFor inputId
              prop.className "absolute inset-0 cursor-pointer"
              prop.children [
                Html.div [
                  prop.className
                    "absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 flex flex-col items-center gap-2"
                  prop.children [
                    Ui.Icon.view Ui.Icon.Name.UploadImage "Upload" (Some "w-10 h-10")
                    Html.span [
                      prop.className "text-preset-3-semibold text-purple-600 whitespace-nowrap"
                      prop.text "+ Upload Image"
                    ]
                  ]
                ]
              ]
            ]
          fileInput p.OnSelected inputId
        ]
      ]

      Html.p [
        prop.className "text-preset-4 text-gray-500 max-w-none md:max-w-[220px]"
        prop.text "Image must be below 1024x1024px. Use PNG or JPG format."
      ]
    ]
  ]
