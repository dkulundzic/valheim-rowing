# Oar braking ("holding water")

**Status:** agreed and implemented on 2026-10-04. The braking strength will be tuned by playtesting.

## Idea

Real crews stop a boat by dipping their blades and holding them still and upright in the water. Rowers can do the same to slow the ship much faster than drifting.

## Rules

- **Control:** hold **J** while seated on a rowing bench. Letting go stops braking. The key is configurable.
  - J is free in Valheim and doesn't clash with H, where a tap rows and a 3 s hold toggles the beat tick.
- **Who can brake:** only rowers on benches, the same as rowing. The helmsman can't brake.
- **While braking:**
  - **Oar:** your blade drops into the water and stays there, upright and still.
  - **Rowing:** you can't row. After letting go, you can row again on the next beat.
  - **Stamina:** braking costs **3 per second**. With no stamina left, you can't brake.
- **Effect:**
  - **Drag:** each braking rower drags against the ship's motion, in proportion to its speed. It's strong when fast and gentle when slow, so the ship glides to a halt instead of jolting. Below a crawl (about 0.3 m/s) it finishes the stop.
  - **Direction:** braking only slows the ship and never pushes it backward. It works going forward or backing, at any speed setting, and with the sail open (the crew fights the wind, e.g. to stop before a dock).
  - **Crew:** each braking rower's drag adds up. There's no bonus for braking together.
- **One-sided braking turns the ship:** drag is applied at each oar's position on the hull, so braking on one side only swings the bow toward that side. It's **on** by default, with a setting to turn it off.

## Strength

These are starting values to tune in play, since the user wants to try them out first:

| Crew braking | Longship from cruise (about 5 m/s) |
|---|---|
| 4 rowers | to a halt in about 5–6 s |
| 1 rower | to a halt in about 20 s |

- **Drag per rower:** a deceleration of about `0.1 × speed` per second. Four rowers give about `0.42 × speed`, which drops speed by 90% in about 5.5 s.
- **Settings:** `Brake.Strength` (the per-rower factor), `Brake.StaminaPerSecond` (3) and `Brake.Turning` (true).

## Feedback

- **Crew panel:** braking benches show **blue**, and the mini oar is held still in the water.
- **Sound:** a gurgling, rushing water sound at the blade while braking, louder at speed. It's a looping slice of the game's swim splashes (`sfx_footstep_swim`).
- **Spray:** the blade spray effect, repeated while the blade is held at speed.
- **Message:** the stroke bar's title shows "Holding water" while braking.

## Multiplayer

- **Broadcast:** brake on and off are broadcast to everyone, like strokes. The ship's owner applies the drag, and every modded player sees the held oars and hears them.
- **Owner:** as with rowing, braking only works when the ship's owner has the mod.
- **Leaving the bench:** a rower who stands up, disconnects or runs out of stamina stops braking. The owner also drops a brake that hasn't been confirmed recently, so a lost "brake off" message can't leave it stuck on.
