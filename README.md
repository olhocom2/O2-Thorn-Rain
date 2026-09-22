# O2 Thorn Rain 🌧️🌵

[![Terraria](https://img.shields.io/badge/Terraria-v1.4.4.9-green.svg)](https://terraria.org/)
[![tModLoader](https://img.shields.io/badge/tModLoader-v2023.11+-orange.svg)](https://github.com/tModLoader/tModLoader)
[![C#](https://img.shields.io/badge/Language-C%23-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![GitHub repo](https://img.shields.io/badge/GitHub-olhocom2%2FO2--Thorn--Rain-blueviolet)](https://github.com/olhocom2/O2-Thorn-Rain)

**O2 Thorn Rain** is a [Terraria](https://terraria.org/) mod built with [tModLoader](https://github.com/tModLoader/tModLoader) that turns ordinary rainy weather into an intense environmental challenge: sharp spikes fall relentlessly from the skies!

---

## 🎯 Features

- 🌧️ **Dynamic Thorn Rain:** Whenever it rains in your Terraria world, thorns rain down over exposed surface areas.
- 💨 **Wind-Influenced Physics:** Falling spikes dynamically respond to the world's real-time wind speed (`Main.windSpeedCurrent`).
- 🛡️ **Underground Exemption:** Deep caves, underground caverns, and submerged shelters remain safe—spikes only spawn in surface and sky zones (`ZoneOverworldHeight`, `ZoneSkyHeight`).
- ⚡ **Optimized Performance:**
  - Spawn cadence controlled via tick throttling.
  - Strict global active projectile cap (`MaxActiveSpikes = 35`) preventing frame drops and FPS stuttering.
  - Solid collision validation before instantiating projectiles to prevent wasted entity allocations.
- 🌐 **Multiplayer Compatible:** Spawn logic is server-authoritative (`Main.netMode != NetmodeID.MultiplayerClient`), ensuring synchronized behavior across all connected players.

---

## 📂 Project Structure

```
O2ThornRain/
├── Common/
│   └── Systems/
│       └── SpikeRainSystem.cs       # World update hooks, wind physics, spawn regulation
├── Content/
│   └── Projectiles/
│       ├── SpikesProjectile.cs     # Spike projectile behavior, damage, lifetime
│       └── SpikesProjectile.png    # Sprite asset
├── Localization/
│   ├── en-US_Mods.O2ThornRain.hjson # English strings
│   └── pt-BR_Mods.O2ThornRain.hjson # Brazilian Portuguese strings
├── Properties/
│   └── launchSettings.json          # Debugging and launch configuration
├── build.txt                        # tModLoader mod metadata (version, author, name)
├── description.txt                  # In-game description
├── description_workshop.txt         # Steam Workshop description
├── icon.png                         # Mod icon (80x80)
├── icon_small.png                   # Small mod icon
├── O2ThornRain.csproj               # .NET project file
├── CONTRIBUTING.md                  # Contribution guidelines
├── COMMIT_CONVENTION.md             # Conventional commit standards
└── README.md                        # Documentation
```

---

## 🚀 Installation

### Option 1: Steam Workshop (Recommended)
1. Subscribe to **O2 Thorn Rain** on the Steam Workshop.
2. Open **tModLoader**, go to **Manage Mods**, and enable **O2 Thorn Rain**.
3. Reload mods and enter your world!

### Option 2: Manual Installation from Source
1. Clone this repository into your tModLoader `ModSources` directory:
   - **Windows:** `%UserProfile%\Documents\My Games\Terraria\tModLoader\ModSources\O2ThornRain`
   - **macOS:** `~/Library/Application Support/Terraria/tModLoader/ModSources/O2ThornRain`
   - **Linux:** `~/.local/share/Terraria/tModLoader/ModSources/O2ThornRain`
2. Launch tModLoader.
3. Navigate to **Workshop** > **Develop Mods**.
4. Find **O2 Thorn Rain** and click **Build + Reload**.

---

## 🛠️ Development & Building

### Requirements
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- [tModLoader](https://store.steampowered.com/app/1281930/tModLoader/) v2023.11+
- An IDE with C# support: [Visual Studio](https://visualstudio.microsoft.com/), [Visual Studio Code](https://code.visualstudio.com/), or [JetBrains Rider](https://www.jetbrains.com/rider/)

### Building via Command Line
Run dotnet build pointing to the project:
```bash
dotnet build O2ThornRain.csproj
```

---

## 🤝 Contributing

Contributions, bug reports, and suggestions are welcome!
- Review [CONTRIBUTING.md](CONTRIBUTING.md) for local setup, workflows, and PR checklists.
- Follow our [COMMIT_CONVENTION.md](COMMIT_CONVENTION.md) to keep git history clear and structured.

---

## 📜 License & Credits

- Created and maintained by **[olhocom2](https://github.com/olhocom2)**.
- Developed for the Terraria community.
