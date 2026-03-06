module Routing

// 1. The Page DU, now in its own module.
type Page =
  | LinksPage
  | ProfilePage
  | PreviewPage of slug: string
  | LoginPage
  | RegisterPage
  | DevGallery

// 2. A function to convert a Page to a URL path.
let pageToPath (page: Page) : string list =
  match page with
  | LinksPage -> ["links"]
  | ProfilePage -> ["profile"]
  | PreviewPage slug -> ["preview"; slug]
  | LoginPage -> ["login"]
  | RegisterPage -> ["register"]
  | DevGallery -> ["dev"]

// 3. A function to parse a URL path into a Page.
let pathParser (path: string list) : Page =
  match path with
  | []
  | [""]
  | ["links"] -> LinksPage
  | ["profile"] -> ProfilePage
  | "preview" :: slug :: [] -> PreviewPage slug
  | ["login"] -> LoginPage
  | ["register"] -> RegisterPage
  | ["dev"] -> DevGallery // We can add the env var check here or in the update function
  | _ -> LinksPage // Default to LinksPage for any unknown route
