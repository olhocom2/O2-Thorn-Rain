using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.NPCs.Boss;

namespace O2ThornRain.Content.NPCs.Minions;

/// <summary>
/// Hostile mini-tornado minion spawned by the Eye of the Thorn Storm boss.
/// Actively tracks and attacks the target player, scaling in speed with boss phases.
/// </summary>
public class ThornMinionTornado : ModNPC
{
    public override string Texture =>
        $"Terraria/Images/Projectile_{ProjectileID.Tempest}";

    private const int FrameCount = 6;
    private const int AnimationSpeed = 4;

    private ref float Phase => ref NPC.ai[0];
    private ref float ParentBossIndex => ref NPC.ai[1];

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
        NPC.width = 36;
        NPC.height = 56;

        NPC.damage = 38;
        NPC.defense = 10;
        NPC.lifeMax = 300;

        NPC.HitSound = SoundID.NPCHit3;
        NPC.DeathSound = SoundID.NPCDeath3;

        NPC.knockBackResist = 0.35f;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;

        NPC.friendly = false;
        NPC.value = 0f;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        NPC.lifeMax = (int)(NPC.lifeMax * 0.70f * balance * bossAdjustment);
        if (Main.masterMode)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 1.35f);
            NPC.damage = 62;
            NPC.defense = 16;
        }
        else if (Main.expertMode)
        {
            NPC.damage = 48;
            NPC.defense = 12;
        }
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (Main.expertMode)
        {
            target.AddBuff(BuffID.Bleeding, 240);
        }
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter++;
        if (NPC.frameCounter >= AnimationSpeed)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y = (NPC.frame.Y + frameHeight) % (FrameCount * frameHeight);
        }
    }

    public override void AI()
    {
        // 1. Verify parent boss validity
        int bossIdx = (int)ParentBossIndex;
        int bossType = ModContent.NPCType<ThornStormBoss>();
        if (bossIdx < 0 || bossIdx >= Main.maxNPCs || !Main.npc[bossIdx].active || Main.npc[bossIdx].type != bossType)
        {
            Dissipate();
            return;
        }

        // 2. Target player
        if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
        {
            NPC.TargetClosest(true);
        }

        Player target = Main.player[NPC.target];
        if (!target.active || target.dead)
        {
            Dissipate();
            return;
        }

        // 3. Movement speed scaling by boss phase & difficulty
        float maxSpeed = Phase switch
        {
            >= 3f => 10.5f,
            >= 2f => 7.5f,
            _ => 4.8f
        };

        float accel = Phase switch
        {
            >= 3f => 0.26f,
            >= 2f => 0.16f,
            _ => 0.09f
        };

        float diffMult = Main.masterMode ? 1.25f : (Main.expertMode ? 1.12f : 1.0f);
        maxSpeed *= diffMult;
        accel *= diffMult;

        Vector2 toTarget = target.Center - NPC.Center;
        float distance = toTarget.Length();

        if (distance > 0f)
        {
            toTarget.Normalize();
            toTarget *= maxSpeed;

            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, toTarget.X, accel);
            NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, toTarget.Y, accel);
        }

        // 4. Ambient particles
        if (Main.rand.NextBool(3))
        {
            Dust dust = Dust.NewDustDirect(
                NPC.position,
                NPC.width,
                NPC.height,
                Main.rand.NextBool() ? DustID.CrimsonTorch : DustID.Blood,
                NPC.velocity.X * 0.2f,
                NPC.velocity.Y * 0.2f,
                100,
                default,
                Main.rand.NextFloat(0.9f, 1.4f)
            );
            dust.noGravity = true;
        }

        NPC.rotation = NPC.velocity.X * 0.04f;
    }

    private void Dissipate()
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrimsonTorch, speed.X, speed.Y, 120, default, 1.2f);
        }
        NPC.active = false;
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        int dustCount = NPC.life <= 0 ? 25 : 5;
        for (int i = 0; i < dustCount; i++)
        {
            Vector2 speed = Main.rand.NextVector2Circular(4f, 4f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrimsonTorch, speed.X, speed.Y, 100, default, 1.3f);
        }
    }
}
