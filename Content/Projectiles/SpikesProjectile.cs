using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace O2ThornRain.Content.Projectiles;

class SpikesProjectile : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 28;

        Projectile.friendly = true;
        Projectile.hostile = true;

        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.aiStyle = 0;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

        Projectile.velocity.Y += 0.12f;
        if (Projectile.velocity.Y > 16f)
        {
            Projectile.velocity.Y = 16f;
        }

        if (Main.rand.NextBool(8))
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0f, 0f, 100, default, 0.9f);
            dust.noGravity = true;
        }
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, PitchVariance = 0.2f }, Projectile.position);

        for (int i = 0; i < 2; i++)
        {
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Iron, Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f);
        }
    }
}