using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Players;

namespace O2ThornRain.Content.Projectiles;

public class SpikesProjectile : ModProjectile
{
    // =========================================================
    // CONTADOR GLOBAL
    // =========================================================

    // Quantos espinhos desse tipo estão ativos atualmente.
    public static int ActiveCount;

    // =========================================================
    // CONFIGURAÇÃO
    // =========================================================

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 28;

        // O espinho pode causar dano ao jogador.
        Projectile.hostile = true;

        // Não precisamos que ele seja friendly.
        Projectile.friendly = false;

        Projectile.penetrate = 1;

        // 180 ticks = aproximadamente 3 segundos.
        //
        // Antes você tinha 300.
        // Isso reduz bastante a quantidade de projéteis
        // que ficam vivos sem necessidade.
        Projectile.timeLeft = 180;

        Projectile.aiStyle = 0;

        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
    }

    // =========================================================
    // NASCIMENTO
    // =========================================================

    public override void OnSpawn(
        Terraria.DataStructures.IEntitySource source)
    {
        ActiveCount++;
    }

    // =========================================================
    // MOVIMENTO
    // =========================================================

    public override void AI()
    {
        // Faz o espinho apontar na direção da queda.
        Projectile.rotation =
            Projectile.velocity.ToRotation()
            - MathHelper.PiOver2;

        // Gravidade.
        Projectile.velocity.Y += 0.12f;

        // Velocidade vertical máxima.
        if (Projectile.velocity.Y > 16f)
        {
            Projectile.velocity.Y = 16f;
        }

        // =====================================================
        // EFEITO VISUAL
        // =====================================================
        //
        // Antes:
        //
        // 1 Dust a cada 8 ticks
        //
        // Agora:
        //
        // 1 Dust a cada 15 ticks.
        //
        // Com 100 espinhos isso reduz bastante a quantidade
        // de partículas criadas.
        //

        if (Main.rand.NextBool(15))
        {
            Dust dust = Dust.NewDustDirect(
                Projectile.position,
                Projectile.width,
                Projectile.height,
                DustID.Water,
                0f,
                0f,
                100,
                default,
                0.7f
            );

            dust.noGravity = true;
        }
    }

    // =========================================================
    // MORTE
    // =========================================================

    public override void OnKill(int timeLeft)
    {
        // Mantém o contador sincronizado.
        ActiveCount--;

        // Segurança contra valores negativos.
        if (ActiveCount < 0)
        {
            ActiveCount = 0;
        }

        // =====================================================
        // IMPACTO
        // =====================================================

        Collision.HitTiles(
            Projectile.position,
            Projectile.velocity,
            Projectile.width,
            Projectile.height
        );

        // =====================================================
        // SOM
        // =====================================================
        //
        // Não queremos 100 sons acontecendo ao mesmo tempo.
        //
        // Apenas alguns espinhos fazem som.
        //

        if (Main.rand.NextBool(4))
        {
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Dig with
                {
                    Volume = 0.25f,
                    PitchVariance = 0.3f
                },
                Projectile.position
            );
        }

        // =====================================================
        // PARTICULAS DE IMPACTO
        // =====================================================

        if (Main.rand.NextBool(2))
        {
            Dust.NewDust(
                Projectile.position,
                Projectile.width,
                Projectile.height,
                DustID.Iron,
                Projectile.velocity.X * 0.2f,
                -Projectile.velocity.Y * 0.2f
            );
        }
    }

    // =========================================================
    // PROTEÇÃO CONTRA DANO
    // =========================================================

    public override bool CanHitPlayer(Player target)
    {
        // Se o jogador estiver protegido por Pele de Ferro ou Guarda-chuva com durabilidade:
        if (target.GetModPlayer<ThornRainPlayer>().IsProtectedFromSpikes)
        {
            return false;
        }

        return base.CanHitPlayer(target);
    }
}