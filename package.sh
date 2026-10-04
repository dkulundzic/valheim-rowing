#!/bin/sh
# Builds the mod and packs a Thunderstore-format zip into dist/:
#   RowingMod-<version>.zip with manifest.json, icon.png, README.md and RowingMod.dll at the root.
# Friends import it in r2modman via Settings > Profile > Import local mod.
set -e
cd "$(dirname "$0")"

plugin_version=$(sed -n 's/.*const string Version = "\(.*\)";.*/\1/p' src/RowingPlugin.cs)
manifest_version=$(sed -n 's/.*"version_number": "\(.*\)".*/\1/p' package/manifest.json)
if [ "$plugin_version" != "$manifest_version" ]; then
    echo "Version mismatch: src/RowingPlugin.cs has $plugin_version, package/manifest.json has $manifest_version" >&2
    exit 1
fi

mise exec dotnet@8 -- dotnet build RowingMod.csproj -c Release --nologo -v quiet

staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT
cp package/manifest.json package/icon.png package/README.md bin/Release/RowingMod.dll "$staging/"
# Released sounds (package/sounds), loaded from a "sounds" folder next to the DLL. Sounds only being tried out
# locally live in assets/sounds and aren't packed.
contents="manifest.json icon.png README.md RowingMod.dll"
if ls package/sounds/*.wav >/dev/null 2>&1; then
    mkdir -p "$staging/sounds"
    cp package/sounds/*.wav "$staging/sounds/"
    contents="$contents sounds"
fi

mkdir -p dist
zip_path="$PWD/dist/RowingMod-$plugin_version.zip"
rm -f "$zip_path"
# COPYFILE_DISABLE and -X keep macOS metadata (._ files, extended attributes) out of the zip.
(cd "$staging" && COPYFILE_DISABLE=1 zip -X -q -r "$zip_path" $contents)
echo "Packed $zip_path"
unzip -l "$zip_path"
