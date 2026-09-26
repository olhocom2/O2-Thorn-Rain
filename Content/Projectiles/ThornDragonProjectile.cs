using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace O2ThornRain.Content.Projectiles;

/// <summary>
/// Dragão de espinhos cuspido pelo ThornStormBoss durante a tempestade final.
/// Persegue o jogador com movimento serpentino e alta velocidade, deixando
/// rastro de partículas e espinhos, desaparecendo após determinada duração.
/// </summary>
public class ThornDragonProjectile : ModProjectile
{
    // Reutiliza o sprite da cabeça do dragão do Cultist do vanilla
    public override string Texture =>
        $"Terraria/Images/NPC_{NPCID.CultistDragonHead}";

    public const int ThornDragonDamage = 65;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = 44;
        Projectile.height = 44;

        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;

        Projectile.penetrate = -1;
        Projectile.timeLeft = 360; // 6 segundos de vida
        Projectile.alpha = 255;
    }

    public override void AI()
    {
        // Fade in suave ao nascer
        if (Projectile.alpha > 0)
        {
            Projectile.alpha = Math.Max(0, Projectile.alpha - 25);
        }

        // Som ocasional de vento e vôo ameaçador
        if (Projectile.timeLeft % 60 == 0)
        {
            SoundEngine.PlaySound(
                SoundID.Item20 with
                {
                    Volume = 0.5f,
                    Pitch = -0.3f
                },
                Projectile.Center
            );
        }

        // Partículas temáticas no rastro (espinhos e tempestade vermelha)
        if (Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustDirect(
                Projectile.position,
                Projectile.width,
                Projectile.height,
                DustID.CrimsonTorch,
                -Projectile.velocity.X * 0.2f,
                -Projectile.velocity.Y * 0.2f,
                100,
                default,
                Main.rand.NextFloat(1.1f, 1.6f)
            );
            dust.noGravity = true;

            if (Main.rand.NextBool(3))
            {
                Dust spore = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.JungleSpore,
                    0f,
                    0f,
                    100,
                    default,
                    0.9f
                );
                spore.noGravity = true;
            }
        }

        // -------------------------------------------------------------
        // MOVIMENTAÇÃO SERPENTINA E PERSEGUIÇÃO AO JOGADOR
        // -------------------------------------------------------------
        Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        if (target != null && target.active && !target.dead)
        {
            Vector2 toTarget = target.Center - Projectile.Center;
            float distance = toTarget.Length();

            if (distance > 0f)
            {
                toTarget.Normalize();

                // Interpolação de direção com inércia para formar curvas naturais
                float speed = 15f;
                float turnSpeed = 0.05f;

                // Ondulação serpentina perpendicular à velocidade
                Vector2 perpendicular = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X);
                if (perpendicular != Vector2.Zero)
                    perpendicular.Normalize();

                float sineWave = (float)Math.Sin(Projectile.timeLeft * 0.18f) * 2.5f;

                Vector2 desiredVelocity = toTarget * speed + perpendicular * sineWave;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, turnSpeed);
            }
        }

        // Rotação alinhada com a velocidade
        if (Projectile.velocity != Vector2.Zero)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Npc[NPCID.CultistDragonHead].Value;
        Vector2 origin = texture.Size() / 2f;

        // Desenha o rastro espectral vermelho da criatura
        for (int i = 0; i < Projectile.oldPos.Length; i++)
        {
            if (Projectile.oldPos[i] == Vector2.Zero) continue;

            float factor = 1f - (i / (float)Projectile.oldPos.Length);
            Color trailColor = new Color(255, 40, 50, 0) * factor * 0.6f;
            Vector2 drawPos = Projectile.oldPos[i] + (Projectile.Size / 2f) - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                trailColor,
                Projectile.oldRot[i],
                origin,
                Projectile.scale * factor * 0.9f,
                SpriteEffects.None,
                0
            );
        }

        // Desenha a cabeça principal com coloração avermelhada de tempestade
        Vector2 mainDrawPos = Projectile.Center - Main.screenPosition;
        Color headColor = Projectile.GetAlpha(new Color(255, 80, 80));

        Main.EntitySpriteDraw(
            texture,
            mainDrawPos,
            null,
            headColor,
            Projectile.rotation,
            origin,
            Projectile.scale * 1.15f,
            SpriteEffects.None,
            0
        );

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(
            SoundID.Item14 with
            {
                Volume = 0.6f,
                Pitch = -0.2f
            },
            Projectile.Center
        );

        // Explosão de poeira e espinhos ao se dissipar
        for (int i = 0; i < 20; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
            Dust.NewDustPerfect(
                Projectile.Center,
                DustID.CrimsonTorch,
                velocity,
                100,
                default,
                Main.rand.NextFloat(1.2f, 1.8f)
            );
        }
    }
}
