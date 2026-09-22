using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
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
        MountData.usesHover = true;

        MountData.spawnDust = DustID.CrimsonTorch;

        // Configuração de frames e animação (evita DivideByZeroException no Mount.Draw)
        MountData.totalFrames = 23;
        MountData.playerYOffsets = Enumerable.Repeat(12, MountData.totalFrames).ToArray();
        MountData.xOffset = 2;
        MountData.yOffset = 16;
        MountData.playerXOffset = 0;
        MountData.playerHeadOffset = 16;
        MountData.bodyFrame = 3;

        MountData.standingFrameCount = 1;
        MountData.standingFrameDelay = 12;
        MountData.standingFrameStart = 8;

        MountData.runningFrameCount = 7;
        MountData.runningFrameDelay = 14;
        MountData.runningFrameStart = 8;

        MountData.flyingFrameCount = 8;
        MountData.flyingFrameDelay = 16;
        MountData.flyingFrameStart = 0;

        MountData.inAirFrameCount = 8;
        MountData.inAirFrameDelay = 6;
        MountData.inAirFrameStart = 0;

        MountData.idleFrameCount = 0;
        MountData.idleFrameDelay = 0;
        MountData.idleFrameStart = 0;
        MountData.idleFrameLoop = false;

        MountData.swimFrameCount = 8;
        MountData.swimFrameDelay = 4;
        MountData.swimFrameStart = 15;

        MountData.dashingFrameCount = 0;
        MountData.dashingFrameDelay = 0;
        MountData.dashingFrameStart = 0;

        // Dimensões base seguras caso os assets ainda não tenham sido medidos
        MountData.textureWidth = 80;
        MountData.textureHeight = 80 * MountData.totalFrames;

        // Herda texturas e propriedades do CuteFishron como fallback seguro
        // até que um sprite dedicado de dragão seja adicionado pelo criador
        if (Mount.mounts != null && Mount.mounts.Length > MountID.CuteFishron && Mount.mounts[MountID.CuteFishron] != null)
        {
            Mount.MountData baseMount = Mount.mounts[MountID.CuteFishron];
            if (baseMount.totalFrames > 0)
                MountData.totalFrames = baseMount.totalFrames;

            if (baseMount.playerYOffsets != null && baseMount.playerYOffsets.Length >= MountData.totalFrames)
                MountData.playerYOffsets = (int[])baseMount.playerYOffsets.Clone();

            MountData.backTexture = baseMount.backTexture;
            MountData.backTextureGlow = baseMount.backTextureGlow;
            MountData.backTextureExtra = baseMount.backTextureExtra;
            MountData.frontTexture = baseMount.frontTexture;
            MountData.frontTextureGlow = baseMount.frontTextureGlow;
            MountData.frontTextureExtra = baseMount.frontTextureExtra;

            if (baseMount.textureWidth > 0)
                MountData.textureWidth = baseMount.textureWidth;
            if (baseMount.textureHeight > 0)
                MountData.textureHeight = baseMount.textureHeight;

            MountData.xOffset = baseMount.xOffset;
            MountData.yOffset = baseMount.yOffset;
            MountData.playerXOffset = baseMount.playerXOffset;
            MountData.playerHeadOffset = baseMount.playerHeadOffset;
            MountData.bodyFrame = baseMount.bodyFrame;

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

        if (!Main.dedServ)
        {
            if (MountData.backTexture == null || MountData.backTexture == Asset<Texture2D>.Empty)
            {
                if (TextureAssets.CuteFishronMount != null && TextureAssets.CuteFishronMount.Length > 0)
                {
                    MountData.backTexture = TextureAssets.CuteFishronMount[0];
                    if (TextureAssets.CuteFishronMount.Length > 1)
                        MountData.backTextureGlow = TextureAssets.CuteFishronMount[1];
                }
            }

            if (MountData.backTexture != null && MountData.backTexture.IsLoaded)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }
    }

    public override void SetMount(Player player, ref bool skipDust)
    {
        // Garante que no momento da ativação em jogo as texturas e dimensões estejam válidas
        if (!Main.dedServ)
        {
            if (MountData.backTexture == null || MountData.backTexture == Asset<Texture2D>.Empty)
            {
                if (TextureAssets.CuteFishronMount != null && TextureAssets.CuteFishronMount.Length > 0)
                {
                    MountData.backTexture = TextureAssets.CuteFishronMount[0];
                    if (TextureAssets.CuteFishronMount.Length > 1)
                        MountData.backTextureGlow = TextureAssets.CuteFishronMount[1];
                }
            }

            if (MountData.backTexture != null && MountData.backTexture.IsLoaded)
            {
                if (MountData.textureWidth <= 0)
                    MountData.textureWidth = MountData.backTexture.Width();
                if (MountData.textureHeight <= 0)
                    MountData.textureHeight = MountData.backTexture.Height();
            }

            if (MountData.totalFrames <= 0)
                MountData.totalFrames = 23;
            if (MountData.textureWidth <= 0)
                MountData.textureWidth = 80;
            if (MountData.textureHeight <= 0)
                MountData.textureHeight = 80 * MountData.totalFrames;
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

