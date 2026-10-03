# RowingMod

Passengers can row to speed up the ship.

## How to row

1. Sit in a passenger seat on a Karve or Longship. A "Rowing ready" message appears, and the stroke bar shows above your stamina bar.
2. Press **H** when the white marker crosses the green zone. That's the ship's beat, and a strong stroke.
   - **Off the beat:** a weak stroke, or a **clash** if others hit the beat, which brakes the boat a little.
   - **Twice in one beat:** wastes stamina.
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
2. Create or select a profile, then go to **Settings → Profile → Import local mod** and pick the `RowingMod-1.0.1.zip` file.
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
| `UI.BarOffset` | 0 | Extra pixels to raise the stroke bar. |
