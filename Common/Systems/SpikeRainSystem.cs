using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// Níveis de intensidade climática da Thorn Rain.
/// </summary>
public enum ThornRainIntensity
{
    Light,
    Normal,
    Heavy,
    Storm
}

/// <summary>
/// Gerencia a mecânica de chuva de espinhos (Thorn Rain),
/// incluindo o ciclo de intensidade climática, limites globais,
/// balanceamento por progressão e sincronização multiplayer.
/// </summary>
public class SpikeRainSystem : ModSystem
{
    // =========================================================
    // INTENSIDADE CLIMÁTICA - CONSTANTES DE BALANCEAMENTO
    // =========================================================

    // LIGHT: Intervalo de 6 ticks, 1 espinho por ciclo, 1% de chance de tornado.
    public const int LightSpawnInterval = 6;
    public const int LightSpikesPerCycle = 1;
    public const float LightTornadoChance = 0.01f;

    // NORMAL: Intervalo de 3 ticks, 2 espinhos por ciclo, 3% de chance de tornado.
    public const int NormalSpawnInterval = 3;
    public const int NormalSpikesPerCycle = 2;
    public const float NormalTornadoChance = 0.03f;

    // HEAVY: Intervalo de 2 ticks, 2 espinhos por ciclo, 10% de chance de tornado.
    public const int HeavySpawnInterval = 2;
    public const int HeavySpikesPerCycle = 2;
    public const float HeavyTornadoChance = 0.10f;

    // STORM: Intervalo de 2 ticks, 3 espinhos por ciclo, 35% de chance de tornado.
    public const int StormSpawnInterval = 2;
    public const int StormSpikesPerCycle = 3;
    public const float StormTornadoChance = 0.35f;

    // =========================================================
    // DURAÇÃO DAS FASES (TICKS) - 60 ticks = 1 segundo
    // =========================================================

    public const int LightMinDurationTicks = 1800;   // 30s
    public const int LightMaxDurationTicks = 3600;   // 60s

    public const int NormalMinDurationTicks = 2400;  // 40s
    public const int NormalMaxDurationTicks = 4200;  // 70s

    public const int HeavyMinDurationTicks = 1800;   // 30s
    public const int HeavyMaxDurationTicks = 3000;   // 50s

    public const int StormMinDurationTicks = 1800;   // 30s
    public const int StormMaxDurationTicks = 3000;   // 50s

    // =========================================================
    // CONFIGURAÇÃO DA CHUVA
    // =========================================================

    public const int MaxGlobalSpikes = 120;

    private const int MaxSpikesPerPlayer = 70;

    private const float SpawnHorizontalRange = 1000f;

    private const float SpawnHeightMin = 550f;
    private const float SpawnHeightMax = 800f;

    // =========================================================
    // DANO - PRÉ-HARDMODE
    // =========================================================

    private const int NormalDamagePreHardmode = 40;
    private const int ExpertDamagePreHardmode = 60;
    private const int MasterDamagePreHardmode = 80;

    // =========================================================
    // DANO - HARDMODE
    // =========================================================

    private const int NormalDamageHardmode = 55;
    private const int ExpertDamageHardmode = 80;
    private const int MasterDamageHardmode = 105;

    // =========================================================
    // DANO - PÓS-PLANTERA
    // =========================================================

    private const int NormalDamagePostPlantera = 70;
    private const int ExpertDamagePostPlantera = 100;
    private const int MasterDamagePostPlantera = 130;

    // =========================================================
    // DANO - PÓS-GOLEM
    // =========================================================

    private const int NormalDamagePostGolem = 85;
    private const int ExpertDamagePostGolem = 120;
    private const int MasterDamagePostGolem = 155;

    // =========================================================
    // DANO - PÓS-MOON LORD
    // =========================================================

    private const int NormalDamagePostMoonLord = 110;
    private const int ExpertDamagePostMoonLord = 150;
    private const int MasterDamagePostMoonLord = 190;

    // =========================================================
    // ESTADO E CONTROLE
    // =========================================================

    public static ThornRainIntensity CurrentIntensity { get; private set; } = ThornRainIntensity.Normal;

    private static int _intensityDirection = 1; // +1 aumentando em direção a Storm, -1 diminuindo em direção a Light
    private static int _intensityTimer;
    private static int _currentPhaseDuration = NormalMinDurationTicks;

    private static int _spawnTimer;

    // =========================================================
    // ACESSO À INTENSIDADE
    // =========================================================

    public static int GetCurrentSpawnInterval()
    {
        return CurrentIntensity switch
        {
            ThornRainIntensity.Light => LightSpawnInterval,
            ThornRainIntensity.Normal => NormalSpawnInterval,
            ThornRainIntensity.Heavy => HeavySpawnInterval,
            ThornRainIntensity.Storm => StormSpawnInterval,
            _ => NormalSpawnInterval
        };
    }

    public static int GetCurrentSpikesPerCycle()
    {
        return CurrentIntensity switch
        {
            ThornRainIntensity.Light => LightSpikesPerCycle,
            ThornRainIntensity.Normal => NormalSpikesPerCycle,
            ThornRainIntensity.Heavy => HeavySpikesPerCycle,
            ThornRainIntensity.Storm => StormSpikesPerCycle,
            _ => NormalSpikesPerCycle
        };
    }

    public static float GetCurrentTornadoChance()
    {
        return CurrentIntensity switch
        {
            ThornRainIntensity.Light => LightTornadoChance,
            ThornRainIntensity.Normal => NormalTornadoChance,
            ThornRainIntensity.Heavy => HeavyTornadoChance,
            ThornRainIntensity.Storm => StormTornadoChance,
            _ => NormalTornadoChance
        };
    }

    // =========================================================
    // CICLO DE VIDA DO MUNDO
    // =========================================================

    public override void ClearWorld()
    {
        CurrentIntensity = ThornRainIntensity.Normal;
        _intensityDirection = 1;
        _intensityTimer = 0;
        _spawnTimer = 0;
        _currentPhaseDuration = NormalMinDurationTicks;
    }

    // =========================================================
    // ATUALIZAÇÃO DO MUNDO
    // =========================================================

    public override void PostUpdateWorld()
    {
        Main.raining = true;
        Main.rainTime = 86400;
        Main.maxRaining = 0.8f;

        // O servidor controla intensidade e spawns no multiplayer.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // Atualiza a progressão climática.
        UpdateIntensity();

        _spawnTimer++;

        if (_spawnTimer < GetCurrentSpawnInterval())
            return;

        _spawnTimer = 0;

        // Limite global de segurança.
        if (SpikesProjectile.ActiveCount >= MaxGlobalSpikes)
            return;

        SpawnSpikes();
    }

    // =========================================================
    // PROGRESSÃO DA INTENSIDADE
    // =========================================================

    private static void UpdateIntensity()
    {
        _intensityTimer++;

        if (_intensityTimer < _currentPhaseDuration)
            return;

        _intensityTimer = 0;

        ThornRainIntensity nextIntensity;

        switch (CurrentIntensity)
        {
            case ThornRainIntensity.Light:
                _intensityDirection = 1;
                nextIntensity = ThornRainIntensity.Normal;
                break;

            case ThornRainIntensity.Normal:
                nextIntensity = _intensityDirection > 0
                    ? ThornRainIntensity.Heavy
                    : ThornRainIntensity.Light;
                break;

            case ThornRainIntensity.Heavy:
                nextIntensity = _intensityDirection > 0
                    ? ThornRainIntensity.Storm
                    : ThornRainIntensity.Normal;
                break;

            case ThornRainIntensity.Storm:
                _intensityDirection = -1;
                nextIntensity = ThornRainIntensity.Heavy;
                break;

            default:
                _intensityDirection = 1;
                nextIntensity = ThornRainIntensity.Normal;
                break;
        }

        SetIntensity(nextIntensity);
    }

    public static void SetIntensity(ThornRainIntensity newIntensity)
    {
        if (CurrentIntensity == newIntensity)
            return;

        ThornRainIntensity previous = CurrentIntensity;
        CurrentIntensity = newIntensity;
        _intensityTimer = 0;
        _currentPhaseDuration = GetNewPhaseDuration(newIntensity);

        // Avisos ao jogador para transições climáticas significativas
        if (newIntensity == ThornRainIntensity.Heavy && previous < ThornRainIntensity.Heavy)
        {
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.RainIntensifying",
                new Color(230, 190, 90)
            );
        }
        else if (newIntensity == ThornRainIntensity.Storm)
        {
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.StormApproaching",
                new Color(255, 90, 70)
            );
        }

        // Sincroniza com os clientes no multiplayer
        SyncIntensity();
    }

    public static void SetIntensityFromNet(ThornRainIntensity intensity)
    {
        CurrentIntensity = intensity;
    }

    private static int GetNewPhaseDuration(ThornRainIntensity intensity)
    {
        return intensity switch
        {
            ThornRainIntensity.Light => Main.rand.Next(LightMinDurationTicks, LightMaxDurationTicks + 1),
            ThornRainIntensity.Normal => Main.rand.Next(NormalMinDurationTicks, NormalMaxDurationTicks + 1),
            ThornRainIntensity.Heavy => Main.rand.Next(HeavyMinDurationTicks, HeavyMaxDurationTicks + 1),
            ThornRainIntensity.Storm => Main.rand.Next(StormMinDurationTicks, StormMaxDurationTicks + 1),
            _ => NormalMinDurationTicks
        };
    }

    // =========================================================
    // MULTIPLAYER E MENSAGENS
    // =========================================================

    public static void SyncIntensity(int toWho = -1, int fromWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket packet = ModContent.GetInstance<O2ThornRain>().GetPacket();
        packet.Write((byte)MessageType.SyncThornRainIntensity);
        packet.Write((byte)CurrentIntensity);
        packet.Send(toWho, fromWho);
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)CurrentIntensity);
    }

    public override void NetReceive(BinaryReader reader)
    {
        CurrentIntensity = (ThornRainIntensity)reader.ReadByte();
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

    // =========================================================
    // SISTEMA DE SPAWN
    // =========================================================

    private static void SpawnSpikes()
    {
        int activePlayers = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (!player.active || player.dead)
                continue;

            if (!IsPlayerInRainZone(player))
                continue;

            activePlayers++;
        }

        if (activePlayers == 0)
            return;

        int remaining =
            MaxGlobalSpikes - SpikesProjectile.ActiveCount;

        if (remaining <= 0)
            return;

        int spikesPerCycle = GetCurrentSpikesPerCycle();

        for (int i = 0; i < Main.maxPlayers && remaining > 0; i++)
        {
            Player player = Main.player[i];

            if (!player.active || player.dead)
                continue;

            if (!IsPlayerInRainZone(player))
                continue;

            int nearSpikes = CountSpikesNearPlayer(player);
            if (nearSpikes >= MaxSpikesPerPlayer)
                continue;

            int toSpawnForPlayer = Math.Min(spikesPerCycle, MaxSpikesPerPlayer - nearSpikes);
            toSpawnForPlayer = Math.Min(toSpawnForPlayer, remaining);

            for (int s = 0; s < toSpawnForPlayer && remaining > 0; s++)
            {
                if (SpawnSpike(player))
                {
                    remaining--;
                }
            }
        }
    }

    // =========================================================
    // VERIFICAÇÃO DO JOGADOR
    // =========================================================

    public static bool IsPlayerInRainZone(Player player)
    {
        bool correctLayer =
            player.ZoneOverworldHeight ||
            player.ZoneSkyHeight;

        if (!correctLayer)
            return false;

        if (player.position.Y / 16f > Main.worldSurface)
            return false;

        return true;
    }

    // =========================================================
    // CONTAGEM LOCAL
    // =========================================================

    private static int CountSpikesNearPlayer(Player player)
    {
        int count = 0;

        const float maxDistance = 1500f;

        float maxDistanceSquared =
            maxDistance * maxDistance;

        int spikeType =
            ModContent.ProjectileType<SpikesProjectile>();

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];

            if (!projectile.active)
                continue;

            if (projectile.type != spikeType)
                continue;

            if (Vector2.DistanceSquared(
                    projectile.Center,
                    player.Center
                ) <= maxDistanceSquared)
            {
                count++;
            }
        }

        return count;
    }

    // =========================================================
    // CÁLCULO DO DANO
    // =========================================================

    public static int GetSpikeDamage()
    {
        // -----------------------------------------------------
        // PÓS-MOON LORD
        // -----------------------------------------------------

        if (NPC.downedMoonlord)
        {
            if (Main.masterMode)
                return MasterDamagePostMoonLord;

            if (Main.expertMode)
                return ExpertDamagePostMoonLord;

            return NormalDamagePostMoonLord;
        }

        // -----------------------------------------------------
        // PÓS-GOLEM
        // -----------------------------------------------------

        if (NPC.downedGolemBoss)
        {
            if (Main.masterMode)
                return MasterDamagePostGolem;

            if (Main.expertMode)
                return ExpertDamagePostGolem;

            return NormalDamagePostGolem;
        }

        // -----------------------------------------------------
        // PÓS-PLANTERA
        // -----------------------------------------------------

        if (NPC.downedPlantBoss)
        {
            if (Main.masterMode)
                return MasterDamagePostPlantera;

            if (Main.expertMode)
                return ExpertDamagePostPlantera;

            return NormalDamagePostPlantera;
        }

        // -----------------------------------------------------
        // HARDMODE
        // -----------------------------------------------------

        if (Main.hardMode)
        {
            if (Main.masterMode)
                return MasterDamageHardmode;

            if (Main.expertMode)
                return ExpertDamageHardmode;

            return NormalDamageHardmode;
        }

        // -----------------------------------------------------
        // PRÉ-HARDMODE
        // -----------------------------------------------------

        if (Main.masterMode)
            return MasterDamagePreHardmode;

        if (Main.expertMode)
            return ExpertDamagePreHardmode;

        return NormalDamagePreHardmode;
    }

    // =========================================================
    // CRIAÇÃO DO ESPINHO
    // =========================================================

    private static bool SpawnSpike(Player player)
    {
        float spawnX =
            player.Center.X +
            Main.rand.NextFloat(
                -SpawnHorizontalRange,
                SpawnHorizontalRange
            );

        float spawnY =
            player.Center.Y -
            Main.rand.NextFloat(
                SpawnHeightMin,
                SpawnHeightMax
            );

        Vector2 spawnPosition =
            new(spawnX, spawnY);

        // Evita nascer dentro de blocos.
        if (Collision.SolidCollision(
                spawnPosition,
                14,
                28))
        {
            return false;
        }

        // Vento influencia a trajetória.
        float speedX =
            Main.windSpeedCurrent * 8f +
            Main.rand.NextFloat(-1.5f, 1.5f);

        float speedY =
            Main.rand.NextFloat(12f, 18f);

        Vector2 velocity =
            new(speedX, speedY);

        int spikeType =
            ModContent.ProjectileType<SpikesProjectile>();

        Projectile.NewProjectile(
            Entity.GetSource_NaturalSpawn(),
            spawnPosition,
            velocity,
            spikeType,
            GetSpikeDamage(),
            3.5f,
            Main.myPlayer
        );

        return true;
    }
}