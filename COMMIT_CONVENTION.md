# Commit Convention Guidelines

This project strictly adheres to the **Conventional Commits v1.0.0** specification. Structured commit messages keep the git history clean, readable, and ready for automated changelogs.

---

## 🏗️ Message Structure

```
<type>(<scope>): <subject>

[optional body]

[optional footer(s)]
```

### 1. Type (Required)
Must be one of the following:

| Type | Description |
|---|---|
| `feat` | A new feature or gameplay mechanic |
| `fix` | A bug fix or gameplay resolution |
| `perf` | A code change that improves performance (e.g. FPS / projectile management) |
| `refactor` | Code refactoring that neither fixes a bug nor adds a feature |
| `docs` | Documentation changes (README, CONTRIBUTING, comments) |
| `style` | Formatting, whitespace, or code styling adjustments (no functional changes) |
| `test` | Adding or updating tests or verification scripts |
| `chore` | Maintenance tasks, build files, `.gitignore`, metadata changes |
| `ci` | CI/CD workflow modifications |

### 2. Scope (Optional but Recommended)
Specifies the section of the codebase affected:
- `spawner` - Spawn timing, world surface checks, wind calculations
- `spikes` - Projectile behavior, damage, knockback, sprites
- `localization` - Translation files (`.hjson`)
- `config` - Mod configuration or settings
- `assets` - Textures, icons, audio
- `repo` - Repository configuration, git setup, guidelines

### 3. Subject (Required)
- Use imperative, present tense ("add", "optimize", "fix", not "added", "optimizing", "fixes").
- Do not capitalize the first letter.
- Do not end with a period (`.`).

---

## 🌟 Examples

### Good Commit Messages
```
feat(spikes): add chance for spikes to cause bleeding debuff
perf(spawner): throttle spawn iterations to maintain 60 FPS
fix(spawner): prevent spikes from generating inside underground caverns
docs(readme): add installation guide for macOS and Linux
chore(deps): update tModLoader target references
```

### Breaking Changes
Indicate breaking changes with `!` before the colon or a `BREAKING CHANGE:` footer:
```
refactor(spikes)!: rename projectile internal type identifier
```

---

## 🚫 Avoid
- `fixed stuff`
- `update`
- `WIP`
- `Commit for Olho Com 2`
- Mixed formatting or paragraphs in the first line
