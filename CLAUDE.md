# RowingMod (Valheim)

A BepInEx 5 mod that lets passengers row a ship to make it faster.

## How it plays

- A passenger sits on a ship seat (a `Chair`, not the helm) and presses the row key (default **H**). A stroke bar (IMGUI) shows a marker sweeping toward a green sweet spot.
- **Shared beat (1.1):** the whole ship rows to one beat.
  - **The owner keeps it:** in `ShipRowing.UpdateBeat`, while anyone is aboard (`!Ship.CanBeRemoved()`), the owner writes ZDO longs `RowingMod_BeatTime` (latest beat, network-clock ms from `ZNet.GetTimeSeconds`) and `RowingMod_BeatPeriod` (ms).
  - **Tempo:** at each beat the owner picks the next period from speed: `StrokeCycleStill` (1.8 s) when still, down to `StrokeCycleTopSpeed` (1.2 s) at top speed.
  - **Clients:** extend the schedule with `GetBeat`. A stale schedule (more than 2 s past the next beat, e.g. after sleeping) restarts.
  - **A press belongs to the nearest beat:** within ±(green zone width)/2 of the beat it's strong, otherwise off-beat. On this branch the width comes from the Rowing skill (`RowingSkill.SweetSpotWidth`); `Timing.SweetSpotWidth` is removed.
  - **One stroke per beat;** a second press is mashing (stamina spent, no stroke).
- **Helm calls** (from `feature/helmsman-beat`, playtested and merged into main on 2026-10-05):
  - **Keys:** `CrewPanel.Update` reads the helm keys (U/N tempo, J "Hold water!" call, H drum) and sends RPC `RowingMod_Helm(int)` to the owner.
  - **State:** the owner stores ZDO `RowingMod_Tempo` (-1/0/1) and `RowingMod_HoldWater` (ms of the last call).
  - **Effect:** `TempoMs` scales the speed-based period by `Helm.EasyTempoFactor` 1.25 / `HardTempoFactor` 0.8.
  - **Feedback:** rowers get snackbars per call, and the panel footer shows "Beat: Easy/Steady/Hard".
  - **Ramming speed (K) was removed from main** right after the merge, at the user's request: it comes back together with the drum rhythms (`feature/ramming-drum`). Helm command 3 is kept free for it. **When merging `feature/ramming-drum` into main, revert the removal commit on main first** (`git revert 85ba936`); otherwise git keeps main's deletion of the ramming code that the branch didn't change.
- **Sync and clash,** computed by the owner per beat in `ApplyBeat`:
  - **Sync:** strong strokes on the same beat each get `+SyncBonusPerRower × (n−1)`, capped at `MaxSyncBonus`.
  - **Clash:** if anyone hit the beat, each off-beat stroke on it adds no boost and adds `ClashBrake` to a separate brake pool. The brake only slows the ship and never reverses it. If nobody hit the beat, off-beat strokes are weak (`WeakStrokeFactor`).
  - **Late strokes:** strokes arrive one at a time, so the owner re-applies the difference for the beat.
- **Stamina:** every cost goes through one chain in `StaminaCost.Multiplier`: **cost = base × (1 + load) × relief**. An exhausted rower can't row.
  - **Base:** `StaminaPerStroke` per stroke, `Brake.StaminaPerSecond` × dt while braking.
  - **Load:** hard conditions, their extra costs **added** and capped at `Stamina.MaxLoad` (1.5): the headwind (strokes only). Weather and cold (`feature/weather-stamina`, `feature/cold-stamina`) must join here when merged, and ramming's ×2 (`feature/ramming-drum`) as a separate effort multiplier between load and relief.
  - **Relief:** the rower's condition and practice, **multiplied** so they never reach zero: Rested, the Rowing skill (`RowingSkill.StaminaRelief`).
  - **Why:** the user asked (2026-10-05) to combine every modifier into "a clean chain, so everything's taken into account and calculated in a logical manner", instead of stacking multipliers that could reach ~7×.
  - **UI:** `StaminaCost.Describe` puts one line above the stroke bar's title, e.g. "Stamina ×1.30 (headwind +70%, rested -10%, Rowing skill -15%)", parts under 5% left out.
  - **Rested:** rowers with the Rested buff (`SEMan.s_statusEffectRested`) pay `Stamina.RestedDiscount` less (10% by default; the user asked for a small bonus) for strokes and braking.
  - Headwind load: `HeadwindStaminaFactor × headwind × wind intensity`. `headwind` is `max(0, dot(windDir, −rowing direction))` on the horizontal plane (`EnvMan.GetWindDir` points where the wind blows to). A tailwind gives no discount.
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
  - **3D, heard by every crew member with the mod:** each stroke's sounds play at that oar, since strokes are broadcast.
  - **War drum** (replaced the private beat tick on 2026-10-04, at the user's request):
    - **Who controls it:** the helmsman presses `Controls.DrumKey` (H) at the helm. That sends RPC `RowingMod_Drum(bool)` to the owner, who stores it in ZDO bool `RowingMod_Drum`. It's off by default.
    - **Playback:** every client plays it from the ship (`ShipOars.UpdateDrum`) on each beat from the shared schedule, while someone is aboard, accenting every fourth beat.
    - **Sound:** a real dundunba hit, `assets/sounds/dundun.wav`. It's CC0, by JIMMYJAMES112 on Freesound (see `samples/SOURCES.md`).
      - **Loading:** from a `sounds` folder next to the DLL (`LoadBundledWav`).
      - **Not released or installed:** the dundun waits in `assets/sounds/`. The user kept it out of 1.2.0 and removed it from the local game too, so local = 1.2.0 with the generated drum. The csproj and `package.sh` only use `package/sounds` (empty now). To use the dundun, move it to `package/sounds/` and add the credit to the package README.
      - **Variation:** accents are pitched 0.92–0.95, other beats 0.98–1.04, with volume jitter.
      - **Fallback:** the generated tom (`MakeDrum`), used if the file is missing or `Sounds.DrumSound = generated`. Volume `Sounds.DrumVolume`, heard up to 70 m.
      - **History:** the user found synthesized drum patterns "boring" and "not good enough" and asked for a dundun; richer patterns are shelved for now.
    - **UI:** the panel footer shows "Drum: on/off", plus the key for the helmsman.
  - **Wakes** (`UI.ShowWakes`, `UI.WakeEffect`, default `vfx_water_surface` at half scale): three ripples along each stroke's drive at the blade on the water line, and every ~0.5 s while braking above 1 m/s.
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
- `src/CrewPanel.cs`: a top-down ship view in the bottom-right corner, for rowers on a bench and the helmsman (`Player.GetControlledShip`).
  - **Hull:** the outline is traced per ship type from gunwale probes (`ShipOars.TraceHull`) and rendered once into a texture. It pulses on each beat.
  - **Benches:** empty rings, occupied discs, and a stroke flash coloured green (strong), yellow (weak), red (clash) or gold (sync). Kinds upgrade as later strokes for the same beat arrive, in `ShipOars.OnStroke`. Your own bench has a white ring.
  - **Mini oars** are projected from the real 3D oars (`ShipOars.GetBenches`).
  - **Helm:** the helmsman is a diamond at `ShipControlls.m_attachPoint`, with no oar. It's an outline when empty and filled when steered (`HaveValidUser`/`GetUser`), with a white ring when it's you.
  - **Also:** a crew-boost bar along the centre line, and a footer with the speed setting ("Paddling · Crew boost N%") and "In sync ×N".
  - Config: `UI.ShowCrewPanel`, and `UI.CrewNames` (off by default).
- `src/RowingUI.cs`: IMGUI scaling and helpers. All mod UI draws in virtual pixels scaled by `UI.Scale` (0 = automatic, `Screen.height / 1080`, at least 1). `DrawLine` builds its own rotation matrix, because `GUIUtility.RotateAroundPivot` takes the pivot in unscaled pixels.
- `src/RowingSkill.cs` (branch `feature/rowing-skill`): a custom "Rowing" skill.
  - **Identity:** SkillType = |stable hash of "com.dkulundzic.rowingmod.skill.rowing"|.
  - **Patches:** `Skills.IsSkillValid` (so it loads from saves), `Skills.GetSkillDef` (definition with a generated oar icon), and `Localization.SetupLanguage` (adds "skill_<id>" = "Rowing" via private `AddWord`). `Localization` lives in `assembly_guiutils.dll`, which is now referenced.
  - **Practice:** `RaiseSkill` per stroke (1 strong, 0.3 weak).
  - **Effects:** green zone = lerp(`Skill.SweetSpotAtLevel0` 0.12, `SweetSpotAtLevel100` 0.28, f), so it's narrower than the old fixed 0.2 below level 50, as the user asked. Stamina ×(1 − 0.3f), strength ×(1 + 0.15f), where f is the level / 100.
  - **Strength reaches the owner:** the stroke's quality carries the skill's strength bonus, so the owner sums per-stroke quality (`BeatStrokes.QualityBySender`) instead of counting strokes.
- `src/Rower.cs`: local-player side. Seat detection, key input via `ZInput.GetKeyDown`, timing, stamina and the stroke bar.
- `lib/`: game and Unity DLLs copied from `valheim.app/Contents/Resources/Data/Managed`. They're not committed (Iron Gate's code).
- `decompiled/`: the game's code decompiled by ilspycmd, for reading only. It's not compiled or committed.
- `.tools/ilspycmd`: decompiler, version 8.2.0.7535. Newer versions don't install on .NET 8.
- `RULES.md`: plain-language rules for players, with the default numbers. Keep it in sync when rules or defaults change.
- `docs/proposals/`: agreed designs not yet built (e.g. `oar-braking.md`).
- `package/`: Thunderstore files: `manifest.json`, `README.md` (player-facing) and `icon.png`. `make_icon.py` regenerates the icon with Pillow.
- `package.sh`: builds Release and writes `dist/RowingMod-<version>.zip` (not committed).

## Performance

Checked on 2026-10-05 at the user's request (no profiling; nothing showed up in game). Keep these when changing code:

- **IMGUI** (`Rower`, `CrewPanel`): `useGUILayout = false`, and `OnGUI` draws only on `EventType.Repaint` (`RowingUI.IsRepaint`). Styles are cached statics made on first draw (`RowingUI.LabelStyle`), text is measured with one reused `GUIContent`, and the panel's ship and seat check are found in `Update`, re-searching chairs only when the seat changes. Don't allocate in `OnGUI`.
- **Sounds:** `RowingSounds.PlayAt` reuses a pool of 24 `AudioSource`s (`Voice`); when all are busy, the one closest to finishing is cut. Priorities: drum 64, other mod sounds 128 (the game's default). They were 160/200 for a day, to yield voices to the game; raised after the user heard drum rhythms that didn't change and a late drum start on the crew server (suspected voice stealing; not confirmed).
- **Far ships:** `ShipOars.Update` does nothing for ships more than 90 m from the local player, and looks for bench occupants only while a player is within 15 m of the ship.
- **Clock:** `ShipRowing.NowMs()` is the network clock smoothed: it runs on real time and eases toward `ZNet.GetTimeSeconds()` (2 s time constant, never backward, at least half speed), snapping only on gaps over 1.5 s. A client's network clock is overwritten by the server's every 2 s and lags on frame hitches; the user's drum log on the crew server (2026-10-05) showed ~40 jumps in 2 minutes, mostly backward, up to 0.7 s, which broke the drum's rhythm.
- **Once per ship:** gunwale raycasts, the hull outline texture and the top-speed estimate.

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
- [x] Hold-to-toggle beat tick: playtested, then **replaced by the helmsman's war drum** (2026-10-04).
- [x] The crew panel and UI scale are playtested; the user says it "looks great" as is (2026-10-04).
  - **Polish done after review:**
    - all mod text has a drop shadow (`RowingUI.Label`);
    - stowed oars in the panel are thin and faint;
    - oar room is 1.2 m, with oars clipped at the panel edge (`RowingUI.ClipLine`), so the hull is bigger;
    - the boost bar is outlined.
- [ ] The helmsman diamond and the speed setting in the panel footer are built; the user will test them.
- [x] **Oar braking** (`docs/proposals/oar-braking.md`): solo Karve braking feels good at the default `Brake.Strength` 0.1 (playtest 2026-10-04). The brake sounds were inaudible at first; they now have a catch splash at any speed, louder rushing water from the loud start of the swim clips, and a 0.15 m/s cutoff. The louder sounds aren't re-tested yet.
  - **Rower:** `Rower.UpdateBrake` holds J; it broadcasts `RowingMod_Brake(bool)` with a 1 s heartbeat and drains stamina.
  - **Ship:** `ShipRowing` tracks brakers and drops them after 2.5 s of silence. The owner's `ApplyBrakes` applies drag at each oarlock (`ShipOars.TryGetOarlock`); the drag never exceeds the speed.
  - **Oars:** `ShipOars` swings the oar square and dug in, with a gurgle and spray from frame-to-frame speed. The panel shows the bench blue.
  - **To tune by playtest:** `Brake.Strength`, and how strong the turning torque is.
- [x] **Released 1.1.0** on GitHub (2026-10-04), bundling everything since 1.0.0; 1.0.1 was never published. `Debug.LogSoundCandidates` is off by default for release.
- [x] **Released 1.2.0** on GitHub (2026-10-04): the helmsman's war drum (generated sound), oar wakes, a 10% Rested discount; the beat tick is removed. The recorded dundun is not included.
- [ ] **Feature branches waiting for playtest** (2026-10-05). Each is branched from main after 1.2.0, builds without warnings, is pushed and isn't merged; none is tested in game. Expect merge conflicts between them in `RowingPlugin.cs`, `Rower.cs` and `CrewPanel.cs`.
  - `feature/speed-gauge`: speed vs top sail speed in the panel.
  - `feature/rhythm-streak`: the crew streak bonus.
  - `feature/tutorial`: first-time snackbars.
  - `feature/voyage-stats`: per-stint summary and lifetime totals.
  - `feature/weather-stamina`: storm and rough-sea cost.
  - `feature/cold-stamina`: Cold and Freezing cost.
  - `feature/colorblind-panel`: Okabe-Ito colours and glyphs.
  - `feature/assisted-rowing`: hold H to auto-row, owner can disallow.
  - `feature/rowing-skill`: a custom Rowing skill; the green zone is narrow at low skill and widens with level (12% at 0, 20% at 50, 28% at 100), as the user asked.
  - `feature/gamepad`: RT row, LT brake.
  - `feature/rower-lean`: experimental body lean.
  - **Not done:** the Drakkar check (the user has none), grunts (waiting for recordings), Thunderstore (needs the user's account), "rowing cools you down" (I recommended skipping it).
- [ ] **Next:** discuss grunting or effort sounds for rowers.
- [x] Switched to native arm64 (see Environment). Joining is about 6× faster.
- [ ] Playtest and tune `StrokeStrength`, `MaxBoost`, `StrokeCycle` and `SweetSpotWidth`. Then test in multiplayer with someone else rowing while you steer.
- [x] Playtested on the crew server: rules, headwind stamina and the stroke bar layout all work.
- [x] Version 1.0.0 is packaged for friends with `./package.sh`, and the git repo is set up.
- [ ] Later: possibly gamepad support, publishing on Thunderstore, and checking the logged `top sail speed` values against real speeds.
