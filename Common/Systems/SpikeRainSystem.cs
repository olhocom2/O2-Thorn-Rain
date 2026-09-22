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

    // Quanto menor, mais espinhos são criados por segundo.
    // 3 = chuva bastante intensa.
    private const int SpawnInterval = 3;

    // Limite absoluto de espinhos ativos no mundo.
    private const int MaxGlobalSpikes = 120;

    // Limite aproximado de espinhos por jogador.
    private const int MaxSpikesPerPlayer = 70;

    // Área horizontal onde os espinhos podem nascer.
    private const float SpawnHorizontalRange = 1000f;

    // Altura acima do jogador onde os espinhos aparecem.
    private const float SpawnHeightMin = 550f;
    private const float SpawnHeightMax = 800f;

    // =========================================================
    // ATUALIZAÇÃO DO MUNDO
    // =========================================================

    public override void PostUpdateWorld()
    {
        // Mantém a chuva normal do Terraria ativa.
        Main.raining = true;
        Main.rainTime = 86400;
        Main.maxRaining = 0.8f;

        // No multiplayer, o servidor deve controlar os spawns.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        _spawnTimer++;

        if (_spawnTimer < SpawnInterval)
            return;

        _spawnTimer = 0;

        // Não cria mais se atingimos o limite global.
        if (SpikesProjectile.ActiveCount >= MaxGlobalSpikes)
            return;

        SpawnSpikes();
    }

    // =========================================================
    // SISTEMA DE SPAWN
    // =========================================================

    private void SpawnSpikes()
    {
        int activePlayers = 0;

        // Primeiro descobrimos quantos jogadores estão
        // participando da chuva.
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

        // Quantos espinhos ainda podemos criar?
        int remaining =
            MaxGlobalSpikes - SpikesProjectile.ActiveCount;

        if (remaining <= 0)
            return;

        // Quantidade criada a cada ciclo.
        //
        // 1 jogador = até 2
        // 2 jogadores = até 4
        // etc.
        //
        // O limite global continua protegendo o desempenho.
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

            // Verifica aproximadamente quantos espinhos
            // estão próximos desse jogador.
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

    private bool IsPlayerInRainZone(Player player)
    {
        bool correctLayer =
            player.ZoneOverworldHeight ||
            player.ZoneSkyHeight;

        if (!correctLayer)
            return false;

        // Não queremos chuva de espinhos no subterrâneo.
        if (player.position.Y / 16f > Main.worldSurface)
            return false;

        return true;
    }

    // =========================================================
    // CONTAGEM LOCAL
    // =========================================================

    private int CountSpikesNearPlayer(Player player)
    {
        int count = 0;

        const float maxDistance = 1500f;
        float maxDistanceSquared =
            maxDistance * maxDistance;

        int spikeType =
            ModContent.ProjectileType<SpikesProjectile>();

        // Essa busca só acontece quando realmente precisamos
        // decidir se podemos criar novos espinhos para esse jogador.
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];

            if (!projectile.active)
                continue;

            if (projectile.type != spikeType)
                continue;

            if (Vector2.DistanceSquared(
                    projectile.Center,
                    player.Center) <= maxDistanceSquared)
            {
                count++;
            }
        }

        return count;
    }

    // =========================================================
    // CRIAÇÃO DO ESPINHO
    // =========================================================

    private bool SpawnSpike(Player player)
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
            new Vector2(spawnX, spawnY);

        // Não nasce dentro de montanhas/blocos.
        if (Collision.SolidCollision(
                spawnPosition,
                14,
                28))
        {
            return false;
        }

        // Vento influencia levemente a trajetória.
        float speedX =
            Main.windSpeedCurrent * 8f +
            Main.rand.NextFloat(-1.5f, 1.5f);

        float speedY =
            Main.rand.NextFloat(12f, 18f);

        Vector2 velocity =
            new Vector2(speedX, speedY);

        int spikeType =
            ModContent.ProjectileType<SpikesProjectile>();

        // damage and more
        Projectile.NewProjectile(
            Entity.GetSource_NaturalSpawn(),
            spawnPosition,
            velocity,
            spikeType,
            40,
            3.5f,
            Main.myPlayer
        );

        return true;
    }
}