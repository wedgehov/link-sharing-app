{
  sources ? import ./nix,
  system ? builtins.currentSystem,
  pkgs ? import sources.nixpkgs {
    inherit system;
    config = { };
    overlays = [ ];
  },
}:
let
  pname = "LinkSharingApp";
  version =
    let
      clean =
        str:
        pkgs.lib.pipe str [
          (pkgs.lib.removePrefix "v")
          (pkgs.lib.removeSuffix "\n")
        ];
      version = builtins.readFile ./.version;
    in
    clean version;
    dotnet-sdk = pkgs.dotnet-sdk_10;
in
rec {
  shell = pkgs.mkShell {
    name = "LinkSharingApp";
    nativeBuildInputs = [
      dotnet-sdk
      pkgs.bun
      pkgs.npins
      pkgs.nixfmt-rfc-style
    ];
    shellHook = ''
      mkdir -p dist/public
    '';
    DOTNET_ROOT = dotnet-sdk;
    DOTNET_CLI_TELEMETRY_OPTOUT = "true";
    NPINS_DIRECTORY = "nix";
  };
}
