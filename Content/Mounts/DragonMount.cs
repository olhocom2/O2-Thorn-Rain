using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Buffs;

namespace O2ThornRain.Content.Mounts;

/// <summary>
/// Montaria voadora do Dragão Celestial do Cultista.
/// Concede voo infinito, agilidade extrema e imunidade exclusiva aos espinhos da Thorn Rain.
/// Utiliza folha de animação com 9 quadros de 200x100 px para um voo ondulatório contínuo e majestoso.
/// </summary>
public class DragonMount : ModMount
{
    private const int FrameWidth = 200;
    private const int FrameHeight = 100;
    private const int TotalFramesCount = 9;

    public override void SetStaticDefaults()
    {
        // Buff associado
        MountData.buff = ModContent.BuffType<DragonMountBuff>();

        // Parâmetros de movimentação e voo ágil infinito
        MountData.flightTimeMax = int.MaxValue;
        MountData.fatigueMax = int.MaxValue;
        MountData.fallDamage = 0f;
        MountData.heightBoost = 20;

        MountData.runSpeed = 13f;
        MountData.dashSpeed = 13f;
        MountData.acceleration = 0.28f;
        MountData.jumpHeight = 12;
        MountData.jumpSpeed = 8f;
        MountData.blockExtraJumps = true;
        MountData.usesHover = true;

        MountData.spawnDust = DustID.CrimsonTorch;

        // Configuração precisa dos 9 quadros de animação (200x100 px cada, total 200x900 px)
        MountData.totalFrames = TotalFramesCount;
        MountData.textureWidth = FrameWidth;
        MountData.textureHeight = FrameHeight * TotalFramesCount;

        // Ondulação suave do jogador acompanhando a sela/dorso do dragão em voo
        MountData.playerYOffsets = new int[] { 14, 10, 8, 18, 17, 18, 16, 14, 15 };
        MountData.xOffset = 0;
        MountData.yOffset = 2;
        MountData.playerXOffset = 0;
        MountData.playerHeadOffset = 18;
        MountData.bodyFrame = 3;

        // Animação completa de voo contínuo (todos os 9 quadros)
        MountData.flyingFrameCount = TotalFramesCount;
        MountData.flyingFrameStart = 0;
        MountData.flyingFrameDelay = 5;

        // Animação planando no ar
        MountData.inAirFrameCount = TotalFramesCount;
        MountData.inAirFrameStart = 0;
        MountData.inAirFrameDelay = 5;

        // Animação repousando / flutuando no solo
        MountData.standingFrameCount = TotalFramesCount;
        MountData.standingFrameStart = 0;
        MountData.standingFrameDelay = 7;

        // Animação em movimento no solo
        MountData.runningFrameCount = TotalFramesCount;
        MountData.runningFrameStart = 0;
        MountData.runningFrameDelay = 5;

        // Animação na água
        MountData.swimFrameCount = TotalFramesCount;
        MountData.swimFrameStart = 0;
        MountData.swimFrameDelay = 5;

        MountData.idleFrameCount = 0;
        MountData.idleFrameStart = 0;
        MountData.idleFrameDelay = 0;
        MountData.idleFrameLoop = false;

        MountData.dashingFrameCount = 0;
        MountData.dashingFrameStart = 0;
        MountData.dashingFrameDelay = 0;

        // Carregamento da textura do Dragão
        if (!Main.dedServ)
        {
            MountData.backTexture = ModContent.Request<Texture2D>("O2ThornRain/Content/Mounts/DragonMount_Back");

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
        MountData.totalFrames = TotalFramesCount;
        MountData.textureWidth = FrameWidth;
        MountData.textureHeight = FrameHeight * TotalFramesCount;

        if (MountData.playerYOffsets == null || MountData.playerYOffsets.Length < TotalFramesCount)
        {
            MountData.playerYOffsets = new int[] { 14, 10, 8, 18, 17, 18, 16, 14, 15 };
        }

        if (!Main.dedServ)
        {
            if (MountData.backTexture == null || MountData.backTexture == Asset<Texture2D>.Empty)
            {
                MountData.backTexture = ModContent.Request<Texture2D>("O2ThornRain/Content/Mounts/DragonMount_Back");
            }
        }
    }

    public override bool Draw(
        List<DrawData> playerDrawData,
        int drawType,
        Player drawPlayer,
        ref Texture2D texture,
        ref Texture2D glowTexture,
        ref Vector2 drawPosition,
        ref Rectangle frame,
        ref Color drawColor,
        ref Color glowColor,
        ref float rotation,
        ref SpriteEffects spriteEffects,
        ref Vector2 drawOrigin,
        ref float drawScale,
        float shadow)
    {
        // Garante amostragem exata de 200x100 por quadro e centro de rotação simétrico
        int frameIndex = drawPlayer.mount._frame;
        if (frameIndex < 0 || frameIndex >= TotalFramesCount)
            frameIndex = 0;

        frame = new Rectangle(0, frameIndex * FrameHeight, FrameWidth, FrameHeight);
        drawOrigin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

        if (texture == null && MountData.backTexture != null && MountData.backTexture.IsLoaded)
        {
            texture = MountData.backTexture.Value;
        }

        return true;
    }

    public override void UpdateEffects(Player player)
    {
        // Efeito de rastro etéreo do Dragão com fogo carmesim e partículas Vortex celestiais
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
