using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.NPCs.Boss;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// Manages atmospheric effects while the Eye of the Thorn Storm boss is active:
/// Darkened sky tint, heavy storm rain, high wind, dense clouds, and blizzard particles.
/// Naturally fades out when the boss is defeated or despawns.
/// </summary>
public class ThornBossIntroSystem : ModSystem
{
    private static float _intensity;

    private const float FadeInSpeed  = 0.015f;
    private const float FadeOutSpeed = 0.008f;

    public override void PostUpdateEverything()
    {
        bool bossAlive = false;
        int bossType = ModContent.NPCType<ThornStormBoss>();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.type == bossType)
            {
                bossAlive = true;
                break;
            }
        }

        if (bossAlive)
        {
            _intensity = MathHelper.Clamp(_intensity + FadeInSpeed, 0f, 1f);
        }
        else
        {
            _intensity = MathHelper.Clamp(_intensity - FadeOutSpeed, 0f, 1f);
        }

        if (Main.netMode == NetmodeID.Server)
            return;

        ApplyWorldEffects();
    }

    private static void ApplyWorldEffects()
    {
        if (_intensity <= 0f)
            return;

        Main.raining = true;
        Main.maxRaining = MathHelper.Lerp(Main.maxRaining, 1f, _intensity * 0.12f);
        Main.windSpeedTarget = MathHelper.Lerp(Main.windSpeedTarget, 1.5f, _intensity * 0.04f);
        Main.numClouds = (int)MathHelper.Lerp(Main.numClouds, 200f, _intensity * 0.1f);

        ApplySkyDarkening();
        SpawnBlizzardDust();
    }

    private static void ApplySkyDarkening()
    {
        // Simula o escurecimento via partículas de névoa escura/carmesim
        // espalhadas pela borda da tela, criando uma vinheta visual
        if (!Main.rand.NextBool(3))
            return;

        float edgeX = Main.rand.NextBool()
            ? Main.rand.NextFloat(Main.screenWidth * 0.08f)
            : Main.rand.NextFloat(Main.screenWidth * 0.92f, Main.screenWidth);

        float edgeY = Main.rand.NextFloat(Main.screenHeight);

        Vector2 worldPos = Main.screenPosition + new Vector2(edgeX, edgeY);

        Dust darkDust = Dust.NewDustDirect(
            worldPos, 1, 1,
            DustID.Smoke,
            0f, -0.5f,
            (int)(200 * _intensity),
            new Color(20, 5, 5),
            Main.rand.NextFloat(1.5f, 3.0f) * _intensity
        );
        darkDust.noGravity = true;
        darkDust.noLight   = true;
        darkDust.fadeIn    = 0.5f;
    }

    private static void SpawnBlizzardDust()
    {
        if (_intensity < 0.3f)
            return;

        // Quantidade de partículas proporcional à intensidade
        int count = (int)(_intensity * 8f);

        for (int i = 0; i < count; i++)
        {
            // Posição aleatória na tela (convertida para mundo)
            float screenX = Main.rand.NextFloat(Main.screenWidth);
            float screenY = Main.rand.NextFloat(Main.screenHeight);

            Vector2 worldPos = Main.screenPosition + new Vector2(screenX, screenY);

            // Alternância: neve + folhas avermelhadas (vento carmesim do tornado)
            int dustType = Main.rand.Next(4) switch
            {
                0 => DustID.Snow,
                1 => DustID.Snow,
                2 => DustID.CrimsonTorch,
                _ => DustID.Cloud
            };

            float windX = Main.windSpeedCurrent * Main.rand.NextFloat(2f, 5f);

            Dust dust = Dust.NewDustDirect(
                worldPos,
                1, 1,
                dustType,
                windX,
                Main.rand.NextFloat(1.5f, 4.5f),
                150,
                default,
                Main.rand.NextFloat(0.6f, 1.4f)
            );
            dust.noGravity  = true;
            dust.noLight    = true;
            dust.fadeIn     = Main.rand.NextFloat(0.4f, 1.0f);
        }
    }

    public override void ClearWorld()
    {
        _intensity = 0f;
    }
}
