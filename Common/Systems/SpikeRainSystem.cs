using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Systems
{
    public class SpikeRainSystem : ModSystem
    {
        private int _spawnTimer = 0;
        private const int SpawnInterval = 8; // A cada 8 ticks (~7.5 espinhos por segundo)
        private const int MaxActiveSpikes = 35; // Limite global de espinhos simultâneos para evitar queda de FPS

        public override void PostUpdateWorld()
        {
            Main.raining = true;
            Main.rainTime = 86400;
            Main.maxRaining = 0.8f;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Controle de cadência de spawn
            _spawnTimer++;
            if (_spawnTimer < SpawnInterval)
            {
                return;
            }
            _spawnTimer = 0;

            // Limite global de espinhos ativos no mundo para preservar FPS
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int activeSpikes = 0;
            for (int p = 0; p < Main.maxProjectiles; p++)
            {
                if (Main.projectile[p].active && Main.projectile[p].type == spikeType)
                {
                    activeSpikes++;
                }
            }

            if (activeSpikes >= MaxActiveSpikes)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                // Cálculo idêntico ao da chuva: somente na superfície ou céu
                bool isSurface = (player.ZoneOverworldHeight || player.ZoneSkyHeight) && (player.position.Y / 16f <= Main.worldSurface);
                if (!isSurface)
                {
                    continue;
                }

                float spawnX = player.position.X + Main.rand.NextFloat(-950f, 950f);
                float spawnY = player.position.Y - Main.rand.NextFloat(600f, 850f);

                // Garante que o ponto de nascimento não fique abaixo da camada de superfície
                if (spawnY / 16f > Main.worldSurface)
                {
                    continue;
                }

                Vector2 spawnPos = new Vector2(spawnX, spawnY);

                // Evita spawnar dentro de montanhas ou blocos sólidos
                if (Collision.SolidCollision(spawnPos, 14, 28))
                {
                    continue;
                }

                float speedX = (Main.windSpeedCurrent * 8f) + Main.rand.NextFloat(-1.5f, 1.5f);
                float speedY = Main.rand.NextFloat(12f, 18f);

                Vector2 velocity = new Vector2(speedX, speedY);

                int damage = 10;
                float knockBack = 3.5f;

                Projectile.NewProjectile(
                    Entity.GetSource_NaturalSpawn(),
                    spawnPos,
                    velocity,
                    spikeType,
                    damage,
                    knockBack,
                    Main.myPlayer
                );
            }
        }
    }
}