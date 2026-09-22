using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Buffs;

namespace O2ThornRain.Content.Mounts;

/// <summary>
/// Montaria voadora concedida como recompensa máxima ao derrotar o evento "A Tempestade dos Quatro".
/// Concede voo infinito, agilidade e imunidade exclusiva aos espinhos da Thorn Rain (SpikesProjectile).
/// </summary>
public class DragonMount : ModMount
{
    public override void SetStaticDefaults()
    {
        // Buff associado
        MountData.buff = ModContent.BuffType<DragonMountBuff>();

        // Parâmetros de movimentação e voo ágil
        MountData.flightTimeMax = int.MaxValue; // Voo infinito
        MountData.fatigueMax = int.MaxValue;
        MountData.fallDamage = 0f;
        MountData.heightBoost = 20;

        MountData.runSpeed = 12f;
        MountData.dashSpeed = 12f;
        MountData.acceleration = 0.25f;
        MountData.jumpHeight = 12;
        MountData.jumpSpeed = 8f;
        MountData.blockExtraJumps = true;

        MountData.spawnDust = DustID.CrimsonTorch;

        // Herda a configuração de frames e textura do CuteFishron como fallback seguro
        // até que um sprite dedicado de dragão seja adicionado pelo criador
        if (Mount.mounts != null && Mount.mounts[MountID.CuteFishron] != null)
        {
            Mount.MountData baseMount = Mount.mounts[MountID.CuteFishron];
            MountData.backTexture = baseMount.backTexture;
            MountData.backTextureGlow = baseMount.backTextureGlow;
            MountData.backTextureExtra = baseMount.backTextureExtra;
            MountData.frontTexture = baseMount.frontTexture;
            MountData.frontTextureGlow = baseMount.frontTextureGlow;
            MountData.frontTextureExtra = baseMount.frontTextureExtra;

            MountData.textureWidth = baseMount.textureWidth;
            MountData.textureHeight = baseMount.textureHeight;
            MountData.xOffset = baseMount.xOffset;
            MountData.yOffset = baseMount.yOffset;
            MountData.playerXOffset = baseMount.playerXOffset;
            MountData.playerYOffsets = baseMount.playerYOffsets;

            MountData.standingFrameCount = baseMount.standingFrameCount;
            MountData.standingFrameDelay = baseMount.standingFrameDelay;
            MountData.standingFrameStart = baseMount.standingFrameStart;

            MountData.runningFrameCount = baseMount.runningFrameCount;
            MountData.runningFrameDelay = baseMount.runningFrameDelay;
            MountData.runningFrameStart = baseMount.runningFrameStart;

            MountData.flyingFrameCount = baseMount.flyingFrameCount;
            MountData.flyingFrameDelay = baseMount.flyingFrameDelay;
            MountData.flyingFrameStart = baseMount.flyingFrameStart;

            MountData.inAirFrameCount = baseMount.inAirFrameCount;
            MountData.inAirFrameDelay = baseMount.inAirFrameDelay;
            MountData.inAirFrameStart = baseMount.inAirFrameStart;

            MountData.idleFrameCount = baseMount.idleFrameCount;
            MountData.idleFrameDelay = baseMount.idleFrameDelay;
            MountData.idleFrameStart = baseMount.idleFrameStart;
            MountData.idleFrameLoop = baseMount.idleFrameLoop;

            MountData.swimFrameCount = baseMount.swimFrameCount;
            MountData.swimFrameDelay = baseMount.swimFrameDelay;
            MountData.swimFrameStart = baseMount.swimFrameStart;

            MountData.dashingFrameCount = baseMount.dashingFrameCount;
            MountData.dashingFrameDelay = baseMount.dashingFrameDelay;
            MountData.dashingFrameStart = baseMount.dashingFrameStart;
        }
    }

    public override void UpdateEffects(Player player)
    {
        // Rastro de poeira tempestuosa enquanto o jogador estiver em movimento no ar
        if (player.velocity.LengthSquared() > 4f && Main.rand.NextBool(3))
        {
            Dust dust = Dust.NewDustDirect(
                player.position,
                player.width,
                player.height,
                DustID.CrimsonTorch,
                -player.velocity.X * 0.25f,
                -player.velocity.Y * 0.25f,
                100,
                default,
                Main.rand.NextFloat(1.1f, 1.5f)
            );
            dust.noGravity = true;
        }
    }
}
