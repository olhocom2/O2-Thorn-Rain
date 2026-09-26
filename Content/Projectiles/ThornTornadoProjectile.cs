using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;

namespace O2ThornRain.Content.Projectiles;

/// <summary>
/// Projétil do Thorn Tornado (Tornado de Espinhos).
///
/// Utiliza diretamente o sprite vanilla do tornado gigante
/// do Duke Fishron (ProjectileID.Cthulunado).
///
/// O sprite é desenhado manualmente em múltiplas camadas
/// verticais para criar um tornado alto e volumoso.
///
/// O tornado causa dano por contato físico direto ao atingir o jogador
/// e dispara rajadas de SpikesProjectile enquanto atravessa o cenário.
/// </summary>
public class ThornTornadoProjectile : ModProjectile
{
    // =========================================================
    // TEXTURA VANILLA
    // =========================================================

    public override string Texture =>
        $"Terraria/Images/Projectile_{ProjectileID.Cthulunado}";


    // =========================================================
    // ANIMAÇÃO
    // =========================================================

    // O Cthulunado vanilla possui 6 frames.
    private const int AnimationFrameCount = 6;

    private const int AnimationFrameSpeed = 4;


    // =========================================================
    // TEMPO DO TORNADO
    // =========================================================

    // 720 ticks = 12 segundos.
    public const int TotalLifetimeTicks = 720;

    // 120 ticks = 2 segundos.
    public const int FormationTicks = 120;

    // Últimos 120 ticks = 2 segundos de dissipação.
    public const int DissipationTicks = 120;


    // =========================================================
    // MOVIMENTO
    // =========================================================

    public const float HorizontalSpeed = 3.5f;

    private const float VerticalWaveSpeed = 0.035f;

    private const float VerticalWaveAmount = 0.45f;


    // =========================================================
    // ESCALA
    // =========================================================

    public const float MaxScale = 1.0f;


    // =========================================================
    // RAJADAS DE ESPINHOS
    // =========================================================

    public const int SpikeBurstInterval = 12;

    private const int MinimumSpikesPerBurst = 2;

    private const int MaximumSpikesPerBurst = 4;

    private const float MinimumSpikeSpeed = 10f;

    private const float MaximumSpikeSpeed = 16f;


    // =========================================================
    // DANO E IMPACTO POR CONTATO
    // =========================================================

    public const float TornadoKnockback = 4f;


    // =========================================================
    // CONTROLE INTERNO
    // =========================================================

    private int _lifetimeTimer;

    private int _burstTimer;


    // =========================================================
    // CONFIGURAÇÃO DOS FRAMES
    // =========================================================

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = AnimationFrameCount;
    }


    // =========================================================
    // CONFIGURAÇÃO DO PROJÉTIL
    // =========================================================

    public override void SetDefaults()
    {
        // Área física para acompanhar o volume visual e colisão de contato.
        Projectile.width = 180;
        Projectile.height = 420;

        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;

        Projectile.timeLeft = TotalLifetimeTicks;

        Projectile.penetrate = -1;

        // O próprio corpo do tornado causa dano por contato ao atingir o jogador.
        Projectile.hostile = true;
        Projectile.friendly = false;

        Projectile.damage = SpikeRainSystem.GetSpikeDamage();
        Projectile.knockBack = TornadoKnockback;

        Projectile.scale = 0.1f;
        Projectile.alpha = 255;

        Projectile.aiStyle = 0;
    }


    // =========================================================
    // COLISÃO COM O JOGADOR
    // =========================================================

    public override bool CanHitPlayer(Player target)
    {
        // O tornado só atinge o jogador quando estiver plenamente formado.
        if (_lifetimeTimer < FormationTicks)
            return false;

        // O tornado atinge o jogador mesmo se estiver atravessando paredes ou dentro de construções.
        return true;
    }


    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // Avalia colisão direta pela sobreposição dos retângulos,
        // garantindo que paredes, tetos e blocos sólidos não bloqueiem o dano.
        return projHitbox.Intersects(targetHitbox);
    }


    // =========================================================
    // IA PRINCIPAL
    // =========================================================

    public override void AI()
    {
        _lifetimeTimer++;

        // =====================================================
        // 1. ANIMAÇÃO DO SPRITE VANILLA
        // =====================================================

        Projectile.frameCounter++;

        if (Projectile.frameCounter >= AnimationFrameSpeed)
        {
            Projectile.frameCounter = 0;

            Projectile.frame++;

            if (Projectile.frame >= AnimationFrameCount)
                Projectile.frame = 0;
        }


        // =====================================================
        // 2. ESCALA E OPACIDADE
        // =====================================================

        UpdateVisualTransition();


        // =====================================================
        // 3. MOVIMENTO HORIZONTAL
        // =====================================================

        float direction =
            Projectile.ai[0] != 0f
                ? Math.Sign(Projectile.ai[0])
                : 1f;

        Projectile.velocity.X =
            direction * HorizontalSpeed;


        // Pequena oscilação vertical.
        Projectile.velocity.Y =
            (float)Math.Sin(
                _lifetimeTimer * VerticalWaveSpeed
            ) * VerticalWaveAmount;


        // O corpo não gira inteiro.
        // A rotação individual das camadas é feita no PreDraw.
        Projectile.rotation = 0f;


        // =====================================================
        // 4. PARTÍCULAS
        // =====================================================

        SpawnAmbientDust();


        // =====================================================
        // 5. RAJADAS
        // =====================================================

        // O servidor controla os projéteis.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;


        // Espinhos somente durante a fase plenamente ativa.
        if (_lifetimeTimer > FormationTicks &&
            Projectile.timeLeft > DissipationTicks)
        {
            _burstTimer++;

            if (_burstTimer >= SpikeBurstInterval)
            {
                _burstTimer = 0;

                SpawnSpikeBurst();
            }
        }
    }


    // =========================================================
    // TRANSIÇÃO VISUAL
    // =========================================================

    private void UpdateVisualTransition()
    {
        // -----------------------------------------------------
        // FORMAÇÃO
        // -----------------------------------------------------

        if (_lifetimeTimer <= FormationTicks)
        {
            float progress =
                _lifetimeTimer /
                (float)FormationTicks;

            progress =
                MathHelper.Clamp(
                    progress,
                    0f,
                    1f
                );

            // Crescimento suavizado.
            float smoothProgress =
                progress * progress *
                (3f - 2f * progress);

            Projectile.scale =
                MathHelper.Lerp(
                    0.10f,
                    MaxScale,
                    smoothProgress
                );

            Projectile.alpha =
                (int)MathHelper.Lerp(
                    255f,
                    45f,
                    smoothProgress
                );

            return;
        }


        // -----------------------------------------------------
        // DISSIPAÇÃO
        // -----------------------------------------------------

        if (Projectile.timeLeft <= DissipationTicks)
        {
            float progress =
                Projectile.timeLeft /
                (float)DissipationTicks;

            progress =
                MathHelper.Clamp(
                    progress,
                    0f,
                    1f
                );

            float smoothProgress =
                progress * progress *
                (3f - 2f * progress);

            Projectile.scale =
                MathHelper.Lerp(
                    0.05f,
                    MaxScale,
                    smoothProgress
                );

            Projectile.alpha =
                (int)MathHelper.Lerp(
                    255f,
                    45f,
                    smoothProgress
                );

            return;
        }


        // -----------------------------------------------------
        // FASE ATIVA
        // -----------------------------------------------------

        Projectile.scale = MaxScale;
        Projectile.alpha = 45;
    }


    // =========================================================
    // POEIRA AMBIENTAL
    // =========================================================

    private void SpawnAmbientDust()
    {
        // Mantém o efeito relativamente leve.
        if (!Main.rand.NextBool(5))
            return;


        // O tornado possui aproximadamente 360px de
        // altura visual útil.
        float vertical =
            Main.rand.NextFloat(
                -175f,
                175f
            );


        // A largura varia de acordo com a altura.
        float normalized =
            (vertical + 175f) / 350f;

        float widthFactor =
            0.35f +
            (float)Math.Sin(
                normalized * MathHelper.Pi
            ) * 0.65f;


        float horizontal =
            Main.rand.NextFloat(
                -110f,
                110f
            ) * widthFactor;


        Vector2 position =
            Projectile.Center +
            new Vector2(
                horizontal,
                vertical
            ) * Projectile.scale;


        Dust dust =
            Dust.NewDustPerfect(
                position,
                DustID.Water,
                new Vector2(
                    -1.2f,
                    Main.rand.NextFloat(
                        -1.5f,
                        0.5f
                    )
                ),
                100,
                default,
                Main.rand.NextFloat(
                    0.6f,
                    1.0f
                )
            );

        dust.noGravity = true;
    }


    // =========================================================
    // DESENHO MULTICAMADAS
    // =========================================================

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture =
            TextureAssets.Projectile[Projectile.type].Value;

        Rectangle frame = texture.Frame(
            1,
            Main.projFrames[Projectile.type],
            0,
            Projectile.frame
        );

        Vector2 origin = frame.Size() / 2f;

        Vector2 drawPosition =
            Projectile.Center - Main.screenPosition;

        /*
         * Muitas camadas, com pouca distância entre elas.
         *
         * O objetivo é transformar os sprites individuais
         * em uma única massa visual contínua.
         */
        const int layers = 15;

        /*
         * Distância vertical TOTAL do tornado.
         */
        const float totalHeight = 360f;

        for (int i = 0; i < layers; i++)
        {
            float normalized =
                i / (float)(layers - 1);

            /*
             * -1 = topo
             *  0 = centro
             * +1 = base
             */
            float vertical =
                normalized * 2f - 1f;

            /*
             * FORMATO DO TORNADO
             *
             * 1.0 no topo
             * vai diminuindo de maneira LINEAR
             * até chegar à ponta.
             *
             * Isso cria lados diagonais,
             * em vez de uma forma circular.
             */
            float widthFactor =
                1f - Math.Abs(vertical);

            /*
             * Pequena largura mínima para
             * a ponta não desaparecer abruptamente.
             */
            widthFactor =
                MathHelper.Lerp(
                    0.10f,
                    1f,
                    widthFactor
                );

            /*
             * Pequena movimentação horizontal.
             * Muito menor que antes para não separar
             * visualmente as camadas.
             */
            float wave =
                MathF.Sin(
                    _lifetimeTimer * 0.035f +
                    i * 0.35f
                ) * 5f;

            Vector2 layerPosition =
                drawPosition +
                new Vector2(
                    wave,
                    vertical * (totalHeight * 0.5f)
                );

            /*
             * O sprite fica MUITO mais largo
             * horizontalmente que verticalmente.
             */
            Vector2 scale = new Vector2(
                Projectile.scale *
                1.65f *
                widthFactor,

                Projectile.scale *
                0.48f
            );

            /*
             * Rotação mínima.
             * Não queremos que pareça um monte
             * de círculos girando separadamente.
             */
            float rotation =
                MathF.Sin(
                    _lifetimeTimer * 0.02f +
                    i * 0.25f
                ) * 0.015f;

            /*
             * Mais opaco no centro,
             * levemente transparente nas pontas.
             */
            float opacity =
                MathHelper.Lerp(
                    0.38f,
                    0.78f,
                    1f - Math.Abs(vertical)
                );

            Color color =
                Projectile.GetAlpha(lightColor) *
                opacity;

            Main.EntitySpriteDraw(
                texture,
                layerPosition,
                frame,
                color,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );
        }

        return false;
    }

    // =========================================================
    // RAJADA DE ESPINHOS
    // =========================================================

    private void SpawnSpikeBurst()
    {
        int spikeCount =
            Main.rand.Next(
                MinimumSpikesPerBurst,
                MaximumSpikesPerBurst + 1
            );


        // -----------------------------------------------------
        // LIMITE GLOBAL
        // -----------------------------------------------------

        int availableSlots =
            SpikeRainSystem.MaxGlobalSpikes -
            SpikesProjectile.ActiveCount;


        if (availableSlots <= 0)
            return;


        if (spikeCount > availableSlots)
            spikeCount = availableSlots;


        int spikeType =
            ModContent.ProjectileType<
                SpikesProjectile
            >();


        int damage =
            SpikeRainSystem.GetSpikeDamage();


        // -----------------------------------------------------
        // CRIAÇÃO DOS ESPINHOS
        // -----------------------------------------------------

        for (int i = 0; i < spikeCount; i++)
        {
            // -------------------------------------------------
            // POSIÇÃO DENTRO DA COLUNA
            // -------------------------------------------------

            float vertical =
                Main.rand.NextFloat(
                    -165f,
                    165f
                );


            float normalized =
                (vertical + 165f) / 330f;


            float widthFactor =
                0.35f +
                (float)Math.Sin(
                    normalized * MathHelper.Pi
                ) * 0.65f;


            float horizontal =
                Main.rand.NextFloat(
                    -75f,
                    75f
                ) * widthFactor;


            Vector2 spawnPos =
                Projectile.Center +
                new Vector2(
                    horizontal,
                    vertical
                ) * Projectile.scale;


            // -------------------------------------------------
            // DIREÇÃO
            // -------------------------------------------------

            float roll =
                Main.rand.NextFloat();


            float angle;


            // 60% - para cima / diagonais superiores.
            if (roll < 0.60f)
            {
                angle =
                    MathHelper.ToRadians(
                        Main.rand.NextFloat(
                            -150f,
                            -30f
                        )
                    );
            }


            // 25% - laterais.
            else if (roll < 0.85f)
            {
                if (Main.rand.NextBool())
                {
                    angle =
                        MathHelper.ToRadians(
                            Main.rand.NextFloat(
                                -30f,
                                30f
                            )
                        );
                }
                else
                {
                    angle =
                        MathHelper.ToRadians(
                            Main.rand.NextFloat(
                                150f,
                                210f
                            )
                        );
                }
            }


            // 15% - para baixo / diagonais inferiores.
            else
            {
                angle =
                    MathHelper.ToRadians(
                        Main.rand.NextFloat(
                            30f,
                            150f
                        )
                    );
            }


            // -------------------------------------------------
            // VELOCIDADE
            // -------------------------------------------------

            float speed =
                Main.rand.NextFloat(
                    MinimumSpikeSpeed,
                    MaximumSpikeSpeed
                );


            Vector2 velocity =
                angle.ToRotationVector2() *
                speed;


            // Pequena influência da direção do tornado.
            velocity.X +=
                Projectile.velocity.X * 0.25f;


            // -------------------------------------------------
            // CRIAÇÃO
            // -------------------------------------------------

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                spikeType,
                damage,
                3.5f,
                Main.myPlayer
            );
        }
    }


    // =========================================================
    // FINALIZAÇÃO
    // =========================================================

    public override void OnKill(int timeLeft)
    {
        // -----------------------------------------------------
        // SOM
        // -----------------------------------------------------

        SoundEngine.PlaySound(
            SoundID.Item120 with
            {
                Volume = 0.45f,
                PitchVariance = 0.2f
            },
            Projectile.Center
        );


        // -----------------------------------------------------
        // POEIRA FINAL
        // -----------------------------------------------------

        for (int i = 0; i < 12; i++)
        {
            Vector2 velocity =
                Main.rand.NextVector2Circular(
                    3f,
                    3f
                );


            Dust.NewDustPerfect(
                Projectile.Center,
                DustID.Water,
                velocity,
                100,
                default,
                Main.rand.NextFloat(
                    0.8f,
                    1.3f
                )
            );
        }
    }
}