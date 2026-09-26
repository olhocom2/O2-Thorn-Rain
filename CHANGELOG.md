# Changelog

All notable changes to this project will be documented in this file.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](https://semver.org/).

---

## [0.3.0] - 2026-09-26

### Added

- **"The Storm of Four" Event** — multi-stage world event featuring 4 ancestral biome tornado pillars (Jungle, Snow, Desert, Corruption/Crimson) and natural ocean vortex encounter.
- **Lightning Rod (`LightningRod`) Official Crafting & Invocation** — crafted with 5 Luminite Bars, 20 Clouds, and 1 Bottled Water at the Ancient Manipulator. Can be invoked at any time provided the event is not already active.
- **2-Day In-Game Event Timeout** — if the player fails to eliminate the pillars and boss within 2 in-game days (172,800 ticks / 48 real minutes), all event entities despawn and a message broadcasts that calm weather has returned.
- **Eye of the Thorn Storm Boss (`ThornStormBoss`)** — colossal multi-phase celestial boss featuring dynamic scaling across difficulty modes.
- **Boss Treasure Bag (`ThornStormBossBag`)** — drops in Expert/Master/Journey/FTW worlds. Opens via right-click; grants coins, Luminite bars, and a difficulty-scaled mount chance.
- **Boss Trophy (`ThornStormBossTrophy` + tile)** — 3×3 wall trophy, 10% direct drop on any difficulty.
- **Dragon Mount (`DragonMount` + `DragonMountItem`)** — Spike Dragon visuals, 8-frame animation, crimson + vortex dust particles.
- **Dragon Power item (`DragonPower`)** — mount summon item; drops inside the boss bag only (Expert/Journey 5%, Master 10%, FTW ~14%).
- **Dynamic difficulty scaling** — boss HP, damage, defense, speed, and mini-tornado count scale per world mode.
- **Boss bar (`ThornStormBossBar`)** — HUD bar with head icon and official name.
- **Boss intro ambient system (`ThornBossIntroSystem`)** — max rain, strong wind, dense clouds, blizzard particles, and dark crimson vignette while boss is alive; fades out on defeat.
- **Destructible mini-tornados (`ThornMinionTornado`)** — flying mobs summoned by the boss; 6-frame animation, full HP bar, scale with boss phase.
- **Phase-based boss sprites** — three spritesheet phases, each 8 frames.
- **Vanilla-style loot model:**
  - Classic: Luminite bars (25–40) + Trophy 10% direct drop.
  - Expert/Master/FTW/Journey: Boss Bag + Trophy 10% direct. Mount inside bag at scaled chance. All items drop normally in Journey mode.
- **Boss size increased 50%** — hitbox 240 → 360, visual scale 2.05f → 3.07f, hover offsets adjusted per phase.

### Fixed & Cleaned

- **Removed development cheats & test items** — removed starter items (Zenith, Fishron Wings, 20x Lightning Rod) from `ThornRainPlayer.cs`.
- **Restored natural weather cycles** — removed forced permanent rain override from `SpikeRainSystem.cs`; rain is now 100% governed by Terraria's natural weather.
- **Restored natural ocean spawn conditions** — Abyssal Mini Vortex only spawns during active storms on the ocean floor with 15% probability every 5 seconds.
- **Removed residual debug flags** — removed `DebugForceTornado` from `ThornTornadoSystem.cs`.
- Dragon mount direction flip: spritesheet reoriented to face left (engine flips right automatically).
- Dragon mount player clip: recalibrated `playerYOffsets` so player sits correctly on the saddle.
- `DragonPower` item size normalized to 24×24 (was 370×306).
- `DivideByZeroException` on mount activation: `MountData.totalFrames` was 0.
- Trophy moved from bag-exclusive to 10% direct world drop.

---

## [0.2.0] - 2026-09-22

### Added

- **Umbrella protection** — vanilla Umbrella blocks thorn damage while held open; durability system with visual bar in inventory slot and state-based tooltips.
- **Ironskin immunity** — while `BuffID.Ironskin` is active, thorns deal no damage and umbrella durability is preserved.
- **Umbrella Slime drop** — 5% chance to drop the vanilla Umbrella.
- **Progressive damage scaling** — spike damage scales with world progression and world mode.

### Changed

- Spawn pacing reduced to every 3 ticks.
- Global spike cap raised to 120; per-player local cap set to 70 within 1500 units.
- `IsPlayerInRainZone` made public for cross-system reuse.
- Modular architecture: `Common/GlobalItems/`, `Common/Systems/`.

---

## [0.1.0] - 2026-09-21

### Added

- Initial release of **O2 Thorn Rain**.
- Storm weather system: dangerous thorns fall from the sky during rain.
- Dynamic trajectory physics driven by `Main.windSpeedCurrent`.
- Danger zone restricted to surface and sky layers.
- Custom hostile projectile `SpikesProjectile` with impact particles and audio.
- Multiplayer support (spawn authority on server).
