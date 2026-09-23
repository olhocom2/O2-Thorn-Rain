using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Buffs;

namespace O2ThornRain.Content.Mounts;

/// <summary>
/// Montaria voadora do Dragão do Cultista concedida como recompensa máxima ao derrotar o evento "A Tempestade dos Quatro".
/// Concede voo infinito, agilidade extrema e imunidade exclusiva aos espinhos da Thorn Rain (SpikesProjectile).
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
        MountData.usesHover = true;

        MountData.spawnDust = DustID.CrimsonTorch;

        // Animação e contagem exata de frames para a textura (8 frames)
        MountData.totalFrames = 8;
        MountData.playerYOffsets = new int[] { 14, 12, 10, 12, 14, 16, 14, 12 };
        MountData.xOffset = 10;
        MountData.yOffset = 4;
        MountData.playerXOffset = 0;
        MountData.playerHeadOffset = 18;
        MountData.bodyFrame = 3;

        // Animação de repouso / parado (frames 0 a 3)
        MountData.standingFrameCount = 4;
        MountData.standingFrameStart = 0;
        MountData.standingFrameDelay = 10;

        // Animação em movimento no solo (frames 0 a 3)
        MountData.runningFrameCount = 4;
        MountData.runningFrameStart = 0;
        MountData.runningFrameDelay = 8;

        // Animação de voo (frames 4 a 7)
        MountData.flyingFrameCount = 4;
        MountData.flyingFrameStart = 4;
        MountData.flyingFrameDelay = 6;

        // Animação no ar / planando (frames 4 a 7)
        MountData.inAirFrameCount = 4;
        MountData.inAirFrameStart = 4;
        MountData.inAirFrameDelay = 6;

        MountData.idleFrameCount = 0;
        MountData.idleFrameStart = 0;
        MountData.idleFrameDelay = 0;
        MountData.idleFrameLoop = false;

        MountData.swimFrameCount = 4;
        MountData.swimFrameStart = 4;
        MountData.swimFrameDelay = 6;

        MountData.dashingFrameCount = 0;
        MountData.dashingFrameStart = 0;
        MountData.dashingFrameDelay = 0;

        // Dimensões da textura: folha com 8 quadros de 120x80 px cada (total 120x640 px)
        MountData.textureWidth = 120;
        MountData.textureHeight = 640;

        // Carregamento dedicado da textura do Dragão do Cultista pelo mod
        if (!Main.dedServ)
        {
            MountData.backTexture = ModContent.Request<Texture2D>("O2ThornRain/Content/Mounts/DragonMount");

            if (MountData.backTexture != null && MountData.backTexture.IsLoaded)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }
    }

    public override void SetMount(Player player, ref bool skipDust)
    {
        // Garante a integridade das dimensões e frames em tempo de execução
        if (!Main.dedServ)
        {
            if (MountData.backTexture == null || MountData.backTexture == Asset<Texture2D>.Empty)
            {
                MountData.backTexture = ModContent.Request<Texture2D>("O2ThornRain/Content/Mounts/DragonMount");
            }

            if (MountData.backTexture != null && MountData.backTexture.IsLoaded)
            {
                if (MountData.textureWidth <= 0)
                    MountData.textureWidth = MountData.backTexture.Width();
                if (MountData.textureHeight <= 0)
                    MountData.textureHeight = MountData.backTexture.Height();
            }

            if (MountData.totalFrames <= 0)
                MountData.totalFrames = 8;
            if (MountData.playerYOffsets == null || MountData.playerYOffsets.Length < MountData.totalFrames)
                MountData.playerYOffsets = new int[] { 14, 12, 10, 12, 14, 16, 14, 12 };
            if (MountData.textureWidth <= 0)
                MountData.textureWidth = 120;
            if (MountData.textureHeight <= 0)
                MountData.textureHeight = 640;
        }
    }

    public override void UpdateEffects(Player player)
    {
        // Efeito característico de partículas de tempestade carmesim acompanhando o movimento
        if (player.velocity.LengthSquared() > 4f && Main.rand.NextBool(2))
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

            // Partículas etéreas cianas do Dragão do Cultista
            if (Main.rand.NextBool(2))
            {
                Dust cyanDust = Dust.NewDustDirect(
                    player.position,
                    player.width,
                    player.height,
                    DustID.Vortex,
                    -player.velocity.X * 0.15f,
                    -player.velocity.Y * 0.15f,
                    120,
                    default,
                    Main.rand.NextFloat(0.8f, 1.3f)
                );
                cyanDust.noGravity = true;
            }
        }
    }
}
