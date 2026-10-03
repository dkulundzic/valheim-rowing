# RowingMod (Valheim)

A BepInEx 5 mod that lets passengers row a ship to make it faster.

## How it plays

- A passenger sits on a ship seat (a `Chair`, not the helm) and presses the row key (default **H**). A stroke bar (IMGUI) shows a marker sweeping toward a green sweet spot.
- **Shared beat (1.1):** the whole ship rows to one beat.
  - **The owner keeps it:** in `ShipRowing.UpdateBeat`, while anyone is aboard (`!Ship.CanBeRemoved()`), the owner writes ZDO longs `RowingMod_BeatTime` (latest beat, network-clock ms from `ZNet.GetTimeSeconds`) and `RowingMod_BeatPeriod` (ms).
  - **Tempo:** at each beat the owner picks the next period from speed: `StrokeCycleStill` (1.8 s) when still, down to `StrokeCycleTopSpeed` (1.2 s) at top speed.
  - **Clients:** extend the schedule with `GetBeat`. A stale schedule (more than 2 s past the next beat, e.g. after sleeping) restarts.
  - **A press belongs to the nearest beat:** within ±`SweetSpotWidth`/2 of the beat it's strong, otherwise off-beat.
  - **One stroke per beat;** a second press is mashing (stamina spent, no stroke).
- **Sync and clash,** computed by the owner per beat in `ApplyBeat`:
  - **Sync:** strong strokes on the same beat each get `+SyncBonusPerRower × (n−1)`, capped at `MaxSyncBonus`.
  - **Clash:** if anyone hit the beat, each off-beat stroke on it adds no boost and adds `ClashBrake` to a separate brake pool. The brake only slows the ship and never reverses it. If nobody hit the beat, off-beat strokes are weak (`WeakStrokeFactor`).
  - **Late strokes:** strokes arrive one at a time, so the owner re-applies the difference for the beat.
- **Stamina:** each stroke costs `StaminaPerStroke` × the headwind multiplier. An exhausted rower can't row.
  - Headwind multiplier: `1 + HeadwindStaminaFactor × headwind × wind intensity`. `headwind` is `max(0, dot(windDir, −rowing direction))` on the horizontal plane (`EnvMan.GetWindDir` points where the wind blows to). A tailwind gives no discount. The bar title shows "Headwind: +N% stamina" above 5%.
- **When rowing works:** always, at every speed setting including `Stop` and with the sail open. While backing (`Back`) strokes push backward, otherwise forward. Changing direction clears the boost (`ShipRowing.m_lastDirection`). Stop was blocked until 1.0.1; the user changed the rule because rowing a stopped boat makes sense, and a seated passenger can't change the speed setting.
- **Who rows:** only passengers on rowing benches (`Chair` with `attach_sitship`): Karve 2, Longship 4. Not the back seat, not Hold fast spots, not the helmsman.
- **Stroke strength:** timing × speed factor. The speed factor is `1 − (v / top)²`, where `v` is the ship's speed in the rowing direction and `top` is its top sail speed × `TopSpeedMultiplier`. It's applied every physics step, so rowing can never push a ship past its top sail speed. The sail setting doesn't change stroke strength.
- **Top sail speed:** the game has no top-speed setting. The mod estimates each ship's top sail speed from its prefab values (`m_sailForceFactor`, `m_dampingForward`, `m_force`): `sqrt(best sail push / (m_dampingForward × submersion))`. The best sail push is about 0.737 × `m_sailForceFactor`, at about a 65° wind. Submersion is `g / (50 × m_force)`. Each ship's value is logged on load: Karve 7.4 m/s, Longship (`VikingShip`) 9.6 m/s.
- **Multiplayer:**
  - **Broadcast:** the rower's client broadcasts RPC `RowingMod_Stroke2(float quality, long beatMs)` to everybody (`ZNetView.Everybody`, which also runs locally). Every client records strokes per beat for the "In sync ×N" / "Clash!" messages; only the owner applies force.
  - **Legacy:** the 1.0 RPC `RowingMod_Stroke(float)` is still accepted from old rowers, with no sync or clash. A 1.0 owner ignores 1.1 strokes.
  - The owner adds a boost that fades over `StrokeFade`, capped at `MaxBoost`. It applies the boost in a postfix on `Ship.CustomFixedUpdate` as `m_backwardForce * boost`, pushed through the centre of mass.
  - The owner syncs the boost to the ZDO key `RowingMod_Boost`, so every rower sees the crew's boost.
  - The owner also writes its session ID to the ZDO key `RowingMod_Owner`. If that doesn't match the ZDO's owner for 3 s, the owner is vanilla, and rowers get a warning that their strokes won't count.
- **Snackbar:** a toast fades in above the bar when you sit down ("Rowing ready") and for the vanilla-owner warning ("Your strokes won't count" / "count again").

## Layout

- `src/RowingPlugin.cs`: plugin entry point and config entries.
- `src/ShipRowing.cs`: component added to every ship (Harmony postfix on `Ship.Awake`). It receives strokes, applies force and logs how many seats the ship has.
- `src/ShipOars.cs`: oar visuals on every client, added to every ship next to `ShipRowing`.
  - **Placement:** the oarlock (rowing pivot) is on the gunwale, found by casting down from above at 5 cm steps outward from the seat; the outermost hit is the gunwale top. Each oar's seat and oarlock positions are logged.
  - **Occupancy:** every rowing bench always shows an oar: stowed inside the hull (flat, 0.35 m in from the narrowest gunwale along its length, parallel to the side, blade toward the stern; centred on its bench, but slid toward the ship's middle until the hull is wide enough at both ends and the middle, because the Karve's benches are near the bow and a centred 3.2 m oar poked through the narrow end) when the bench is empty, out in the water when it's occupied, with a 0.8 s blend between the two. Occupancy also decides whose strokes animate it: the local player when attached to the seat, remote players when within 0.5 m of the seat's attach point, since attach state isn't synced.
  - **Look:** primitives with the hull's material and no colliders.
  - **Animation:** pitch follows the water level; strokes from the broadcast RPC animate drive, recovery and settle.
  - Config `UI.ShowOars`.
- `src/RowingSounds.cs`: sounds and the blade spray.
  - **Mixer:** everything plays through the game's SFX mixer group, taken from a game sfx prefab's AudioSource.
  - **3D, heard by every crew member with the mod:** each stroke's sounds play at that oar, since strokes are broadcast. Only the beat tick is local.
  - **Beat tick:** generated, heard only by the local seated rower (`Sounds.BeatTick`).
    - **Toggle:** holding the row key for 3 s toggles it (`Rower.UpdateHold`, saved to the config). The press that starts the hold still counts as a stroke.
    - **UI:** a label beside the bar shows "Beat tick on/off · hold H 3 s", the message line counts down after 0.4 s of holding, and the "Rowing ready" snackbar mentions the hold.
  - **Each stroke is layered** (the user wants believable, non-repeating sounds; values are defaults, each a `Sounds.*` setting):
    - **Splash** (`fx_footstep_water`, 7 wading clips): random clip, pitch 0.9–1.12, volume jitter.
    - **Run-off** (`sfx_ship_waterimpact` after-splash): a 0.7 s slice, 50% of strokes.
    - **Sync** (`sfx_land_water` pitched ×0.8–0.9): a deeper splash added when the stroke lands on a beat others hit, fuller with more rowers.
    - **Drips** (`sfx_footstep_swim`): a 0.6 s slice on the recovery, 60% of strokes.
    - **Oarlock knock** (`fx_footstep_wood_jog`): 40% of strokes.
    - **Creak** (bog witch creak and the ship's sail-change vibration, pooled): a 0.9 s slice on 60% of strong strokes, at most once per 0.7 s per ship, louder with crew boost.
    - **Clash** (`sfx_wood_blocked`) replaces all of the above.
  - **Settings:** each names one or more prefabs (comma-separated, clips pooled). Empty means the default; `generated` means a sound made in code.
  - **Spray** (`UI.ShowSplashes`, `UI.SplashEffect`, default `fx_footstep_water`): the prefab's particles, with its ZSFX and AudioSources removed. It's instantiated under an inactive holder so the effect's own sound never wakes, and skipped if the prefab is networked or has no particles.
  - **Discovery:** `Debug.LogSoundCandidates` logs candidate sfx/vfx/fx prefabs once per session, with clips, particle counts and whether they're networked, including footstep, water and ship effect prefabs.
  - **Generated clips** use `AudioClip.Create` with a PCM reader callback, because Unity 6's `SetData` has a `ReadOnlySpan` overload that net48 can't compile against.
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
- Build: `mise exec dotnet@8 -- dotnet build RowingMod.csproj -c Release`. Unity 6 modules (e.g. AudioModule) target .NET Standard 2.1, so the project also references the game's own `lib/netstandard.dll`. Output is `bin/Release/RowingMod.dll`. If the Valheim install has `BepInEx/plugins`, the build also copies the DLL to `BepInEx/plugins/RowingMod/`.
- Decompile one more type: `DOTNET_ROLL_FORWARD=Major mise exec dotnet@8 -- .tools/ilspycmd -t <Type> lib/<dll> -o decompiled/<name>`. `DOTNET_ROLL_FORWARD` is needed because the tool targets .NET 6.

## Game facts learned from the decompiled code

- `Ship.CustomFixedUpdate(float)` runs physics only on the owner. `Ship.Speed` is Stop/Back/Slow/Half/Full. `IsSailUp()` and `GetSpeedSetting()` are public, and so are `m_backwardForce`, `m_waterLevelOffset` and `m_disableLevel`. `m_body` and `m_nview` are private, so the mod gets them with `GetComponent`.
- `Player.SetControls` stands a seated player up on any movement, attack, block, jump or crouch input. The row key must not be one of those.
- Valheim uses Unity's new Input System, so read keys with `ZInput`, not `UnityEngine.Input`.
- Free default keys: B, H, J, K, L, N, O, P, U, Y, Z. F is the guardian power, R is hide weapons, X is sit and T is emote.
- UI checks: `Console.IsVisible()`, `Chat.instance.HasFocus()`, `TextInput.IsVisible()`, `Menu.IsVisible()` and `InventoryGui.IsVisible()`.
- Ship seat spots are all `Chair` components, including standing **Hold fast** spots (`m_name` `$ship_holdfast`; the English text "Hold fast" is in `resources.assets`). Confirmed from the log, every spot is a `Chair`:
  - **Rowing benches,** with animation `attach_sitship`: Karve has 2 (`sit_box_fl`, `sit_box_fr`) and Longship (`VikingShip`) has 4.
  - **A back seat on the centre line,** with animation `attach_chair` (`sit_box_back` / `sit_box (4)`).
  - **Hold fast spots:** `$ship_holdfast`, with `attach_mast` / `attach_dragon`.
  - `ShipRowing.IsRowingSeat` accepts only `attach_sitship`, as the user decided: "Karve has two seats at the front, Longship four".
- Ship ownership: the server gives an unowned object to the first client whose active area it is in (`ZDOMan.ReleaseNearbyZDOS`). `Ship.UpdateOwner` hands it on only when the owner is no longer in the boat. Taking the helm (`ShipControlls.RPC_RequestControl`) sets `s_user` but does **not** change the owner. So rowing only works when the owner has the mod; strokes sent to a vanilla owner are dropped. The dedicated server never needs the mod.
- The HUD stamina, eitr and adrenaline bars are `Hud.m_staminaBar2Root`, `m_eitrBarRoot` and `m_adrenalineBarRoot`. The stroke bar is placed above whichever is highest.

## Environment

- Valheim install (Steam, external drive): `/Volumes/CORSAIR/SteamLibrary/steamapps/common/Valheim`.
- The drive is exFAT, so macOS creates `._*` AppleDouble files. If BepInEx warns about `._*.dll`, run `dot_clean` on the folder.
- The game is a universal binary (x86_64 + arm64). Since 2026-10-03 it runs **natively as arm64** with BepInEx:
  - **BepInEx:** `BepInEx.dll`, `BepInEx.Preloader.dll`, `BepInEx.Harmony.dll`, `HarmonyXInterop.dll` and `0Harmony20.dll` in `BepInEx/core` are built from the BepInEx `v5-lts` branch (commit `f4c1b11`, cloned to `.tools/BepInEx-v5lts`). Build: `mise exec dotnet@8 -- dotnet build BepInEx.Preloader/BepInEx.Preloader.csproj -c Release`. It includes PR #1402 (`AppleSiliconDetourFix`) and PR #1288.
  - **Doorstop:** `libdoorstop.dylib` is 4.6.0 from the UnityDoorstop `ci` release; it exports `doorstop_jit_memcpy`, which #1402 needs.
  - **Unchanged:** Harmony and MonoMod, so BepInEx 5 mods stay compatible.
  - **Launch script:** `run_bepinex.sh` is back to the shipped version (`ARCHPREFERENCE="arm64,x86_64"`, `exec arch -e …`).
  - **Why the stock 5.4.23.5 release fails on arm64:** its old MonoMod can't write detours into macOS MAP_JIT memory (NRE in `DetourHelper.GetIdentifiable`).
  - **Check the arch:** `vmmap $(pgrep -f MacOS/Valheim) | grep "Code Type"`.
  - **Roll back to Rosetta:** restore `BepInEx/core`, `libdoorstop.dylib`, `.doorstop_version` and `run_bepinex.sh` from `Valheim/_rosetta_backup/`. The backup's script forces `arch -x86_64`.
  - Updating BepInEx would overwrite these files.
- Joining the crew server: native arm64 takes about 9 s from connecting to spawning (minimap 3.3 s). Under Rosetta it took about 52 s (minimap 13 s), and macOS showed "not responding" meanwhile.
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
- [x] Speed-based strength, backing and headwind stamina are playtested.
- [x] Rowing at Stop, the shared beat with speed-based tempo, sync/clash, Hold fast and back-seat exclusion, and oars (gunwale placement, stowing that fits the Karve) are playtested (2026-10-03).
- [x] Layered stroke sounds and spray are playtested; the user is happy with the sounds as they are. The spray uses `fx_footstep_water` (4 particle systems, not networked); `fx_land_water` has a bigger spray.
- [ ] Hold-to-toggle beat tick: built, not yet checked in game.
- [ ] **Next, 1.1.0:** discuss oar braking and a top-down crew GUI, then release 1.1.0, bundling everything since 1.0.0; 1.0.1 was never published.
- [x] Switched to native arm64 (see Environment). Joining is about 6× faster.
- [ ] Playtest and tune `StrokeStrength`, `MaxBoost`, `StrokeCycle` and `SweetSpotWidth`. Then test in multiplayer with someone else rowing while you steer.
- [x] Playtested on the crew server: rules, headwind stamina and the stroke bar layout all work.
- [x] Version 1.0.0 is packaged for friends with `./package.sh`, and the git repo is set up.
- [ ] Later: possibly gamepad support, publishing on Thunderstore, and checking the logged `top sail speed` values against real speeds.
