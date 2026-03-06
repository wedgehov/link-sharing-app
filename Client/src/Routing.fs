module Routing

// 1. The Page DU, now in its own module.
type Page =
  | LinksPage
  | ProfilePage
  | PreviewPage of slug: string
  | LoginPage
  | RegisterPage

// 2. A function to convert a Page to a URL path.
let pageToPath (page: Page) : string list =
  match page with
  | LinksPage -> ["links"]
  | ProfilePage -> ["profile"]
  | PreviewPage slug -> ["preview"; slug]
  | LoginPage -> ["login"]
  | RegisterPage -> ["register"]

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
  | _ -> LinksPage // Default to LinksPage for any unknown route
