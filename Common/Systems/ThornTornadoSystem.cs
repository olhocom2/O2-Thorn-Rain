using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// Lifecycle states for the Thorn Tornado event.
/// </summary>
public enum TornadoEventState
{
    None,
    Warning,
    Active,
    Cooldown
}

/// <summary>
/// Manages the lifecycle of Thorn Tornadoes.
/// Server-authoritative for state transitions, target selection, and spawning.
/// </summary>
public class ThornTornadoSystem : ModSystem
{
    public const int CheckIntervalTicks = 600; // 10 seconds
    public const int WarningSecondMessageDelayTicks = 150; // 2.5 seconds
    public const int WarningTotalDurationTicks = 240; // 4 seconds
    public const int CooldownDurationTicks = 3600; // 60 seconds
    public const float SpawnDistanceX = 1200f;

    public static bool DebugForceTornado;

    public static TornadoEventState CurrentState { get; private set; } = TornadoEventState.None;

    private static int _stateTimer;
    private static int _checkTimer;
    private static float _chosenDirection = 1f; // -1 = left, +1 = right
    private static int _targetPlayerIndex = -1;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (DebugForceTornado)
        {
            DebugForceTornado = false;
            StartTornadoWarning();
            return;
        }

        switch (CurrentState)
        {
            case TornadoEventState.None:
                UpdateNoneState();
                break;

            case TornadoEventState.Warning:
                UpdateWarningState();
                break;

            case TornadoEventState.Active:
                UpdateActiveState();
                break;

            case TornadoEventState.Cooldown:
                UpdateCooldownState();
                break;
        }
    }

    private static void UpdateNoneState()
    {
        if (!Main.raining)
            return;

        _checkTimer++;
        if (_checkTimer < CheckIntervalTicks)
            return;

        _checkTimer = 0;

        int eligiblePlayer = FindEligibleRainPlayer();
        if (eligiblePlayer < 0)
            return;

        float currentTornadoChance = SpikeRainSystem.GetCurrentTornadoChance();
        if (Main.rand.NextFloat() <= currentTornadoChance)
        {
            _targetPlayerIndex = eligiblePlayer;
            StartTornadoWarning();
        }
    }

    public static void StartTornadoWarning()
    {
        CurrentState = TornadoEventState.Warning;
        _stateTimer = 0;

        if (_targetPlayerIndex < 0 || !IsPlayerValidTarget(_targetPlayerIndex))
        {
            _targetPlayerIndex = FindEligibleRainPlayer();
        }

        _chosenDirection = Main.rand.NextBool() ? 1f : -1f;

        BroadcastEventMessage(
            "Mods.O2ThornRain.Events.TornadoWarning",
            new Color(80, 200, 160)
        );

        SoundEngine.PlaySound(
            SoundID.Item121 with
            {
                Volume = 0.5f,
                Pitch = -0.3f
            }
        );
    }

    private static void UpdateWarningState()
    {
        _stateTimer++;

        if (_stateTimer == WarningSecondMessageDelayTicks)
        {
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.TornadoIncoming",
                new Color(255, 120, 80)
            );

            SoundEngine.PlaySound(
                SoundID.Item122 with
                {
                    Volume = 0.75f,
                    Pitch = -0.1f
                }
            );
        }

        if (_stateTimer >= WarningTotalDurationTicks)
        {
            SpawnThornTornado();
            CurrentState = TornadoEventState.Active;
            _stateTimer = 0;
        }
    }

    private static void UpdateActiveState()
    {
        int tornadoType = ModContent.ProjectileType<ThornTornadoProjectile>();
        bool tornadoExists = false;

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.type == tornadoType)
            {
                tornadoExists = true;
                break;
            }
        }

        if (!tornadoExists)
        {
            CurrentState = TornadoEventState.Cooldown;
            _stateTimer = 0;
            _targetPlayerIndex = -1;
        }
    }

    private static void UpdateCooldownState()
    {
        _stateTimer++;

        if (_stateTimer >= CooldownDurationTicks)
        {
            CurrentState = TornadoEventState.None;
            _stateTimer = 0;
            _checkTimer = 0;
        }
    }

    private static void SpawnThornTornado()
    {
        Vector2 spawnPosition = GetTornadoSpawnPosition();
        int tornadoType = ModContent.ProjectileType<ThornTornadoProjectile>();
        int damage = SpikeRainSystem.GetSpikeDamage();

        Projectile.NewProjectile(
            Entity.GetSource_NaturalSpawn(),
            spawnPosition,
            new Vector2(
                _chosenDirection * ThornTornadoProjectile.HorizontalSpeed,
                0f
            ),
            tornadoType,
            damage,
            ThornTornadoProjectile.TornadoKnockback,
            Main.myPlayer,
            ai0: _chosenDirection
        );
    }

    private static Vector2 GetTornadoSpawnPosition()
    {
        Player targetPlayer = null;

        if (_targetPlayerIndex >= 0 && IsPlayerValidTarget(_targetPlayerIndex))
        {
            targetPlayer = Main.player[_targetPlayerIndex];
        }
        else
        {
            int index = FindEligibleRainPlayer();
            if (index >= 0)
            {
                targetPlayer = Main.player[index];
            }
        }

        if (targetPlayer == null)
        {
            return new Vector2(
                Main.maxTilesX * 8f,
                (float)Main.worldSurface * 16f - 100f
            );
        }

        float spawnX = targetPlayer.Center.X - (_chosenDirection * SpawnDistanceX);
        float spawnY = targetPlayer.Center.Y - 60f;

        return new Vector2(spawnX, spawnY);
    }

    private static int FindEligibleRainPlayer()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (IsPlayerValidTarget(i))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsPlayerValidTarget(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return false;

        Player player = Main.player[playerIndex];
        if (!player.active || player.dead)
            return false;

        return SpikeRainSystem.IsPlayerInRainZone(player);
    }

    private static void BroadcastEventMessage(string translationKey, Color color)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(Language.GetTextValue(translationKey), color);
            return;
        }

        if (Main.netMode == NetmodeID.Server)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromKey(translationKey), color);
        }
    }
}