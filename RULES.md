# How rowing works

Passengers can row to make a ship go faster. This page explains the rules in plain terms. The exact numbers come at the end.

## In one sentence

Sit in a passenger seat, press **H** in rhythm, and each good stroke gives the ship a push that fades away. The whole crew's pushes add up, but they can't make the ship faster than it could go under full sail in perfect wind.

## 1. Who can row, and when

| Situation | Can you row? |
|---|---|
| Sitting in a passenger seat | ✅ |
| Steering at the helm | ❌ The helmsman steers and doesn't row. |
| Standing or walking on deck | ❌ You must be seated. |
| Ship paddling forward (Slow) | ✅ |
| Sail open (Half or Full) | ✅ Rowing adds to the wind. |
| Ship backing (Back) | ✅ Rowing pushes **backward**. |
| Ship set to **Stop** | ❌ Rowing is paused. |
| Out of stamina | ❌ "Too tired to row". |

## 2. The stroke bar: timing

When you sit down, a bar appears above your stamina bar. A white marker sweeps from left to right after each stroke:

```
 [ RED |                 | GREEN |          ]
   too fast       early   strong    late
```

- **Green zone:** a **strong** stroke, full power.
- **Early or late:** a **weak** stroke, about a third of the power.
- **Red zone (pressing too soon):** mashing. You lose stamina and get no stroke.
- **Every press restarts the marker**, including a wasted one. Mashing doesn't just fail; it also throws off your rhythm.

The best rhythm is one stroke about every 1.5 seconds, each one in the green zone. Fast weak strokes give less speed than steady strong ones, and they cost more stamina.

## 3. Stamina

- **Every press costs stamina,** including wasted ones.
- **Rowing into the wind costs more,** up to **double** when rowing straight into a strong wind.
  - A side wind or tailwind costs the normal amount.
  - While backing, "into the wind" means the wind you're backing into.
- **Extra cost is shown** in the bar title, e.g. `Headwind: +60% stamina`.

## 4. How strokes speed up the ship

- **Each stroke is a push that fades.** A strong stroke gives the ship a push that dies away over a second or two.
- **The crew's pushes add up.** Every rower's strokes go into one shared pool, so more rowers in rhythm means more push. There's a ceiling, though: about 4 rowers with good timing reach it, and more add nothing.
- **Rowing works best when slow.** The faster the ship already goes, the less each stroke helps:

  | Ship speed (compared with its top speed) | How much a stroke helps |
  |---|---|
  | Standing still | 100% |
  | Half of top speed | 75% |
  | Three-quarters of top speed | about 45% |
  | At top speed | 0% |

- **Each ship has its own top speed:** the speed it would reach with full sail, the strongest wind and the best wind angle. Rowing can never push past it. In good wind, rowing gets you to top speed sooner. In calm or bad wind, it's what keeps you moving.
- **Rowing pushes straight,** so it never turns the ship. Steering stays with the helmsman.

## 5. Playing together

- **The server doesn't need the mod.** Everyone, with or without the mod, sees the ship go faster.
- **The ship's "owner" needs the mod.** Valheim lets one player's game run each ship's physics. That's the ship's owner: usually the first player aboard, **not necessarily the helmsman**. Your strokes go to that player's game.
  - If the owner has the mod, everyone's strokes count.
  - If the owner doesn't, nobody's strokes count, and seated rowers see **"Your strokes won't count"**.
  - **Easiest rule: the whole crew installs the mod.**
- **The owner's settings decide.** If someone changes their config, ships they own row differently. Keep the defaults to keep it fair.

## 6. Messages you'll see

| Message | Meaning |
|---|---|
| **Rowing ready** | You sat down in a seat and can row. |
| **Rowing paused** | The ship was set to Stop. |
| **You can row now** | The ship started moving again. |
| **Your strokes won't count** | The ship's owner doesn't have the mod. |
| **Your strokes count again** | The ship's owner now has the mod. |
| Strong stroke! / Early / Late / Too fast! | How your last press went. |
| Too tired to row | Not enough stamina for a stroke. |

## The numbers (default settings)

All of these can be changed in `BepInEx/config/com.dkulundzic.rowingmod.cfg`.

**Timing:**
- Rhythm (`StrokeCycle`): **1.5 s** from one stroke to the middle of the green zone.
- Green zone (`SweetSpotWidth`): 20% of the cycle, about **1.35–1.65 s** after your last press.
- Too fast: under half a cycle, **0.75 s**.
- Weak-stroke power (`WeakStrokeFactor`): **35%** of a strong stroke.

**Push:**
- One strong stroke adds **0.6** to the crew's push (`StrokeStrength`). The unit is "the ship's own paddle force": a push of 1.0 equals one extra set of paddles.
- The crew's push is capped at **2.0** (`MaxBoost`).
- The push fades by about 63% every **1.2 s** (`StrokeFade`).
- On average, one rower with perfect timing adds about **0.5**, two add about **1.0**, and four reach the cap.
- When the ship is set to Stop, the push resets to zero.

**Speed:**
- How much a stroke helps = `1 − (speed ÷ top speed)²`.
- Top speed is estimated per ship from its sail strength and water drag, then multiplied by `TopSpeedMultiplier` (default **1**).
- Estimated top speeds: **Karve 7.4 m/s**, **Longship 9.6 m/s**. Other ships appear in the BepInEx log when they load.

**Stamina:**
- Cost per press = **6** × (1 + headwind), where headwind runs from 0 (side or tailwind) to 1 (straight into a full-strength wind). That gives 6 to 12 stamina.
- `StaminaPerStroke` sets the base cost, and `HeadwindStaminaFactor` sets the headwind extra (0 turns it off).
