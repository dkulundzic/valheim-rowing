# How rowing works

Passengers can row to make a ship go faster. This page explains the rules in plain terms. The exact numbers come at the end.

## In one sentence

Sit on a rowing bench, press **H** on the beat, and each good stroke gives the ship a push that fades away. The whole crew's pushes add up, but they can't make the ship faster than it could go under full sail in perfect wind.

## 1. Who can row, and when

| Situation | Can you row? |
|---|---|
| Sitting on a **rowing bench** (Karve 2, Longship 4) | ✅ |
| Sitting in the **back seat** | ❌ It's a passenger seat on the centre line, with no oar. |
| Bracing at a **Hold fast** spot | ❌ You're standing and holding on, not sitting at an oar. |
| Steering at the helm | ❌ The helmsman steers and doesn't row. |
| Standing or walking on deck | ❌ You must be seated. |
| Ship paddling forward (Slow) | ✅ |
| Sail open (Half or Full) | ✅ Rowing adds to the wind. |
| Ship backing (Back) | ✅ Rowing pushes **backward**. |
| Ship set to **Stop** | ✅ Rowing pushes forward, so a crew can row a ship that nobody is steering. |
| Out of stamina | ❌ "Too tired to row". |

## 2. The ship's beat: timing

The whole ship rows to **one shared beat**, like a drummer keeping time. Every rower sees the same beat at the same moment.

- **The beat speeds up with the ship:** slow, heavy strokes to get moving (one every 1.8 s), and a quicker rhythm near top speed (one every 1.2 s). The tempo only changes between beats.
- **The stroke bar shows the beat.** The green zone sits on the beat in the middle of the bar. The white marker sweeps through it as the beat passes, then starts over.

```
 [            |GREEN|            ]
     early     strong     late
```

- **One stroke per beat:**
  - **Pressing in the green zone** gives a **strong** stroke, full power.
  - **Pressing elsewhere** gives an off-beat stroke: weak when rowing alone, a **clash** when others hit the beat (see section 5).
  - **Pressing again in the same beat** is mashing: it wastes stamina and gives no stroke. The marker turns grey once you've rowed on the current beat.

**Oars:** every rowing bench has an oar. On an empty bench it's pulled in and stowed inside the hull; when someone sits down, it swings out and rests in the water. Each stroke swings it through the water and back. A crew in sync rows visibly together, and you can hear it: every oar splashes where it is, and a crew hitting the beat together sounds fuller and deeper. Wood creaks under strong strokes, and each blade leaves a subtle wake on the water. Only players with the mod see and hear the oars.

**The helmsman calls the beat:** at the helm,
- **U** calls a quicker beat and **N** a slower one, stepping through Easy (a slower beat, easier on stamina), Steady (automatic, the default) and Hard (a quicker beat, more push and more stamina).
- **J** calls **"Hold water!"**, telling the crew to brake. Each rower brakes themselves with J.

Rowers see every call in the middle of the screen (like the helmsman does), and the crew panel shows the current beat.

**War drum:** the helmsman can beat a war drum in time with the ship's beat, by pressing **H** at the helm. Everyone aboard hears it, and nearby ships faintly. It starts off, and the crew panel shows whether it's on.

**Rowing skill:** rowing raises a new **Rowing** skill (in the skills screen, with an oar icon). Strong strokes train it most, weak ones a little. **The green zone grows with the skill:** a beginner gets a narrow window, **12% of the beat at level 0**, widening steadily to **28% at level 100** (20%, the old fixed width, at level 50). At level 100, strokes and braking also cost **30% less** stamina and strokes are **15% stronger**, scaling smoothly with level. Like other skills, it drops a little on death.

## 3. Stamina

- **Every press costs stamina,** including wasted ones.
- **Rowing into the wind costs more,** up to **double** when rowing straight into a strong wind.
  - A side wind or tailwind costs the normal amount.
  - While backing, "into the wind" means the wind you're backing into.
- **Rough weather costs more:** **+30% in a storm**, and up to **+25% in strong wind** from any direction (rough seas); the larger applies, added to the headwind cost. It covers braking too.
- **Cold costs more:** with the **Cold** debuff strokes and braking cost **15% more**, and **30% more** when **Freezing**, added to the other hard conditions. Warm clothes and a fire before a northern voyage help.
- **Rested rowers pay 10% less,** for strokes and braking: sleep or rest by a fire before a voyage.
- **The Rowing skill takes up to 30% off,** at level 100 (see the Rowing skill above).
- **How it all adds up:** hard conditions are added together, then your discounts each take their share off what's left.
  - **Hard conditions** (headwind, storm or rough sea, cold): their extra costs are added, and together they never add more than **+150%**.
  - **Discounts** (Rested, the Rowing skill) multiply: Rested and level 100 together cost 0.9 × 0.7 = **63%**, never nothing.
  - Example: a strong headwind (+70%), Rested and Rowing 50 (−15%): 6 × 1.7 × 0.9 × 0.85 = **7.8** stamina per stroke.
- **The cost and its reasons are shown** above the bar title, e.g. `Stamina ×1.30 (headwind +70%, rested -10%, Rowing skill -15%)`.

## 4. How strokes speed up the ship

- **Each stroke is a push that fades.** A strong stroke gives the ship a push that dies away over a second or two.
- **The crew's pushes add up.** Every rower's strokes go into one shared pool, so more rowers on the beat means more push. There's a ceiling, though: about 4 rowers in sync reach it, and more add nothing.
- **Rowing works best when slow.** The faster the ship already goes, the less each stroke helps:

  | Ship speed (compared with its top speed) | How much a stroke helps |
  |---|---|
  | Standing still | 100% |
  | Half of top speed | 75% |
  | Three-quarters of top speed | about 45% |
  | At top speed | 0% |

- **Each ship has its own top speed:** the speed it would reach with full sail, the strongest wind and the best wind angle. Rowing can never push past it. In good wind, rowing gets you to top speed sooner. In calm or bad wind, it's what keeps you moving.
- **Rowing pushes straight,** so it never turns the ship. Steering stays with the helmsman.

## 5. Rowing together: sync and clashes

- **In sync:** when several rowers hit the **same beat** in the green zone, each of their strokes gets a bonus: **+15% per extra rower**, up to **+45%**. Three rowers in sync each row 30% harder. You'll see **"In sync ×3!"**.
- **Clash:** an off-beat stroke on a beat someone else hit means your oar fights the crew's rhythm. It gives **no push** and **brakes the boat a little**. You'll see **"Clash!"**. A clash never pushes the boat backward; it only slows it.
- **Alone,** or when **nobody** hits the beat, an off-beat stroke is just weak (about a third of a strong one), not a clash. There's no rhythm to break.

How much the crew pushes, on average (ship still, in units of the ship's paddle force):

| Crew | Push |
|---|---|
| 1 rower, on beat | 0.40 |
| 1 rower, off beat | 0.14 |
| 2 rowers in sync | 0.92 |
| 3 rowers in sync | 1.56 |
| 2 in sync + 1 clashing | 0.79 |
| 3 rowers all off beat | 0.42 |

Solo rowing is meant to be hard work. A crew in sync is far stronger than the same crew rowing sloppily.

## 6. Playing together

- **The server doesn't need the mod.** Everyone, with or without the mod, sees the ship go faster.
- **The ship's "owner" needs the mod.** Valheim lets one player's game run each ship's physics. That's the ship's owner: usually the first player aboard, **not necessarily the helmsman**. Your strokes go to that player's game.
  - If the owner has the mod, everyone's strokes count.
  - If the owner doesn't, nobody's strokes count, and seated rowers see **"Your strokes won't count"**.
  - **Easiest rule: the whole crew installs the mod.**
- **The owner's settings decide.** If someone changes their config, ships they own row differently. Keep the defaults to keep it fair.

## 7. Braking: holding water

Hold **J** on a rowing bench to dig your blade into the water and hold it there. This slows the ship much faster than drifting.

- **Strength:** braking is strong at speed and gentle when slow, so the ship glides to a halt. It never pushes the ship backward.
- **Crew:** each braking rower adds drag. A full crew stops a Longship from cruising speed in a few seconds; one rower takes much longer.
- **Turning:** braking on **one side only swings the bow toward that side**, like a real crew turning sharply.
- **Cost:** braking costs **3 stamina per second**, and you can't row while braking. Let go of J to row again.
- **When it works:** any time, including under sail and while backing.
- **What you'll see and hear:** the bar title says "Holding water", your bench shows blue in the crew panel, and you hear water rushing past the blade.

## 8. The crew panel

While you row or steer, a small top-down view of the ship sits in the bottom-right corner:

- **Benches:** a dim ring is an empty bench, and a disc is a rower. After each stroke the disc flashes **green** (strong), **yellow** (weak), **red** (clash) or **gold** (in sync with others). It's **blue** while braking. Your own bench has a white ring.
- **Oars:** little oars swing just like the real ones, and stowed oars lie inside the hull.
- **Crew boost:** the blue bar along the middle fills with the crew's push.
- **Speed:** a gauge under the ship shows its speed, e.g. "5.2 m/s", filling toward its top sail speed, which is marked at the right end. That's the most rowing can push it to.
- **The beat:** the ship's outline pulses on every beat, so you can see the rhythm even with the drum off.
- **Drum:** the footer shows whether the war drum is on, and for the helmsman, which key turns it on or off.
- **Helmsman:** a diamond at the helm, with no oar. It's an outline when nobody steers and filled when someone does, with a white ring when it's you.
- **Footer:** the ship's speed setting and the crew's boost (e.g. "Half sail · Crew boost 40%"), and "In sync ×N" when N rowers hit the same beat.

The helmsman sees the panel too, which shows who's rowing and who's in time.

## 9. Messages you'll see

| Message | Meaning |
|---|---|
| **Rowing ready** | You sat down in a seat and can row. |
| **Tip: …** | A short tip the first time something matters: sitting at an oar, your first stroke, another rower joining, your first clash, the ship passing 3 m/s, the stamina line appearing, or running out of stamina. Each shows once per session (again after every logout; turn `Tutorial.ResetOnLogout` off to see each only once, ever). Tips wait until no other notice is up, and a tip a notice replaces shows again afterwards. |
| **Voyage: …** (where the stroke bar was, when you stand up) | Your stint at the oar: distance, time, strokes, % on the beat, syncs, clashes, plus your character's lifetime distance and strokes. |
| **Your strokes won't count** | The ship's owner doesn't have the mod. |
| **Your strokes count again** | The ship's owner now has the mod. |
| **In sync ×N!** | Your strong stroke landed on the same beat as N−1 others. |
| **Clash!** | Your off-beat stroke fought the crew's rhythm. |
| Strong stroke! / Early / Late | How your last press went. |
| Too fast! One stroke per beat | You already rowed on this beat. |
| **War drum on / off** | The helmsman turned the drum on or off. |
| Too tired to row | Not enough stamina for a stroke. |

## The numbers (default settings)

All of these can be changed in `BepInEx/config/com.dkulundzic.rowingmod.cfg`.

**Timing:**
- Beat: **1.8 s** when still (`StrokeCycleStill`), down to **1.2 s** at top speed (`StrokeCycleTopSpeed`), in proportion to speed ÷ top speed.
- Green zone: centred on the beat, its width set by the Rowing skill: **12%** of the beat at level 0 (`Skill.SweetSpotAtLevel0`), widening to **28%** at level 100 (`Skill.SweetSpotAtLevel100`). At level 50 that's 20%, i.e. ±0.18 s at the slowest beat and ±0.12 s at the fastest.
- Weak-stroke power (`WeakStrokeFactor`): **35%** of a strong stroke.

**Crew:**
- Sync bonus: **+15%** per extra rower on the same beat (`SyncBonusPerRower`), up to **+45%** (`MaxSyncBonus`).
- Clash brake: **0.2** of the ship's paddle force (`ClashBrake`), fading like a stroke.

**Push:**
- One strong stroke adds **0.6** to the crew's push (`StrokeStrength`). The unit is "the ship's own paddle force": a push of 1.0 equals one extra set of paddles.
- The crew's push is capped at **2.0** (`MaxBoost`).
- The push fades by about 63% every **1.2 s** (`StrokeFade`).
- On average, one rower on every beat adds about **0.4** when the ship is still (slow beat) and **0.6** near top speed (fast beat), before the speed limit below.
- Switching between forward and back resets the push to zero, so leftover push never shoves the ship the wrong way.

**Speed:**
- How much a stroke helps = `1 − (speed ÷ top speed)²`.
- Top speed is estimated per ship from its sail strength and water drag, then multiplied by `TopSpeedMultiplier` (default **1**).
- Estimated top speeds: **Karve 7.4 m/s**, **Longship 9.6 m/s**. Other ships appear in the BepInEx log when they load.

**Braking:**
- Each braking rower decelerates the ship by **0.1 × its speed** per second (`Brake.Strength`), plus a little near a standstill to finish the stop. The crew's braking adds up, but never reverses the ship.
- **3 stamina per second** (`Brake.StaminaPerSecond`). With `Brake.Turning` on (the default), the drag acts at the rower's side of the hull.

**Stamina:**
- Cost per press = **6** × (1 + load) × relief.
  - **Load:** the extra costs of hard conditions, added together and capped at `Stamina.MaxLoad` (1.5):
    - headwind, from 0 (side or tailwind) to 1 (straight into a full-strength wind), for strokes only;
    - weather, the larger of storm (0.3) and rough sea (up to 0.25 as the wind goes from 60% to full strength);
    - cold: 0.15 when Cold, 0.3 when Freezing.
  - **Relief:** (1 − 0.1 if Rested) × (1 − 0.3 × Rowing level / 100).
  - That gives 3.8 (calm, Rested, Rowing 100) to 15 stamina (full headwind in a storm while Freezing: +160%, capped at +150%; no discounts).
- Braking uses the same chain on its 3 stamina per second, without the headwind.
- `StaminaPerStroke` sets the base cost, `HeadwindStaminaFactor` the headwind extra (0 turns it off), `StormFactor` and `RoughSeaFactor` the weather, `ColdFactor` and `FreezingFactor` the cold, `MaxLoad` the cap on hard conditions, `RestedDiscount` and `Skill.StaminaReduction` the discounts.
