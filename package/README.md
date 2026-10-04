# RowingMod

Passengers can row to speed up the ship.

## How to row

1. Sit on a rowing bench on a Karve (2 benches) or Longship (4 benches). The back seat and Hold fast spots don't row. A "Rowing ready" message appears, and the stroke bar shows above your stamina bar.
2. Press **H** when the white marker crosses the green zone. That's the ship's beat, and a strong stroke.
   - **Off the beat:** a weak stroke, or a **clash** if others hit the beat, which brakes the boat a little.
   - **Twice in one beat:** wastes stamina.
   - **Hold J** to brake: the oar digs into the water and slows the ship. Braking on one side turns it.
3. The whole ship shares one beat, slow when still and quicker at speed. Rowers who hit the same beat get a sync bonus of up to +45%.

## Rules

- **When rowing works:** always, at every speed setting, including Stop and with the sail open. While the ship is backing, rowing pushes it backward; otherwise it pushes forward.
- **Who can row:** only passengers in seats. The helmsman can't.
- **Strength by speed:** rowing helps most when the ship is slow, then fades as it nears the ship's top sail speed. It can never push a ship past that speed; it just gets you there sooner.
- **Stamina:** each stroke costs stamina, and up to twice as much when rowing straight into a strong wind. The bar shows the extra cost.

## Multiplayer

- **Who needs the mod:** the server doesn't, and other players still see the ship go faster.
- **The ship's owner must have it.** That's the player whose game took control of the ship first, often the first one aboard, and not necessarily the helmsman.
  - If the owner doesn't have the mod, rowing does nothing, and you'll see "Your strokes won't count".
- **Settings:** the owner's settings decide how strong rowing is, so it's easiest if everyone keeps the defaults.

## Installing (Windows)

**With r2modman or the Thunderstore Mod Manager (recommended):**

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) and select Valheim.
2. Create or select a profile, then go to **Settings → Profile → Import local mod** and pick the `RowingMod-1.2.0.zip` file.
3. When asked, let it install the dependency **BepInExPack_Valheim**.
4. Start the game with **Start modded**.

**By hand:**

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/): unzip it and copy the contents of its `BepInExPack_Valheim` folder into your Valheim folder.
2. Copy `RowingMod.dll` into `Valheim\BepInEx\plugins\RowingMod\`.
3. Start Valheim normally.

## Settings

After the first launch, the settings are in `BepInEx\config\com.dkulundzic.rowingmod.cfg`. In r2modman, use **Config editor**.

| Setting | Default | What it does |
|---|---|---|
| `Controls.RowKey` | H | Key for a stroke. Don't use movement, attack, jump or crouch keys; they make you stand up. |
| `Controls.BrakeKey` | J | Hold to brake (hold water with your oar). |
| `Brake.Strength` | 0.1 | How hard one braking rower slows the ship. |
| `Brake.StaminaPerSecond` | 3 | Stamina braking costs per second. |
| `Brake.Turning` | true | Braking on one side swings the bow toward that side. |
| `Timing.StrokeCycleStill` | 1.8 | Seconds between beats when the ship is still. |
| `Timing.StrokeCycleTopSpeed` | 1.2 | Seconds between beats at top sail speed. |
| `Timing.SweetSpotWidth` | 0.2 | Width of the green zone around each beat, as a fraction of the beat. |
| `Timing.WeakStrokeFactor` | 0.35 | Strength of an early or late stroke. |
| `Stamina.StaminaPerStroke` | 6 | Stamina per stroke. |
| `Stamina.HeadwindStaminaFactor` | 1 | Extra cost into the wind (1 = up to double; 0 = off). |
| `Force.StrokeStrength` | 0.6 | Push of one strong stroke, as a fraction of the ship's paddle force. |
| `Force.MaxBoost` | 2 | Most push the whole crew can build up. |
| `Force.StrokeFade` | 1.2 | Seconds for a stroke's push to fade. |
| `Force.TopSpeedMultiplier` | 1 | Rowing stops helping at the ship's top sail speed times this. |
| `Crew.SyncBonusPerRower` | 0.15 | Bonus per extra rower hitting the same beat. |
| `Crew.MaxSyncBonus` | 0.45 | Largest sync bonus. |
| `Crew.ClashBrake` | 0.2 | Braking from an off-beat stroke when others hit the beat. |
| `UI.Scale` | 0 | Size of the stroke bar, messages and crew panel. 0 is automatic (fits the screen; 1 at 1080p). |
| `UI.ShowCrewPanel` | true | Show the crew panel (top-down ship with its rowers) in the bottom-right corner while rowing or steering. |
| `UI.CrewNames` | false | Show player names next to the benches in the crew panel. |
| `UI.BarOffset` | 0 | Extra pixels to raise the stroke bar. |
| `UI.ShowOars` | true | Show an oar at every rowing seat, swinging with each stroke. |
| `Sounds.SplashVolume` | 0.8 | Volume of the stroke splash, clash and oarlock knock; weak strokes are quieter. |
| `Sounds.CreakVolume` | 0.5 | Volume of wood creaking under strong strokes (0 turns it off). |
| `Sounds.SplashSound`, `RunoffSound`, `DripSound`, `KnockSound`, `SyncSound`, `ClashSound`, `CreakSound` | (empty) | Game sounds for each layer of a stroke (comma-separated to combine); empty uses the mod's default choice. |
| `UI.ShowSplashes` | true | Show water spray at the blade on each stroke. |
| `Controls.DrumKey` | H | At the helm: turn the ship's war drum on or off. |
| `Sounds.DrumVolume` | 0.8 | Volume of the war drum. |
| `Stamina.RestedDiscount` | 0.1 | Rested rowers pay this fraction less stamina. |
| `UI.ShowWakes` | true | Show subtle wakes where blades sweep through the water. |
