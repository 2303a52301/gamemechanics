# Hyderabad Night Rush: Unity 3D prototype

A playable 3D arcade night-racing prototype that implements the pitch deck (6 zones, traffic types, near-miss
combos, drift, nitro, slipstream, rivals, night-time traffic curve). It uses **only code and primitive shapes**, so
there are no assets to import and nothing to wire up by hand.

## Setup (2 minutes)
1. Create a new Unity project: **Unity 2021.3 LTS or newer, including Unity 6**. 3D (Built-in) or 3D (URP) both work.
2. Copy the `Assets/NightRush` folder into your project's `Assets` folder.
3. Menu **Night Rush > Create Game Scene**, then press **Play**.
   (Or skip step 3: pressing Play in any scene auto-starts the game.)

Input: works with either the new Input System or the legacy Input Manager (Player Settings > Active Input Handling).

## Controls
| Key | Action |
|---|---|
| W / S or Up / Down | Throttle / brake |
| A / D or Left / Right | Steer |
| Space | Drift (holds a slide and charges nitro) |
| Left Shift | Nitro |
| [ and ] (or - and =) | Slow down / speed up the simulation (0.5x, 0.75x, 1x, 1.5x, 2x) |
| R / Esc / Enter | Restart / menu / start |

Gamepad (new Input System): triggers, left stick, A = nitro, B = drift.

## Simulation speed and countdown
Choose a simulation speed (0.5x to 2x) in the menu, or change it any time during the countdown and the race
with `[` and `]`. It scales the whole race through `Time.timeScale`; the menu, countdown and results screen always run
at normal speed. Each race starts with a READY, 3, 2, 1, GO countdown, and the chosen speed shows on the HUD.
Score and time are counted in game time, so a faster setting is a harder, quicker race.

## Theme selection
The menu opens with four themes. Pick one, then a mode, then press Enter.

| Theme | Setting | Road and handling | Traffic |
|---|---|---|---|
| **Desert** | Midday sun, dunes, cacti, red mesas | 3 lanes, long straights, slightly higher top speed | Sparse trucks and cars, a few rocks |
| **Highway** | Sunset, 5 lanes, guardrails, sign gantries, lamps | Fastest top speed | Dense, fast sedans and trucks |
| **Traffic City** | Hyderabad at night: neon towers, wet road, street lamps | 4 lanes | Heavy mixed traffic (autos, bikes, buses). Thins from 8 PM to 2 AM |
| **Off Road** | Overcast forest trail, dirt ruts, no lane lines | 3 lanes, loose grip (easy slides), slower, bumpy camera | Static rocks and fallen logs plus a few slow vehicles |

Add or tweak a theme in `ZoneProfile.All`. Every look and feel value (colours, sun, fog, lanes, traffic mix,
grip, speed, clock) lives in that one entry.

## How it maps to the deck
| Deck slide | In the project |
|---|---|
| Four pillars | `RoadGenerator` (roads, lights), `TrafficSpawner` (traffic), `RivalAI` (duels), lighting in `GameManager.ApplyLighting` |
| Core loop | Pick theme and mode, race, weave traffic, beat rivals, score (upgrade shop not built) |
| Signature mechanics | `CarController` (drift, nitro) and `NearMissScorer` (near-miss combo up to x8, slipstream) |
| Traffic system | `TrafficVehicle`: autos cut lanes, buses slow and wide, bikes weave, trucks give slipstream |
| Game modes | Circuit Sprint (3 rivals) and Time Attack are playable. Traffic Duel and Career are not built yet |

## Code map (`Assets/NightRush/Scripts`)
- `GameManager`: state machine (menu, countdown, race, finish), scoring, HUD and menu (IMGUI, no packages needed)
- `CarController`: arcade physics shared by player and rivals. Inputs come from `PlayerInput` or `RivalAI`
- `TrafficSpawner`, `TrafficVehicle`, `NearMissScorer`, `RivalAI`, `ChaseCamera`
- `RoadGenerator`: endless recycled road segments. `VehicleFactory`: builds every car and prop from cubes
- `ZoneProfile` (themes), `Track`, `InputBridge`; `Editor/NightRushSceneBuilder`

## Tuning
Everything important is a public field: top speed, grip and drift grip on `CarController`; `finishDistance` and
`rivalCount` on `GameManager`; theme data in `ZoneProfile`. If lamp light looks too strong or weak in your render
pipeline, change `l.intensity` in `RoadGenerator.AddLamp` (URP usually wants a different value than Built-in).

## Next steps toward a full game
1. Replace `VehicleFactory.Create` with real car, auto, bus, bike and truck models (instantiate prefabs instead of cubes).
2. URP: add a Volume with Bloom, Vignette and Lens Distortion for the neon and headlight glare. Add a reflection probe for wet roads.
3. Add engine, drift and horn audio. Replace the IMGUI HUD with uGUI or UI Toolkit.
4. Build Career progression (Rookie to Rush Legend), car upgrades, Traffic Duel, and online play (Netcode for GameObjects).
5. Curved roads, flyovers and tunnels (Splines package) for the real Hyderabad landmarks.

Note: this code was written without access to the Unity Editor, so it has not been compiled or play-tested.
If Unity reports a compile error or something feels off, send me the Console message and I will fix it.
