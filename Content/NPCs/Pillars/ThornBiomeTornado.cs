using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;
using O2ThornRain.Content.BossBars;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Content.NPCs.Pillars;

/// <summary>
/// Stationary ancestral biome tornado pillar (Jungle, Snow, Desert, Corruption).
/// Features independent health, contact damage, and biome-specific spike attack patterns.
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

        // Treated as mini-boss for HUD bar and map icon
        NPC.boss = true;
        NPC.friendly = false;
        NPC.BossBar = ModContent.GetInstance<ThornBiomeTornadoBar>();
    }

    public override bool CheckActive()
    {
        return false;
    }

    public override void AI()
    {
        NPC.velocity = Vector2.Zero;

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

        SpawnBiomeDust();

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

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

    // Jungle: vertical burst in tight spread
    private void UpdateJungleAttacks()
    {
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

    // Snow: fast diagonal and horizontal bursts
    private void UpdateSnowAttacks()
    {
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

    // Desert: 360-degree radial ring burst
    private void UpdateDesertAttacks()
    {
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

    // Corruption: alternating cadences and asymmetrical angles
    private void UpdateCorruptionAttacks()
    {
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

    private void SpawnBiomeDust()
    {
        if (!Main.rand.NextBool(3))
            return;

        int dustType = Biome switch
        {
            TornadoBiome.Jungle => DustID.JungleSpore,
            TornadoBiome.Snow => DustID.Ice,
            TornadoBiome.Desert => DustID.Sand,
            TornadoBiome.Corruption => DustID.Demonite,
            _ => DustID.Water
        };

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

        Color biomeTint = Biome switch
        {
            TornadoBiome.Jungle => new Color(110, 240, 150),
            TornadoBiome.Snow => new Color(140, 220, 255),
            TornadoBiome.Desert => new Color(245, 210, 120),
            TornadoBiome.Corruption => new Color(210, 130, 255),
            _ => Color.White
        };

        Color finalColor = NPC.GetAlpha(biomeTint * 0.85f);

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

        ThornStormEventSystem.RegisterTornadoDefeated(Biome);
    }
}
