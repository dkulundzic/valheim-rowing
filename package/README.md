# RowingMod

Passengers can row to speed up the ship.

## How to row

1. Sit in a passenger seat on a Karve or Longship. A "Rowing ready" message appears, and the stroke bar shows above your stamina bar.
2. Press **H** when the white marker reaches the green zone. That's a strong stroke.
   - **Early or late:** a weak stroke.
   - **Before the red zone ends:** mashing, which wastes stamina and does nothing.
3. Keep the rhythm. Each stroke gives the ship a push that fades over about a second, and the whole crew's strokes add up.

## Rules

- **When rowing works:** at every speed setting except **Stop**, including with the sail open. While the ship is backing, rowing pushes it backward.
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
2. Create or select a profile, then go to **Settings → Profile → Import local mod** and pick the `RowingMod-1.0.0.zip` file.
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
| `Timing.StrokeCycle` | 1.5 | Seconds from one stroke to the middle of the next green zone. |
| `Timing.SweetSpotWidth` | 0.2 | Width of the green zone, as a fraction of the cycle. |
| `Timing.WeakStrokeFactor` | 0.35 | Strength of an early or late stroke. |
| `Stamina.StaminaPerStroke` | 6 | Stamina per stroke. |
| `Stamina.HeadwindStaminaFactor` | 1 | Extra cost into the wind (1 = up to double; 0 = off). |
| `Force.StrokeStrength` | 0.6 | Push of one strong stroke, as a fraction of the ship's paddle force. |
| `Force.MaxBoost` | 2 | Most push the whole crew can build up. |
| `Force.StrokeFade` | 1.2 | Seconds for a stroke's push to fade. |
| `Force.TopSpeedMultiplier` | 1 | Rowing stops helping at the ship's top sail speed times this. |
| `Rules.AllowRowingWhenStopped` | false | Allow rowing at Stop (pushes forward). Useful for testing alone. The ship owner's setting decides. |
| `UI.BarOffset` | 0 | Extra pixels to raise the stroke bar. |
