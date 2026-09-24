using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;
using O2ThornRain.Content.BossBars;
using O2ThornRain.Content.Items;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Content.NPCs;

/// <summary>
/// Boss final do evento "A Tempestade dos Quatro".
/// Um tornado colossal carmesim/vermelho que persegue e pressiona o jogador,
/// possui 3 fases de combate de acordo com sua vida, desfere chuvas de espinhos,
/// invoca mini tornados destruíveis e dropa a Bolsa do Tesouro e o Troféu ao ser derrotado.
/// </summary>
[AutoloadBossHead]
public class ThornStormBoss : ModNPC
{
    private const int FrameCount = 8;
    private int _frameIndex;

    // Fases de combate
    public enum BossPhase
    {
        Phase1_Gathering,   // 100% - 70% HP: Perseguição suave, espinhos controlados, poucos mini tornados
        Phase2_Gale,        // 70% - 40% HP: Velocidade aumentada, pequenos dashes, mais mini tornados
        Phase3_Cataclysm    // < 40% HP: Dashes frenéticos, tempestade de espinhos 360°, enxame de mini tornados
    }

    public BossPhase CurrentPhase
    {
        get
        {
            float lifeRatio = (float)NPC.life / NPC.lifeMax;
            if (lifeRatio > 0.70f)
                return BossPhase.Phase1_Gathering;
            if (lifeRatio > 0.40f)
                return BossPhase.Phase2_Gale;
            return BossPhase.Phase3_Cataclysm;
        }
    }

    /// <summary>
    /// Multiplicador dinâmico de velocidade e agressividade conforme a dificuldade do mundo.
    /// Normal: 1.0x | Expert: 1.12x | Master: 1.24x | Legendary/FTW: 1.38x.
    /// </summary>
    private static float DifficultySpeedMultiplier =>
        Main.getGoodWorld ? 1.38f :
        Main.masterMode ? 1.24f :
        Main.expertMode ? 1.12f : 1.0f;

    /// <summary>
    /// Redução dinâmica dos tempos de recarga e intervalos de ataque/dashes conforme a dificuldade.
    /// Normal: 1.0x | Expert: 0.88x | Master: 0.78x | Legendary/FTW: 0.68x.
    /// </summary>
    private static float DifficultyRateMultiplier =>
        Main.getGoodWorld ? 0.68f :
        Main.masterMode ? 0.78f :
        Main.expertMode ? 0.88f : 1.0f;

    // Variáveis de IA armazenadas nos slots de AI do NPC para sincronização multiplayer
    private ref float AttackTimer => ref NPC.ai[0];
    private ref float ChargeTimer => ref NPC.ai[1];
    private ref float ChargeState => ref NPC.ai[2]; // 0: normal, 1: preparando investida, 2: investindo
    private ref float InternalCounter => ref NPC.ai[3];

    private int _minionTornadoTimer;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = FrameCount;

        NPCID.Sets.BossBestiaryPriority.Add(Type);
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.TrailCacheLength[Type] = 8;
        NPCID.Sets.TrailingMode[Type] = 1;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
    }

    public override void SetDefaults()
    {
        // Hitbox aumentado em 50% para refletir a escala colossal do boss
        NPC.width = 360;
        NPC.height = 360;

        NPC.damage = 75;
        NPC.defense = 34;
        NPC.lifeMax = 48000;

        NPC.HitSound = SoundID.NPCHit3;
        NPC.DeathSound = SoundID.NPCDeath10;

        NPC.knockBackResist = 0f;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;

        NPC.boss = true;
        NPC.friendly = false;
        // Moedas são concedidas apenas via Boss Bag (Expert/Master).
        // No modo Clássico, somente Luminita e Troféu caem diretamente.
        NPC.value = 0;

        // Barra de vida oficial do boss
        NPC.BossBar = ModContent.GetInstance<ThornStormBossBar>();

        Music = MusicID.Boss2;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        // Escala básica de vida considerando multiplayer
        NPC.lifeMax = (int)(NPC.lifeMax * 0.70f * balance * bossAdjustment);

        if (Main.masterMode)
        {
            // Modo Master: vida substancialmente ampliada, dano e defesa reforçados
            NPC.lifeMax = (int)(NPC.lifeMax * 1.35f);
            NPC.damage = 145;
            NPC.defense = 48;
        }
        else if (Main.expertMode)
        {
            // Modo Expert: dano e defesa moderadamente ampliados
            NPC.damage = 110;
            NPC.defense = 40;
        }

        // Suporte especial para sementes de desafio supremo (For the Worthy / Legendary Mode)
        if (Main.getGoodWorld)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 1.25f);
            NPC.damage = (int)(NPC.damage * 1.25f);
            NPC.defense = (int)(NPC.defense * 1.15f);
        }
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        // Aplica debuffs dinâmicos temáticos de tempestade e espinhos conforme a dificuldade
        if (Main.masterMode)
        {
            target.AddBuff(BuffID.Bleeding, 480);   // 8s de sangramento profundo
            target.AddBuff(BuffID.Slow, 120);       // 2s de lentidão por ventos fortes
            target.AddBuff(BuffID.WindPushed, 180); // 3s de pressão atmosférica
        }
        else if (Main.expertMode)
        {
            target.AddBuff(BuffID.Bleeding, 360);   // 6s de sangramento
            target.AddBuff(BuffID.WindPushed, 120); // 2s de rajada de vento
        }
    }

    public override bool CheckActive()
    {
        if (NPC.target >= 0 && NPC.target < Main.maxPlayers)
        {
            Player target = Main.player[NPC.target];
            return !target.active || target.dead;
        }

        return true;
    }

    public override void AI()
    {
        // -------------------------------------------------------------
        // 0. SOM CLÁSSICO DE SPAWN DO BOSS (ROAR) NO PRIMEIRO FRAME
        // -------------------------------------------------------------
        if (NPC.localAI[3] == 0f)
        {
            NPC.localAI[3] = 1f;
            SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
        }

        // -------------------------------------------------------------
        // 1. GERENCIAMENTO DE ALVO E DESPAWN QUANDO TODOS MORREM
        // -------------------------------------------------------------
        if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
        {
            NPC.TargetClosest(true);
        }

        Player targetPlayer = Main.player[NPC.target];

        if (!targetPlayer.active || targetPlayer.dead)
        {
            // Todos os jogadores morreram: sobe rapidamente para os céus e desaparece
            NPC.velocity.Y -= 0.6f;
            NPC.velocity.X *= 0.94f;
            NPC.EncourageDespawn(10);

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.timeLeft <= 1)
            {
                ThornStormEventSystem.OnBossEscaped();
            }
            return;
        }

        // -------------------------------------------------------------
        // 2. PARTÍCULAS TEMPESTUOSAS
        // -------------------------------------------------------------
        SpawnStormDust();

        // -------------------------------------------------------------
        // 3. FASE ATUAL
        // -------------------------------------------------------------
        BossPhase phase = CurrentPhase;

        // -------------------------------------------------------------
        // 4. MOVIMENTAÇÃO DE PERSEGUIÇÃO E DASHES POR FASE
        // -------------------------------------------------------------
        UpdateMovement(targetPlayer, phase);

        // Apenas o servidor gera projéteis e orquestra ataques e summons
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // -------------------------------------------------------------
        // 5. INVOÇÃO DE MINI TORNADOS E ATAQUES DE ESPINHOS
        // -------------------------------------------------------------
        UpdateMinionTornadoes(targetPlayer, phase);
        UpdateAttacks(targetPlayer, phase);
    }

    // =========================================================================
    // MOVIMENTAÇÃO, PERSEGUIÇÃO E INVESTIDAS (DASHES)
    // =========================================================================

    private void UpdateMovement(Player target, BossPhase phase)
    {
        // 1. Inclinação orgânica suave acompanhando o movimento lateral
        NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.velocity.X * 0.025f, 0.12f);

        // 2. Parâmetros dinâmicos de velocidade e agressividade por fase e dificuldade do mundo
        float diffSpeed = DifficultySpeedMultiplier;
        float diffRate = DifficultyRateMultiplier;

        float maxSpeedX;
        float accelX;
        float hoverOffsetY;

        switch (phase)
        {
            case BossPhase.Phase1_Gathering:
                // Fase 1: Perseguição suave e controlada
                maxSpeedX = 6.0f * diffSpeed;
                accelX = 0.10f * diffSpeed;
                hoverOffsetY = -380f; // aumentado para compensar o novo tamanho
                ChargeState = 0;
                ChargeTimer = 0;
                break;

            case BossPhase.Phase2_Gale:
                // Fase 2: Perseguição ágil com pequenos dashes ocasionais
                maxSpeedX = 11.0f * diffSpeed;
                accelX = 0.22f * diffSpeed;
                hoverOffsetY = -300f; // aumentado para compensar o novo tamanho
                break;

            case BossPhase.Phase3_Cataclysm:
            default:
                // Fase 3: Perseguição frenética e extrema
                maxSpeedX = 16.0f * diffSpeed;
                accelX = 0.40f * diffSpeed;
                hoverOffsetY = -220f; // aumentado para compensar o novo tamanho
                break;
        }

        // 3. Sistema de Dashes (Investidas) para Fases 2 e 3
        if (phase != BossPhase.Phase1_Gathering)
        {
            ChargeTimer++;

            int baseInterval = (phase == BossPhase.Phase2_Gale) ? 300 : 160;
            int chargeInterval = Math.Max(70, (int)(baseInterval * diffRate));
            int telegraphTicks = (int)(((phase == BossPhase.Phase2_Gale) ? 30 : 20) * (Main.masterMode ? 0.85f : 1.0f));

            if (ChargeState == 0 && ChargeTimer >= chargeInterval)
            {
                // Inicia aviso/telegraph da investida
                ChargeState = 1;
                ChargeTimer = 0;
                NPC.velocity *= 0.4f;
                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.3f, Volume = 0.9f }, NPC.Center);
            }
            else if (ChargeState == 1)
            {
                // Preparação (aviso com partículas concentradas)
                for (int d = 0; d < 2; d++)
                {
                    Vector2 dustVel = (target.Center - NPC.Center);
                    if (dustVel != Vector2.Zero) dustVel.Normalize();
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrimsonTorch, dustVel.X * 4f, dustVel.Y * 4f, 100, default, 1.4f);
                }

                if (ChargeTimer >= telegraphTicks)
                {
                    ChargeState = 2;
                    ChargeTimer = 0;

                    Vector2 chargeDir = (target.Center - NPC.Center);
                    if (chargeDir != Vector2.Zero)
                        chargeDir.Normalize();
                    else
                        chargeDir = new Vector2(0f, 1f);

                    float chargeSpeed = ((phase == BossPhase.Phase2_Gale) ? 15f : 22f) * diffSpeed;
                    NPC.velocity = chargeDir * chargeSpeed;

                    SoundEngine.PlaySound(SoundID.ForceRoar with { Pitch = phase == BossPhase.Phase3_Cataclysm ? -0.35f : -0.1f }, NPC.Center);
                }
                return;
            }
            else if (ChargeState == 2)
            {
                // Investindo por 26 ticks
                if (ChargeTimer >= 26)
                {
                    ChargeState = 0;
                    ChargeTimer = 0;
                    NPC.velocity *= 0.35f;
                }
                return;
            }
        }

        // 4. Movimento padrão: pairar e pressionar o jogador
        Vector2 targetHoverPos = target.Center + new Vector2(0f, hoverOffsetY);
        Vector2 toTarget = targetHoverPos - NPC.Center;

        if (toTarget.X > 35f)
        {
            NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + accelX, -maxSpeedX, maxSpeedX);
        }
        else if (toTarget.X < -35f)
        {
            NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X - accelX, -maxSpeedX, maxSpeedX);
        }
        else
        {
            NPC.velocity.X *= 0.96f;
        }

        float targetYVel = MathHelper.Clamp(toTarget.Y * 0.04f, -8f * diffSpeed, 8f * diffSpeed);
        NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, targetYVel, 0.09f * diffSpeed);
    }

    // =========================================================================
    // INVOÇÃO DE MINI TORNADOS DESTRUÍVEIS (SUBSTITUEM OS DRAGÕES)
    // =========================================================================

    private void UpdateMinionTornadoes(Player target, BossPhase phase)
    {
        _minionTornadoTimer++;

        int baseInterval;
        int maxMinions;
        int phaseNumber;
        float diffRate = DifficultyRateMultiplier;

        switch (phase)
        {
            case BossPhase.Phase1_Gathering:
                baseInterval = 480; // 8s no clássico
                maxMinions = Main.masterMode ? 4 : (Main.expertMode ? 3 : 2);
                phaseNumber = 1;
                break;

            case BossPhase.Phase2_Gale:
                baseInterval = 270; // 4.5s no clássico
                maxMinions = Main.masterMode ? 8 : (Main.expertMode ? 6 : 4);
                phaseNumber = 2;
                break;

            case BossPhase.Phase3_Cataclysm:
            default:
                baseInterval = 180; // 3s no clássico
                maxMinions = Main.masterMode ? 12 : (Main.expertMode ? 9 : 7);
                phaseNumber = 3;
                break;
        }

        int interval = Math.Max(70, (int)(baseInterval * diffRate));

        if (_minionTornadoTimer >= interval)
        {
            _minionTornadoTimer = 0;

            int minionType = ModContent.NPCType<ThornMinionTornado>();
            int currentMinions = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.type == minionType)
                    currentMinions++;
            }

            if (currentMinions < maxMinions)
            {
                int spawnCount = (phase == BossPhase.Phase3_Cataclysm && currentMinions <= maxMinions - 2)
                    ? (Main.masterMode ? 3 : 2)
                    : 1;

                for (int s = 0; s < spawnCount; s++)
                {
                    Vector2 spawnOffset = Main.rand.NextVector2Circular(NPC.width * 0.4f, NPC.height * 0.4f);
                    Vector2 spawnPos = NPC.Center + spawnOffset;

                    int minionIdx = NPC.NewNPC(
                        NPC.GetSource_FromAI(),
                        (int)spawnPos.X,
                        (int)spawnPos.Y,
                        minionType,
                        ai0: phaseNumber,
                        ai1: NPC.whoAmI
                    );

                    if (minionIdx >= 0 && minionIdx < Main.maxNPCs)
                    {
                        Main.npc[minionIdx].netUpdate = true;
                    }
                }

                SoundEngine.PlaySound(
                    SoundID.Item122 with { Pitch = 0.2f, Volume = 0.85f },
                    NPC.Center
                );
            }
        }
    }

    // =========================================================================
    // ATAQUES DE ESPINHOS POR FASE
    // =========================================================================

    private void UpdateAttacks(Player target, BossPhase phase)
    {
        if (SpikesProjectile.ActiveCount >= SpikeRainSystem.MaxGlobalSpikes)
            return;

        AttackTimer++;

        switch (phase)
        {
            case BossPhase.Phase1_Gathering:
                UpdatePhase1Attacks(target);
                break;

            case BossPhase.Phase2_Gale:
                UpdatePhase2Attacks(target);
                break;

            case BossPhase.Phase3_Cataclysm:
                UpdatePhase3Attacks(target);
                break;
        }
    }

    /// <summary>
    /// FASE 1: Cortina de espinhos escalonada com a dificuldade (a cada 40-55 ticks).
    /// </summary>
    private void UpdatePhase1Attacks(Player target)
    {
        int cooldown = Math.Max(25, (int)(55 * DifficultyRateMultiplier));
        if (AttackTimer >= cooldown)
        {
            AttackTimer = 0;

            int baseCount = Main.masterMode ? 4 : (Main.expertMode ? 3 : 2);
            int count = Math.Min(baseCount, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            for (int i = 0; i < count; i++)
            {
                float offsetX = Main.rand.NextFloat(-NPC.width * 0.35f, NPC.width * 0.35f);
                Vector2 spawnPos = NPC.Center + new Vector2(offsetX, NPC.height * 0.3f);
                Vector2 velocity = new(
                    Main.rand.NextFloat(-1.5f, 1.5f) * DifficultySpeedMultiplier,
                    Main.rand.NextFloat(8f, 12f) * DifficultySpeedMultiplier
                );

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPos,
                    velocity,
                    spikeType,
                    damage,
                    3.5f,
                    Main.myPlayer
                );
            }
        }
    }

    /// <summary>
    /// FASE 2: Rajadas diagonais cruzadas intensificadas no Expert/Master (a cada 20-28 ticks).
    /// </summary>
    private void UpdatePhase2Attacks(Player target)
    {
        int cooldown = Math.Max(14, (int)(28 * DifficultyRateMultiplier));
        if (AttackTimer >= cooldown)
        {
            AttackTimer = 0;
            InternalCounter++;

            int baseCount = Main.masterMode ? 6 : (Main.expertMode ? 5 : 4);
            int count = Math.Min(baseCount, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            bool alternateSide = (InternalCounter % 2 == 0);
            float baseAngle = alternateSide ? 45f : 135f;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.ToRadians(baseAngle + Main.rand.NextFloat(-24f, 24f));
                float speed = Main.rand.NextFloat(12f, 17f) * DifficultySpeedMultiplier;
                Vector2 velocity = angle.ToRotationVector2() * speed;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    velocity,
                    spikeType,
                    damage,
                    4f,
                    Main.myPlayer
                );
            }
        }
    }

    /// <summary>
    /// FASE 3: Tempestade frenética de espinhos 360° e leques velozes escalonados com a dificuldade.
    /// </summary>
    private void UpdatePhase3Attacks(Player target)
    {
        int cooldown = Math.Max(10, (int)(18 * DifficultyRateMultiplier));
        if (AttackTimer >= cooldown)
        {
            AttackTimer = 0;
            InternalCounter++;

            int desired = (InternalCounter % 3 == 0)
                ? (Main.masterMode ? 12 : (Main.expertMode ? 9 : 7))
                : (Main.masterMode ? 8 : (Main.expertMode ? 6 : 5));

            int count = Math.Min(desired, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            if (InternalCounter % 3 == 0)
            {
                // Disparo radial 360 graus
                for (int i = 0; i < count; i++)
                {
                    float angle = i * (MathHelper.TwoPi / desired) + Main.rand.NextFloat(-0.1f, 0.1f);
                    Vector2 velocity = angle.ToRotationVector2() * (14f * DifficultySpeedMultiplier);

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        velocity,
                        spikeType,
                        damage,
                        4.5f,
                        Main.myPlayer
                    );
                }
            }
            else
            {
                // Leque concentrado na direção do jogador
                Vector2 toTarget = target.Center - NPC.Center;
                float targetAngle = toTarget.ToRotation();

                for (int i = 0; i < count; i++)
                {
                    float spread = MathHelper.ToRadians((i - (count - 1) / 2f) * 14f);
                    Vector2 velocity = (targetAngle + spread).ToRotationVector2() * (Main.rand.NextFloat(14f, 19f) * DifficultySpeedMultiplier);

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        velocity,
                        spikeType,
                        damage,
                        4.5f,
                        Main.myPlayer
                    );
                }
            }
        }
    }

    // =========================================================================
    // ANIMAÇÃO FLUIDA E EFEITOS VISUAIS
    // =========================================================================

    private void UpdateAnimation()
    {
        NPC.frameCounter++;
        // Velocidades de transição mais rápidas para eliminar a rigidez
        int speed = CurrentPhase switch
        {
            BossPhase.Phase3_Cataclysm => 2,
            BossPhase.Phase2_Gale => 3,
            _ => 4
        };

        if (NPC.frameCounter >= speed)
        {
            NPC.frameCounter = 0;
            _frameIndex = (_frameIndex + 1) % FrameCount;
        }

        int frameHeight = 168;
        NPC.frame = new Rectangle(0, _frameIndex * frameHeight, 168, frameHeight);
    }

    public override void FindFrame(int frameHeight)
    {
        UpdateAnimation();
    }

    private void SpawnStormDust()
    {
        if (Main.rand.NextBool(2))
        {
            Vector2 offset = new(
                Main.rand.NextFloat(-NPC.width * 0.45f, NPC.width * 0.45f),
                Main.rand.NextFloat(-NPC.height * 0.45f, NPC.height * 0.45f)
            );

            int dustType = Main.rand.Next(3) switch
            {
                0 => DustID.CrimsonTorch,
                1 => DustID.RedTorch,
                _ => DustID.Blood
            };

            Dust dust = Dust.NewDustDirect(
                NPC.Center + offset,
                1,
                1,
                dustType,
                NPC.velocity.X * 0.3f,
                Main.rand.NextFloat(-3f, 1f),
                100,
                default,
                Main.rand.NextFloat(1.4f, 2.2f)
            );
            dust.noGravity = true;
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = CurrentPhase switch
        {
            BossPhase.Phase2_Gale => ModContent.Request<Texture2D>("O2ThornRain/Content/NPCs/ThornStormBoss_Phase2").Value,
            BossPhase.Phase3_Cataclysm => ModContent.Request<Texture2D>("O2ThornRain/Content/NPCs/ThornStormBoss_Phase3").Value,
            _ => TextureAssets.Npc[Type].Value
        };

        int frameHeight = texture.Height / FrameCount;
        Rectangle frame = new Rectangle(0, _frameIndex * frameHeight, texture.Width, frameHeight);
        Vector2 origin = frame.Size() / 2f;

        // Flutuação orgânica contínua vertical
        float floatOffset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f) * 8f;
        Vector2 drawPosition = NPC.Center - screenPos + new Vector2(0f, floatOffset);

        // Pulso suave conforme a intensidade da tempestade — baseScale +50% (2.05f → 3.07f)
        float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5.5f) * 0.14f;
        float baseScale = 3.07f + pulse;

        // Trilha de sombras e distorção de vento (afterimages) para fases 2 e 3
        if (CurrentPhase != BossPhase.Phase1_Gathering)
        {
            int trailCount = CurrentPhase == BossPhase.Phase3_Cataclysm ? 6 : 4;
            for (int i = 1; i <= trailCount; i++)
            {
                Vector2 trailPos = (NPC.oldPos.Length > i && NPC.oldPos[i] != Vector2.Zero ? NPC.oldPos[i] + NPC.Size / 2f : NPC.Center) - screenPos + new Vector2(0f, floatOffset);
                Color trailColor = (CurrentPhase == BossPhase.Phase3_Cataclysm
                    ? new Color(255, 30, 30, 0)
                    : new Color(220, 75, 45, 0)) * ((trailCount - i) / (float)trailCount * 0.45f);

                spriteBatch.Draw(
                    texture,
                    trailPos,
                    frame,
                    trailColor,
                    NPC.rotation,
                    origin,
                    baseScale * 0.96f,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        // Brilho pulsante externo (aura)
        Color auraColor = (CurrentPhase switch
        {
            BossPhase.Phase3_Cataclysm => new Color(255, 30, 30, 100),
            BossPhase.Phase2_Gale => new Color(255, 80, 40, 80),
            _ => new Color(220, 50, 50, 60)
        }) * (0.8f + pulse);

        for (int i = 0; i < 4; i++)
        {
            Vector2 offset = (i * MathHelper.PiOver2).ToRotationVector2() * (4f + pulse * 5f);
            spriteBatch.Draw(
                texture,
                drawPosition + offset,
                frame,
                auraColor * 0.35f,
                NPC.rotation,
                origin,
                baseScale,
                SpriteEffects.None,
                0f
            );
        }

        // Desenho principal do boss
        Color mainColor = NPC.GetAlpha(drawColor);
        spriteBatch.Draw(
            texture,
            drawPosition,
            frame,
            mainColor,
            NPC.rotation,
            origin,
            baseScale,
            SpriteEffects.None,
            0f
        );

        return false;
    }

    // =========================================================================
    // MORTE E CONCLUSÃO DO EVENTO
    // =========================================================================

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        // ─── MODO CLÁSSICO (sem boss bag) ──────────────────────────────
        // Barras de Luminita garantidas caem diretamente no chão
        LeadingConditionRule classicRule = new(new Conditions.NotExpert());
        classicRule.OnSuccess(ItemDropRule.Common(ItemID.LunarBar, 1, 25, 40));
        npcLoot.Add(classicRule);

        // ─── BOSS BAG (Expert / Master / FTW) ─────────────────────────
        // Bolsa contém moedas, luminita e a montaria DragonPower (% por dificuldade)
        npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ThornStormBossBag>()));

        // ─── TROFÉU: 10% de chance de drop direto em qualquer dificuldade ─
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ThornStormBossTrophy>(), 10));
    }

    public override void OnKill()
    {
        SoundEngine.PlaySound(
            SoundID.Item122 with
            {
                Volume = 1f,
                Pitch = -0.5f
            },
            NPC.Center
        );

        SoundEngine.PlaySound(
            SoundID.NPCDeath10 with
            {
                Volume = 1f,
                Pitch = -0.3f
            },
            NPC.Center
        );

        // Grande explosão de partículas carmesim e tempestade
        for (int i = 0; i < 70; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
            Dust.NewDustPerfect(
                NPC.Center,
                DustID.CrimsonTorch,
                velocity,
                100,
                default,
                Main.rand.NextFloat(1.5f, 2.4f)
            );

            Dust.NewDustPerfect(
                NPC.Center,
                DustID.Blood,
                velocity * 0.85f,
                100,
                default,
                Main.rand.NextFloat(1.2f, 2.0f)
            );
        }

        // Notifica o sistema do evento sobre a vitória (recompensas obtidas exclusivamente via Boss Bag)
        ThornStormEventSystem.OnBossDefeated();
    }
}
