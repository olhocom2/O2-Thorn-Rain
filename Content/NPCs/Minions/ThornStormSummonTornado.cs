using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;

namespace O2ThornRain.Content.NPCs.Minions;

/// <summary>
/// Ocean vortex trigger for "The Storm of Four" event.
/// Peaceful, stationary trigger destroyed by the player to initiate the storm.
/// </summary>
public class ThornStormSummonTornado : ModNPC
{
    public override string Texture =>
        $"Terraria/Images/Projectile_{ProjectileID.Tempest}";

    private const int FrameCount = 6;
    private const int AnimationSpeed = 4;

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
        NPC.width = 44;
        NPC.height = 70;

        NPC.damage = 0;
        NPC.defense = 0;
        NPC.lifeMax = 250;

        NPC.HitSound = SoundID.NPCHit3;
        NPC.DeathSound = SoundID.NPCDeath3;

        NPC.knockBackResist = 0f;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;

        NPC.dontTakeDamageFromHostiles = true;
        NPC.friendly = false;
    }

    public override bool CheckActive()
    {
        return false;
    }

    public override void AI()
    {
        NPC.velocity = Vector2.Zero;

        if (Main.rand.NextBool(4))
        {
            Dust dust = Dust.NewDustDirect(
                NPC.position,
                NPC.width,
                NPC.height,
                DustID.Water,
                0f,
                Main.rand.NextFloat(-2f, -0.5f),
                100,
                default,
                Main.rand.NextFloat(0.8f, 1.2f)
            );
            dust.noGravity = true;
        }
    }

    public override void FindFrame(int frameHeight)
    {
        NPC.frameCounter++;
        if (NPC.frameCounter >= AnimationSpeed)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y += frameHeight;

            if (NPC.frame.Y >= frameHeight * FrameCount)
            {
                NPC.frame.Y = 0;
            }
        }
    }

    public override void OnKill()
    {
        SoundEngine.PlaySound(
            SoundID.Item122 with
            {
                Volume = 0.8f,
                Pitch = -0.2f
            },
            NPC.Center
        );

        for (int i = 0; i < 20; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
            Dust.NewDustPerfect(
                NPC.Center,
                DustID.Water,
                velocity,
                100,
                default,
                Main.rand.NextFloat(1.0f, 1.5f)
            );
        }

        ThornStormEventSystem.StartEvent();
    }
}
