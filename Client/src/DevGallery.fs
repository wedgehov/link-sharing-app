module DevGallery

open Feliz

let view =
  Html.div [
    prop.className "max-w-5xl mx-auto p-8 md:p-12 flex flex-col gap-12 md:gap-14 lg:gap-16"
    prop.children [
      Html.h1 [
        prop.className "text-preset-1"
        prop.text "Dev Gallery"
      ]
      Html.p [
        prop.className "text-preset-3-regular text-gray-500"
        prop.text "Preview shared UI components and variants."
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Buttons"
          ]
          Html.div [
            prop.className "grid grid-cols-1 md:grid-cols-2 gap-8 md:gap-10 lg:gap-12"
            prop.children [
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Primary"
                  ]
                  Html.div [
                    prop.className "flex flex-col gap-4"
                    prop.children [
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Primary
                        size = Ui.Button.Size.Md
                        active = false
                        disabled = false
                        onClick = ignore
                        text = "Default"
                      |}
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Primary
                        size = Ui.Button.Size.Md
                        active = true
                        disabled = false
                        onClick = ignore
                        text = "Active"
                      |}
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Primary
                        size = Ui.Button.Size.Md
                        active = false
                        disabled = true
                        onClick = ignore
                        text = "Disabled"
                      |}
                    ]
                  ]
                ]
              ]
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Secondary"
                  ]
                  Html.div [
                    prop.className "flex flex-col gap-4"
                    prop.children [
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Secondary
                        size = Ui.Button.Size.Md
                        active = false
                        disabled = false
                        onClick = ignore
                        text = "Default"
                      |}
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Secondary
                        size = Ui.Button.Size.Md
                        active = true
                        disabled = false
                        onClick = ignore
                        text = "Active"
                      |}
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Secondary
                        size = Ui.Button.Size.Md
                        active = false
                        disabled = true
                        onClick = ignore
                        text = "Disabled"
                      |}
                    ]
                  ]
                ]
              ]
            ]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "TextField"
          ]
          Html.div [
            prop.className "grid grid-cols-1 md:grid-cols-2 gap-8 md:gap-10 lg:gap-12"
            prop.children [
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Empty"
                  ]
                  Ui.TextField.view {
                    Id = "tf-empty"
                    Label = "Label"
                    Value = ""
                    Placeholder = "Placeholder"
                    HelpText = Some "Helper text"
                    Error = None
                    AutoFocus = false
                    InputType = "text"
                    LeftIcon = None
                    OnChange = ignore
                  }
                ]
              ]
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Filled"
                  ]
                  Ui.TextField.view {
                    Id = "tf-filled"
                    Label = "Label"
                    Value = "Some text"
                    Placeholder = "Placeholder"
                    HelpText = Some "Helper text"
                    Error = None
                    AutoFocus = false
                    InputType = "text"
                    LeftIcon = Some Ui.Icon.Name.Link
                    OnChange = ignore
                  }
                ]
              ]
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Active"
                  ]
                  Ui.TextField.view {
                    Id = "tf-active"
                    Label = "Label"
                    Value = ""
                    Placeholder = "Placeholder"
                    HelpText = Some "Helper text"
                    Error = None
                    AutoFocus = true
                    InputType = "text"
                    LeftIcon = None
                    OnChange = ignore
                  }
                ]
              ]
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Error"
                  ]
                  Ui.TextField.view {
                    Id = "tf-error"
                    Label = "Label"
                    Value = ""
                    Placeholder = "Placeholder"
                    HelpText = None
                    Error = Some "This field is required"
                    AutoFocus = false
                    InputType = "text"
                    LeftIcon = None
                    OnChange = ignore
                  }
                ]
              ]
            ]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Card"
          ]
          Html.div [
            prop.className "bg-white rounded-[var(--radius-lg)] shadow-[var(--shadow-md)] p-4"
            prop.children [Html.p "Cards wrap arbitrary content."]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Dropdown"
          ]
          Html.div [
            prop.className "grid grid-cols-1 md:grid-cols-2 gap-8 md:gap-10 lg:gap-12"
            prop.children [
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Default"
                  ]
                  Ui.Dropdown.view {
                    Items = [
                      {Id = "github"; Label = "GitHub"; Icon = Ui.Icon.Name.GitHub}
                      {Id = "twitter"; Label = "Twitter"; Icon = Ui.Icon.Name.Twitter}
                      {Id = "linkedin"; Label = "LinkedIn"; Icon = Ui.Icon.Name.LinkedIn}
                    ]
                    SelectedId = None
                    Open = false
                    Placeholder = "Select platform"
                    Inline = true
                    OnToggle = (fun () -> ())
                    OnSelect = ignore
                  }
                ]
              ]
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "Active"
                  ]
                  Ui.Dropdown.view {
                    Items = [
                      {Id = "github"; Label = "GitHub"; Icon = Ui.Icon.Name.GitHub}
                      {Id = "twitter"; Label = "Twitter"; Icon = Ui.Icon.Name.Twitter}
                      {Id = "linkedin"; Label = "LinkedIn"; Icon = Ui.Icon.Name.LinkedIn}
                    ]
                    SelectedId = Some "twitter"
                    Open = true
                    Placeholder = "Select platform"
                    Inline = true
                    OnToggle = (fun () -> ())
                    OnSelect = ignore
                  }
                ]
              ]
              Html.div [
                prop.children [
                  Html.h3 [
                    prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                    prop.text "All platforms"
                  ]
                  Ui.Dropdown.view {
                    Items = [
                      {Id = "github"; Label = "GitHub"; Icon = Ui.Icon.Name.GitHub}
                      {Id = "twitter"; Label = "Twitter"; Icon = Ui.Icon.Name.Twitter}
                      {Id = "linkedin"; Label = "LinkedIn"; Icon = Ui.Icon.Name.LinkedIn}
                      {Id = "youtube"; Label = "YouTube"; Icon = Ui.Icon.Name.YouTube}
                      {Id = "facebook"; Label = "Facebook"; Icon = Ui.Icon.Name.Facebook}
                      {Id = "twitch"; Label = "Twitch"; Icon = Ui.Icon.Name.Twitch}
                      {Id = "devto"; Label = "DevTo"; Icon = Ui.Icon.Name.DevTo}
                      {Id = "codewars"; Label = "Codewars"; Icon = Ui.Icon.Name.CodeWars}
                      {Id = "freecodecamp"; Label = "freeCodeCamp"; Icon = Ui.Icon.Name.FreeCodeCamp}
                      {Id = "gitlab"; Label = "GitLab"; Icon = Ui.Icon.Name.GitLab}
                      {Id = "hashnode"; Label = "Hashnode"; Icon = Ui.Icon.Name.Hashnode}
                      {Id = "stackoverflow"; Label = "Stack Overflow"; Icon = Ui.Icon.Name.StackOverflow}
                      {Id = "frontend-mentor"; Label = "Frontend Mentor"; Icon = Ui.Icon.Name.FrontendMentor}
                    ]
                    SelectedId = None
                    Open = true
                    Placeholder = "Select platform"
                    Inline = true
                    OnToggle = (fun () -> ())
                    OnSelect = ignore
                  }
                ]
              ]
            ]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Tabs"
          ]
          Html.div [
            prop.className "flex flex-col gap-6"
            prop.children [
              Html.h3 [
                prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                prop.text "Default (Links active)"
              ]
              Html.div [
                prop.className "w-full"
                prop.children [
                  Ui.Tabs.view {Active = Ui.Tabs.TabId.Links; OnSelect = ignore; Layout = Ui.Tabs.Layout.Compact}
                ]
              ]
              Html.h3 [
                prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                prop.text "Active (Profile active)"
              ]
              Html.div [
                prop.className "w-full"
                prop.children [
                  Ui.Tabs.view {Active = Ui.Tabs.TabId.Profile; OnSelect = ignore; Layout = Ui.Tabs.Layout.Compact}
                ]
              ]
            ]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Image Upload"
          ]
          Html.div [
            prop.className "flex flex-col gap-8"
            prop.children [
              Html.h3 [
                prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                prop.text "Upload Image (no current image)"
              ]
              Ui.ImageUpload.view {ImageUrl = None; OnSelected = ignore}

              Html.h3 [
                prop.className "text-preset-3-semibold mb-3 lg:mb-4"
                prop.text "Change Image (with current image)"
              ]
              Ui.ImageUpload.view {ImageUrl = Some "https://via.placeholder.com/200"; OnSelected = ignore}
            ]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Icons"
          ]
          Html.div [
            prop.className "flex flex-wrap gap-6 items-center justify-around"
            prop.children [
              Ui.Icon.view Ui.Icon.Name.GitHub "GitHub" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Twitter "Twitter" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.LinkedIn "LinkedIn" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.YouTube "YouTube" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Facebook "Facebook" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Twitch "Twitch" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.DevTo "DevTo" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.CodeWars "Codewars" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.FreeCodeCamp "freeCodeCamp" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.GitLab "GitLab" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Hashnode "Hashnode" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.StackOverflow "Stack Overflow" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.FrontendMentor "Frontend Mentor" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.ChevronDown "ChevronDown" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Link "Link" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Email "Email" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.Password "Password" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.UploadImage "UploadImage" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.ArrowRight "ArrowRight" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.ChangesSaved "ChangesSaved" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.LinksHeader "LinksHeader" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.ProfileHeader "ProfileHeader" (Some "w-6 h-6")
              Ui.Icon.view Ui.Icon.Name.PreviewHeader "PreviewHeader" (Some "w-6 h-6")
            ]
          ]
        ]
      ]

      Html.section [
        prop.children [
          Html.h2 [
            prop.className "text-preset-2 mt-2 mb-6"
            prop.text "Platform Links"
          ]
          Html.div [
            prop.className "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4"
            prop.children (
              let platforms = [
                Shared.SharedModels.Platform.GitHub
                Shared.SharedModels.Platform.Twitter
                Shared.SharedModels.Platform.LinkedIn
                Shared.SharedModels.Platform.YouTube
                Shared.SharedModels.Platform.Facebook
                Shared.SharedModels.Platform.Twitch
                Shared.SharedModels.Platform.DevTo
                Shared.SharedModels.Platform.CodeWars
                Shared.SharedModels.Platform.FreeCodeCamp
                Shared.SharedModels.Platform.GitLab
                Shared.SharedModels.Platform.Hashnode
                Shared.SharedModels.Platform.StackOverflow
                Shared.SharedModels.Platform.FrontendMentor
              ]
              platforms
              |> List.map (fun p -> Ui.PlatformLink.view {Platform = p; Url = "#"})
            )
          ]
        ]
      ]
    ]
  ]
