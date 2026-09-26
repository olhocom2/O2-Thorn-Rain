# Contributing to O2 Thorn Rain

Thank you for your interest in contributing to **O2 Thorn Rain**! This document outlines guidelines and workflows to ensure smooth collaboration.

---

## 📋 Table of Contents
1. [Code of Conduct](#code-of-conduct)
2. [How Can I Contribute?](#how-can-i-contribute)
   - [Reporting Bugs](#reporting-bugs)
   - [Suggesting Features](#suggesting-features)
   - [Pull Requests](#pull-requests)
3. [Development Setup](#development-setup)
4. [Coding Guidelines](#coding-guidelines)
5. [Commit Conventions](#commit-conventions)

---

## 🕊️ Code of Conduct
Please be respectful, helpful, and constructive in discussions, issue reviews, and pull requests.

---

## 💡 How Can I Contribute?

### Reporting Bugs
Before filing an issue, please search existing issues to avoid duplicates. When filing a bug report, include:
- A clear, descriptive title.
- Steps to reproduce the bug.
- Expected behavior vs. actual behavior.
- Game logs or error stack traces (`tModLoader-Logs/Natives.log` / `client.log`).
- Mod version and tModLoader version.

### Suggesting Features
Feature requests are always appreciated! Please describe:
- The problem your feature solves or the gameplay experience it adds.
- Clear examples or potential mechanics.
- Any performance or multiplayer implications.

### Pull Requests
1. **Fork** the repository and clone your fork locally.
2. Create a branch following the naming convention:
   - `feature/your-feature-name`
   - `fix/issue-description`
   - `perf/fps-optimization`
3. Test your changes thoroughly in both single-player and multiplayer if applicable.
4. Follow the [Commit Conventions](COMMIT_CONVENTION.md).
5. Open a Pull Request against the `main` branch with a concise summary of changes.

---

## 🛠️ Development Setup

1. **Prerequisites:**
   - [.NET 8.0+ SDK](https://dotnet.microsoft.com/download)
   - [tModLoader](https://store.steampowered.com/app/1281930/tModLoader/) v2023.11+ (1.4.4 branch)
   - IDE: VS Code (with C# Dev Kit), Visual Studio 2022, or JetBrains Rider.

2. **Repository Location:**
   Place this repository under the `ModSources` folder:
   - **macOS:** `~/Library/Application Support/Terraria/tModLoader/ModSources/O2ThornRain`
   - **Windows:** `%UserProfile%\Documents\My Games\Terraria\tModLoader\ModSources\O2ThornRain`
   - **Linux:** `~/.local/share/Terraria/tModLoader/ModSources/O2ThornRain`

3. **Building & Testing:**
   - In tModLoader: **Workshop** > **Develop Mods** > **Build + Reload**.
   - Verify that changes do not cause projectile leaks or frame rate drops.

---

## 🧹 Coding Guidelines

- **C# Conventions:** Follow standard Microsoft C# naming conventions:
  - PascalCase for class names, methods, properties, and constants.
  - camelCase with leading underscore (`_variable`) for private fields.
- **Performance First:**
  - Avoid heavy LINQ allocations or heap allocations inside `PostUpdateWorld()` or projectile update loops.
  - Keep active projectile counts capped (`MaxActiveSpikes`) to preserve 60 FPS on lower-end devices.
  - Check collision before instantiating projectiles to prevent entity bloat.
- **Multiplayer Consideration:**
  - Always guard world-altering spawns behind server-side checks (`Main.netMode != NetmodeID.MultiplayerClient`).
- **Localization:**
  - Add user-facing text to `Localization/en-US_Mods.O2ThornRain.hjson`, `Localization/pt-BR_Mods.O2ThornRain.hjson`, and `Localization/zh-Hans_Mods.O2ThornRain.hjson`.

---

## 📝 Commit Conventions

All commits must follow the **Conventional Commits** specification. See [COMMIT_CONVENTION.md](COMMIT_CONVENTION.md) for full details and examples.
