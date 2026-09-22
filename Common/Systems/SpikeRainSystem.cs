using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Systems;

public class SpikeRainSystem : ModSystem
{
    private int _spawnTimer;

    // =========================================================
    // CONFIGURAÇÃO DA CHUVA
    // =========================================================

    private const int SpawnInterval = 3;

    private const int MaxGlobalSpikes = 120;

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
    // ATUALIZAÇÃO DO MUNDO
    // =========================================================

    public override void PostUpdateWorld()
    {
        Main.raining = true;
        Main.rainTime = 86400;
        Main.maxRaining = 0.8f;

        // O servidor controla os spawns no multiplayer.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        _spawnTimer++;

        if (_spawnTimer < SpawnInterval)
            return;

        _spawnTimer = 0;

        // Limite global de segurança.
        if (SpikesProjectile.ActiveCount >= MaxGlobalSpikes)
            return;

        SpawnSpikes();
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

        int spawnCount = Math.Min(
            activePlayers * 2,
            remaining
        );

        for (int i = 0;
             i < Main.maxPlayers && spawnCount > 0;
             i++)
        {
            Player player = Main.player[i];

            if (!player.active || player.dead)
                continue;

            if (!IsPlayerInRainZone(player))
                continue;

            if (CountSpikesNearPlayer(player) >= MaxSpikesPerPlayer)
                continue;

            if (SpawnSpike(player))
            {
                spawnCount--;
            }
        }
    }

    // =========================================================
    // VERIFICAÇÃO DO JOGADOR
    // =========================================================

    private static bool IsPlayerInRainZone(Player player)
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

    private static int GetSpikeDamage()
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