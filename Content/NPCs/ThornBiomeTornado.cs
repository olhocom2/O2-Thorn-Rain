using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Content.NPCs;

/// <summary>
/// Tornado ancestral estacionário associado a um dos 4 biomas (Selva, Neve, Deserto, Corrupção).
/// Possui vida própria, causa dano por contato físico, dispara espinhos com padrões distintos por bioma
/// e não destrói blocos nem se move pelo cenário.
/// </summary>
public class ThornBiomeTornado : ModNPC
{
    public override string Texture =>
        $"Terraria/Images/Projectile_{ProjectileID.Cthulunado}";

    private const int FrameCount = 6;
    private const int AnimationSpeed = 4;

    public TornadoBiome Biome => (TornadoBiome)(int)NPC.ai[0];

    private ref float AttackTimer => ref NPC.ai[1];
    private ref float InternalCounter => ref NPC.ai[2];

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = FrameCount;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
    }

    public override void SetDefaults()
    {
        NPC.width = 140;
        NPC.height = 320;

        NPC.damage = 50;
        NPC.defense = 20;
        NPC.lifeMax = 6000;

        NPC.HitSound = SoundID.NPCHit3;
        NPC.DeathSound = SoundID.NPCDeath3;

        NPC.knockBackResist = 0f;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;

        NPC.boss = false;
        NPC.friendly = false;
    }

    public override bool CheckActive()
    {
        // Impede que os tornados de bioma desapareçam quando o jogador estiver longe
        return false;
    }

    public override void AI()
    {
        // Permanece estacionário no local de spawn
        NPC.velocity = Vector2.Zero;

        // Animação de rotação suave dos frames
        NPC.frameCounter++;
        if (NPC.frameCounter >= AnimationSpeed)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y += NPC.height;
            if (NPC.frame.Y >= NPC.height * FrameCount)
            {
                NPC.frame.Y = 0;
            }
        }

        // Partículas temáticas ao redor do corpo
        SpawnBiomeDust();

        // O servidor controla o disparo dos projéteis
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // Ataques periódicos com respeito estrito ao limite global de espinhos
        if (SpikesProjectile.ActiveCount >= SpikeRainSystem.MaxGlobalSpikes)
            return;

        AttackTimer++;

        switch (Biome)
        {
            case TornadoBiome.Jungle:
                UpdateJungleAttacks();
                break;

            case TornadoBiome.Snow:
                UpdateSnowAttacks();
                break;

            case TornadoBiome.Desert:
                UpdateDesertAttacks();
                break;

            case TornadoBiome.Corruption:
                UpdateCorruptionAttacks();
                break;
        }
    }

    // =========================================================
    // PADRÕES DE ATAQUE DISTINTOS POR BIOMA
    // =========================================================

    /// <summary>
    /// SELVA: Ataques predominantemente verticais e chuva concentrada.
    /// </summary>
    private void UpdateJungleAttacks()
    {
        // A cada 45 ticks (0,75s), lança rajada para cima em leque fechado
        if (AttackTimer >= 45)
        {
            AttackTimer = 0;

            int count = Math.Min(3, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = new(
                    Main.rand.NextFloat(-2.5f, 2.5f),
                    Main.rand.NextFloat(-16f, -12f)
                );

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center + new Vector2(Main.rand.NextFloat(-30f, 30f), -80f),
                    velocity,
                    spikeType,
                    damage,
                    3.5f,
                    Main.myPlayer
                );
            }
        }
    }

    /// <summary>
    /// NEVE: Espinhos velozes com rajadas horizontais e diagonais.
    /// </summary>
    private void UpdateSnowAttacks()
    {
        // A cada 35 ticks (aprox. 0,6s), dispara rajadas rápidas diagonais/laterais
        if (AttackTimer >= 35)
        {
            AttackTimer = 0;

            int count = Math.Min(2, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            for (int i = 0; i < count; i++)
            {
                bool toLeft = Main.rand.NextBool();
                float angle = toLeft
                    ? MathHelper.ToRadians(Main.rand.NextFloat(140f, 220f))
                    : MathHelper.ToRadians(Main.rand.NextFloat(-40f, 40f));

                float speed = Main.rand.NextFloat(14f, 18f);
                Vector2 velocity = angle.ToRotationVector2() * speed;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    velocity,
                    spikeType,
                    damage,
                    3.5f,
                    Main.myPlayer
                );
            }
        }
    }

    /// <summary>
    /// DESERTO: Explosões radiais em 360 graus em campo aberto.
    /// </summary>
    private void UpdateDesertAttacks()
    {
        // A cada 60 ticks (1 segundo), dispara uma estrela radial de 6 espinhos
        if (AttackTimer >= 60)
        {
            AttackTimer = 0;

            int desired = 6;
            int available = SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount;
            int count = Math.Min(desired, available);

            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            for (int i = 0; i < count; i++)
            {
                float angle = i * (MathHelper.TwoPi / desired);
                Vector2 velocity = angle.ToRotationVector2() * 12f;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    velocity,
                    spikeType,
                    damage,
                    3.5f,
                    Main.myPlayer
                );
            }
        }
    }

    /// <summary>
    /// CORRUPÇÃO: Cadência imprevisível e rajadas alternadas.
    /// </summary>
    private void UpdateCorruptionAttacks()
    {
        // Alterna entre intervalo curto (25 ticks) e longo (55 ticks)
        float targetInterval = (InternalCounter % 2 == 0) ? 25f : 55f;

        if (AttackTimer >= targetInterval)
        {
            AttackTimer = 0;
            InternalCounter++;

            int count = Math.Min(2, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            for (int i = 0; i < count; i++)
            {
                // Ângulo oscilante e assimétrico
                float baseAngle = (InternalCounter % 2 == 0) ? -60f : 120f;
                float angle = MathHelper.ToRadians(baseAngle + Main.rand.NextFloat(-35f, 35f));
                float speed = Main.rand.NextFloat(11f, 16f);

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center + new Vector2(Main.rand.NextFloat(-20f, 20f), 0f),
                    angle.ToRotationVector2() * speed,
                    spikeType,
                    damage,
                    3.5f,
                    Main.myPlayer
                );
            }
        }
    }

    // =========================================================
    // VISUAL E PARTÍCULAS
    // =========================================================

    private void SpawnBiomeDust()
    {
        if (!Main.rand.NextBool(3))
            return;

        int dustType;
        switch (Biome)
        {
            case TornadoBiome.Jungle:
                dustType = DustID.JungleSpore;
                break;

            case TornadoBiome.Snow:
                dustType = DustID.Ice;
                break;

            case TornadoBiome.Desert:
                dustType = DustID.Sand;
                break;

            case TornadoBiome.Corruption:
                dustType = DustID.Demonite;
                break;

            default:
                dustType = DustID.Water;
                break;
        }

        Vector2 offset = new(
            Main.rand.NextFloat(-NPC.width * 0.45f, NPC.width * 0.45f),
            Main.rand.NextFloat(-NPC.height * 0.45f, NPC.height * 0.45f)
        );

        Dust dust = Dust.NewDustDirect(
            NPC.Center + offset,
            1,
            1,
            dustType,
            0f,
            Main.rand.NextFloat(-2f, 1f),
            100,
            default,
            Main.rand.NextFloat(0.8f, 1.3f)
        );
        dust.noGravity = true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = TextureAssets.Projectile[ProjectileID.Cthulunado].Value;

        Rectangle frame = texture.Frame(
            1,
            FrameCount,
            0,
            (int)(NPC.frame.Y / (float)NPC.height) % FrameCount
        );

        Vector2 origin = frame.Size() / 2f;
        Vector2 drawPosition = NPC.Center - screenPos;

        // Coloração temática sutil por bioma
        Color biomeTint = Biome switch
        {
            TornadoBiome.Jungle => new Color(110, 240, 150),
            TornadoBiome.Snow => new Color(140, 220, 255),
            TornadoBiome.Desert => new Color(245, 210, 120),
            TornadoBiome.Corruption => new Color(210, 130, 255),
            _ => Color.White
        };

        Color finalColor = NPC.GetAlpha(biomeTint * 0.85f);

        // Desenha camadas sobrepostas para dar densidade visual à coluna
        for (int i = 0; i < 6; i++)
        {
            float verticalOffset = (i - 2.5f) * 45f;
            float scaleX = 1.3f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i) * 0.1f;

            spriteBatch.Draw(
                texture,
                drawPosition + new Vector2(0f, verticalOffset),
                frame,
                finalColor,
                (i % 2 == 0 ? 1 : -1) * Main.GlobalTimeWrappedHourly * 0.5f,
                origin,
                new Vector2(scaleX, 0.9f),
                SpriteEffects.None,
                0f
            );
        }

        return false;
    }

    // =========================================================
    // FINALIZAÇÃO E REGISTRO
    // =========================================================

    public override void OnKill()
    {
        SoundEngine.PlaySound(
            SoundID.Item122 with
            {
                Volume = 0.9f,
                Pitch = -0.3f
            },
            NPC.Center
        );

        // Dispersão de partículas temáticas
        for (int i = 0; i < 25; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
            Dust.NewDustPerfect(
                NPC.Center,
                DustID.Water,
                velocity,
                100,
                default,
                Main.rand.NextFloat(1.0f, 1.6f)
            );
        }

        // Notifica o sistema do evento da derrota deste tornado
        ThornStormEventSystem.RegisterTornadoDefeated(Biome);
    }
}
