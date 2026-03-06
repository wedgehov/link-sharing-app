module ApiClient

open Fable.Remoting.Client
open Shared.Api

let AuthApi: IAuthApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<IAuthApi>

let ProfileApi: IProfileApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<IProfileApi>

let LinkApi: ILinkApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<ILinkApi>

let PublicApi: IPublicApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<IPublicApi>
