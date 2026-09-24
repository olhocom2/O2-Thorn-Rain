using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.NPCs;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// Gerencia os efeitos ambientais globais enquanto o ThornStormBoss está vivo:
/// - Céu escurecido e tintado de carmesim
/// - Chuva máxima (tempestade)
/// - Vento forte
/// - Nuvens densas
/// - Partículas de neve/nevasca espalhadas pela tela (simulam blizzard)
///
/// Todos os efeitos são revertidos suavemente quando o boss morre ou foge.
/// </summary>
public class ThornBossIntroSystem : ModSystem
{
    // =========================================================
    // ESTADO
    // =========================================================

    /// <summary>Intensidade atual do efeito (0f = inativo, 1f = pleno).</summary>
    private static float _intensity;

    /// <summary>Velocidade de interpolação de entrada/saída do efeito.</summary>
    private const float FadeInSpeed  = 0.015f;
    private const float FadeOutSpeed = 0.008f;

    // Valores originais salvos para reverter
    private static float _savedWindSpeedTarget;
    private static int   _savedNumClouds;
    private static bool  _effectsApplied;

    // =========================================================
    // UPDATE
    // =========================================================

    public override void PostUpdateEverything()
    {
        // Verifica se o boss está ativo no mundo
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

        // Fade in / fade out suave
        if (bossAlive)
        {
            _intensity = MathHelper.Clamp(_intensity + FadeInSpeed, 0f, 1f);
        }
        else
        {
            _intensity = MathHelper.Clamp(_intensity - FadeOutSpeed, 0f, 1f);
        }

        // Só aplica efeitos no cliente
        if (Main.netMode == NetmodeID.Server)
            return;

        ApplyWorldEffects(bossAlive);
    }

    // =========================================================
    // EFEITOS AMBIENTAIS
    // =========================================================

    private static void ApplyWorldEffects(bool bossAlive)
    {
        if (_intensity <= 0f)
        {
            // Efeito completamente revertido
            if (_effectsApplied)
            {
                Main.windSpeedTarget = _savedWindSpeedTarget;
                Main.numClouds       = _savedNumClouds;
                _effectsApplied      = false;
            }
            return;
        }

        // Salva valores originais na primeira aplicação
        if (!_effectsApplied)
        {
            _savedWindSpeedTarget = Main.windSpeedTarget;
            _savedNumClouds       = Main.numClouds;
            _effectsApplied       = true;
        }

        // ── CHUVA / TEMPESTADE ────────────────────────────────────────
        Main.raining    = true;
        Main.maxRaining = MathHelper.Lerp(Main.maxRaining, 1f, _intensity * 0.12f);

        // ── VENTO FORTE ───────────────────────────────────────────────
        Main.windSpeedTarget = MathHelper.Lerp(
            _savedWindSpeedTarget,
            1.5f * (Main.rand.NextBool(4) ? -1f : 1f), // alternância de direção ocasional
            _intensity * 0.04f
        );

        // ── NUVENS DENSAS ─────────────────────────────────────────────
        Main.numClouds = (int)MathHelper.Lerp(_savedNumClouds, 200f, _intensity);

        // ── ESCURECIMENTO DO CÉU ─────────────────────────────────────
        // Aplica uma tinta escura carmesim sobre a cor do céu
        ApplySkyDarkening();

        // ── PARTÍCULAS DE NEVE (simulação de blizzard) ───────────────
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

    // =========================================================
    // LIMPEZA AO SAIR DO MUNDO
    // =========================================================

    public override void ClearWorld()
    {
        _intensity      = 0f;
        _effectsApplied = false;
    }
}
