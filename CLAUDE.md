# RowingMod (Valheim)

A BepInEx 5 mod that lets passengers row a ship to make it faster.

## How it plays

- A passenger sits on a ship seat (a `Chair`, not the helm) and presses the row key (default **H**). A stroke bar (IMGUI) shows a marker sweeping toward a green sweet spot.
- **Stroke timing:**
  - A press in the sweet spot is a strong stroke.
  - An early or late press is a weak stroke (`WeakStrokeFactor`).
  - A press before half a cycle (`MinStrokePhase`) is mashing: it costs stamina and makes no stroke.
- **Stamina:** each stroke costs `StaminaPerStroke` × the headwind multiplier. An exhausted rower can't row.
  - Headwind multiplier: `1 + HeadwindStaminaFactor × headwind × wind intensity`. `headwind` is `max(0, dot(windDir, −rowing direction))` on the horizontal plane (`EnvMan.GetWindDir` points where the wind blows to). A tailwind gives no discount. The bar title shows "Headwind: +N% stamina" above 5%.
- **When rowing works:** at every speed setting except `Ship.Speed.Stop`, including with the sail open (Half or Full). While backing (`Back`), strokes push backward. Going from forward to back passes through Stop, which clears the boost.
- **Who rows:** only passengers on ship seats (`Chair`). The helmsman can't row.
- **Stroke strength:** timing × speed factor. The speed factor is `1 − (v / top)²`, where `v` is the ship's speed in the rowing direction and `top` is its top sail speed × `TopSpeedMultiplier`. It's applied every physics step, so rowing can never push a ship past its top sail speed. The sail setting doesn't change stroke strength.
- **Top sail speed:** the game has no top-speed setting. The mod estimates each ship's top sail speed from its prefab values (`m_sailForceFactor`, `m_dampingForward`, `m_force`): `sqrt(best sail push / (m_dampingForward × submersion))`. The best sail push is about 0.737 × `m_sailForceFactor`, at about a 65° wind. Submersion is `g / (50 × m_force)`. Each ship's value is logged on load. With the default field values it's about 4.3 m/s.
- **Multiplayer:**
  - The rower's client sends RPC `RowingMod_Stroke(float quality)` to the ship's owner (`ZNetView.InvokeRPC` routes to the owner).
  - The owner adds a boost that fades over `StrokeFade`, capped at `MaxBoost`. It applies the boost in a postfix on `Ship.CustomFixedUpdate` as `m_backwardForce * boost`, pushed through the centre of mass.
  - The owner syncs the boost to the ZDO key `RowingMod_Boost`, so every rower sees the crew's boost.
  - The owner also writes its session ID to the ZDO key `RowingMod_Owner`. If that doesn't match the ZDO's owner for 3 s, the owner is vanilla, and rowers get a warning that their strokes won't count.
- **Snackbar:** a toast fades in above the bar when you sit down ("Rowing ready"), when rowing becomes possible or blocked (stopping or starting the ship), and for the vanilla-owner warning.

## Layout

- `src/RowingPlugin.cs`: plugin entry point and config entries.
- `src/ShipRowing.cs`: component added to every ship (Harmony postfix on `Ship.Awake`). It receives strokes, applies force and logs how many seats the ship has.
- `src/Rower.cs`: local-player side. Seat detection, key input via `ZInput.GetKeyDown`, timing, stamina and the stroke bar.
- `lib/`: game and Unity DLLs copied from `valheim.app/Contents/Resources/Data/Managed`. They're not committed (Iron Gate's code).
- `decompiled/`: the game's code decompiled by ilspycmd, for reading only. It's not compiled or committed.
- `.tools/ilspycmd`: decompiler, version 8.2.0.7535. Newer versions don't install on .NET 8.
- `RULES.md`: plain-language rules for players, with the default numbers. Keep it in sync when rules or defaults change.
- `package/`: Thunderstore files: `manifest.json`, `README.md` (player-facing) and `icon.png`. `make_icon.py` regenerates the icon with Pillow.
- `package.sh`: builds Release and writes `dist/RowingMod-<version>.zip` (not committed).

## Commands

- `dotnet` comes from mise and isn't on PATH. Run it as `mise exec dotnet@8 -- dotnet …`.
- Release zip for friends: `./package.sh`. It fails if `Version` in `src/RowingPlugin.cs` and `version_number` in `package/manifest.json` differ, so bump both. Friends are on Windows and import the zip in r2modman (Settings → Profile → Import local mod).
- Git: `main` tracks `origin` (git@github.com:dkulundzic/valheim-rowing.git, **public**, MIT license). Keep private details such as server addresses out of commits. Uses conventional commits (`feat:`, `fix:`, `docs:`, `build:`, `chore:`, `refactor:`).
- Build: `mise exec dotnet@8 -- dotnet build RowingMod.csproj -c Release`. Output is `bin/Release/RowingMod.dll`. If the Valheim install has `BepInEx/plugins`, the build also copies the DLL to `BepInEx/plugins/RowingMod/`.
- Decompile one more type: `DOTNET_ROLL_FORWARD=Major mise exec dotnet@8 -- .tools/ilspycmd -t <Type> lib/<dll> -o decompiled/<name>`. `DOTNET_ROLL_FORWARD` is needed because the tool targets .NET 6.

## Game facts learned from the decompiled code

- `Ship.CustomFixedUpdate(float)` runs physics only on the owner. `Ship.Speed` is Stop/Back/Slow/Half/Full. `IsSailUp()` and `GetSpeedSetting()` are public, and so are `m_backwardForce`, `m_waterLevelOffset` and `m_disableLevel`. `m_body` and `m_nview` are private, so the mod gets them with `GetComponent`.
- `Player.SetControls` stands a seated player up on any movement, attack, block, jump or crouch input. The row key must not be one of those.
- Valheim uses Unity's new Input System, so read keys with `ZInput`, not `UnityEngine.Input`.
- Free default keys: B, H, J, K, L, N, O, P, U, Y, Z. F is the guardian power, R is hide weapons, X is sit and T is emote.
- UI checks: `Console.IsVisible()`, `Chat.instance.HasFocus()`, `TextInput.IsVisible()`, `Menu.IsVisible()` and `InventoryGui.IsVisible()`.
- Ships have seats: Karve has 4 and Longship (`VikingShip`) has 7 (confirmed in game).
- Ship ownership: the server gives an unowned object to the first client whose active area it is in (`ZDOMan.ReleaseNearbyZDOS`). `Ship.UpdateOwner` hands it on only when the owner is no longer in the boat. Taking the helm (`ShipControlls.RPC_RequestControl`) sets `s_user` but does **not** change the owner. So rowing only works when the owner has the mod; strokes sent to a vanilla owner are dropped. The dedicated server never needs the mod.
- The HUD stamina, eitr and adrenaline bars are `Hud.m_staminaBar2Root`, `m_eitrBarRoot` and `m_adrenalineBarRoot`. The stroke bar is placed above whichever is highest.

## Environment

- Valheim install (Steam, external drive): `/Volumes/CORSAIR/SteamLibrary/steamapps/common/Valheim`.
- The drive is exFAT, so macOS creates `._*` AppleDouble files. If BepInEx warns about `._*.dll`, run `dot_clean` on the folder.
- The game is a universal binary (x86_64 + arm64), and the Mac is Apple Silicon. With BepInEx it must run as **x86_64 under Rosetta**: BepInEx 5 bundles MonoMod 21.x, which can't detour on arm64 (every Harmony patch fails with an NRE in `DetourHelper.GetIdentifiable`). `run_bepinex.sh` is edited near the end of the file: `ARCHPREFERENCE="x86_64,arm64"` and `exec arch -x86_64 -e …` (the original had `arm64,x86_64` and no `-x86_64`). A BepInEx update would overwrite this edit.
- Joining the crew server under Rosetta takes about 60 s to spawn: about 20 s before the version check, about 18 s of world setup, about 13 s for `Generating new world minimap`, then about 8 s to respawn. macOS shows "not responding" during this; wait it out. The native timing hasn't been measured.
- Code changes need a game restart: BepInEx 5 loads plugins once and Mono can't unload assemblies.
- If the preloader crashes, it writes `preloader_<timestamp>.log` to `valheim.app/Contents/MacOS/`, not to `BepInEx/`. The game's own log is `~/Library/Logs/IronGate/Valheim/Player.log`.
- This session needs access to the Valheim folder: `/add-dir /Volumes/CORSAIR/SteamLibrary/steamapps/common/Valheim`.

## Status (2026-10-03)

- [x] The project builds with no warnings (`net48`, BepInEx.Core 5.4.21 from nuget.bepinex.dev).
- [x] BepInEx **5.4.23.5 macOS universal** is unzipped into the Valheim folder. That added `run_bepinex.sh`, `libdoorstop.dylib`, `.doorstop_version`, `changelog.txt` and `BepInEx/core/`.
- [x] `run_bepinex.sh` has `executable_name="valheim.app"` (the variable name is unchanged in 5.4.23.5).
- [x] Steam launch options are set to `"/Volumes/CORSAIR/SteamLibrary/steamapps/common/Valheim/run_bepinex.sh" %command%`.
- [x] BepInEx loads under Rosetta (Chainloader started, Unity 6000.0.75f1 detected, no corlib errors). `BepInEx/plugins` exists.
  - If the log shows `MissingMethodException` or other errors caused by the game's trimmed system libraries, take `unstripped_corlib` from Thunderstore's BepInExPack_Valheim and point doorstop's DLL search path override at it.
- [x] Rebuilt; the DLL is in `BepInEx/plugins/RowingMod/`. Run `dot_clean -m` on `BepInEx/plugins` after each build.
- [x] Tested on the crew's vanilla dedicated server: sitting, the stroke bar and the speed boost all work.
- [x] The stroke bar overlapped the stamina bar. It now sits above the HUD bars, with config `UI.BarOffset`. **Not yet checked in game.**
- [x] Snackbar notices and the vanilla-owner check are built. **Not yet checked in game.**
- [x] New rules are built: row at any setting except Stop, backward while backing, strength falls with speed up to the top sail speed. **Not yet checked in game.** Check the log for `top sail speed` per ship and compare with real speeds.
- [ ] Performance under Rosetta is poor. Researched plan for native arm64, **waiting for the user's go-ahead**:
  1. Build BepInEx from the `v5-lts` branch. It has PR #1402 (`AppleSiliconDetourFix`, tested on Valheim 6000.0.75f1) and PR #1288.
  2. Replace `BepInEx.dll`, `BepInEx.Preloader.dll`, `BepInEx.Harmony.dll` and `HarmonyXInterop.dll`.
  3. Use the doorstop 4.6.0 CI build (`gh release download ci -R NeighTools/UnityDoorstop -p doorstop_macos_release_4.6.0.zip`), which exports `doorstop_jit_memcpy`.
  4. Remove `arch -x86_64` from `run_bepinex.sh`. Back up `BepInEx/core`, `libdoorstop.dylib` and `run_bepinex.sh` first.
  - Fallback: `Relokk1/valheim-native-arm64`. BepInEx 6 doesn't work on arm64 yet.
- [ ] Playtest and tune `StrokeStrength`, `MaxBoost`, `StrokeCycle` and `SweetSpotWidth`. Then test in multiplayer with someone else rowing while you steer.
- [x] Playtested on the crew server: rules, headwind stamina and the stroke bar layout all work.
- [x] Version 1.0.0 is packaged for friends with `./package.sh`, and the git repo is set up.
- [ ] Later: possibly gamepad support, publishing on Thunderstore, and checking the logged `top sail speed` values against real speeds.
