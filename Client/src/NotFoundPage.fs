module NotFoundPage

open Feliz
open Routing

let view (fallbackPage: Page) =
  Html.div [
    prop.className "min-h-screen bg-gray-50 px-6 py-16"
    prop.children [
      Html.main [
        prop.className "max-w-xl mx-auto"
        prop.children [
          Html.div [
            prop.className "bg-white rounded-[var(--radius-lg)] shadow-[var(--shadow-md)] p-8 text-center"
            prop.children [
              Html.h1 [
                prop.className "text-preset-1 mb-2"
                prop.text "Page not found"
              ]
              Html.p [
                prop.className "text-preset-3-regular text-gray-600 mb-8"
                prop.text "The link you opened does not exist or is no longer available."
              ]
              Html.a [
                prop.className
                  "inline-flex items-center justify-center rounded-[var(--radius-md)] bg-purple-600 text-white px-6 py-3 text-preset-3-semibold hover:bg-purple-700 transition-colors"
                prop.href (href fallbackPage)
                prop.text "Go back"
              ]
            ]
          ]
        ]
      ]
    ]
  ]
