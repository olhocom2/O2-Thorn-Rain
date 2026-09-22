using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;
using O2ThornRain.Content.Items;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Content.NPCs;

/// <summary>
/// Boss final do evento "A Tempestade dos Quatro".
/// Um tornado gigantesco carmesim/vermelho que persegue e pressiona o jogador,
/// possui 3 fases de combate de acordo com sua vida, desfere chuvas de espinhos,
/// cospe Dragões de Espinhos (Thorn Dragons) e dropa o item "Poder do Dragão" ao ser derrotado.
/// </summary>
public class ThornStormBoss : ModNPC
{
    // Reutiliza o sprite do tornado pet (Tempest) do Duke Fishron
    public override string Texture =>
        $"Terraria/Images/Projectile_{ProjectileID.Tempest}";

    private const int FrameCount = 6;
    private const int AnimationSpeed = 3;

    // Fases de combate
    public enum BossPhase
    {
        Phase1_Gathering,   // 100% - 70% HP: Rastreamento horizontal e cortina de espinhos
        Phase2_Gale,        // 70% - 40% HP: Velocidade aumentada, rajadas diagonais
        Phase3_Cataclysm    // < 40% HP: Tempestade violenta, velocidade extrema, rajadas radiais
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

    // Variáveis de IA armazenadas nos slots de AI do NPC para suporte total à sincronização
    private ref float AttackTimer => ref NPC.ai[0];
    private ref float ChargeTimer => ref NPC.ai[1];
    private ref float ChargeState => ref NPC.ai[2]; // 0: normal, 1: preparando investida, 2: investindo
    private ref float InternalCounter => ref NPC.ai[3];

    private int _dragonTimer;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = FrameCount;

        NPCID.Sets.BossBestiaryPriority.Add(Type);
        NPCID.Sets.MPAllowedEnemies[Type] = true;

        NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
    }

    public override void SetDefaults()
    {
        NPC.width = 180;
        NPC.height = 420;

        NPC.damage = 70;
        NPC.defense = 32;
        NPC.lifeMax = 45000;

        NPC.HitSound = SoundID.NPCHit3;
        NPC.DeathSound = SoundID.NPCDeath10;

        NPC.knockBackResist = 0f;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;

        NPC.boss = true;
        NPC.friendly = false;
        NPC.value = Item.buyPrice(gold: 15);

        Music = MusicID.Boss2;
    }

    public override bool CheckActive()
    {
        // Enquanto o jogador estiver vivo, nunca sofre despawn por distância.
        // Se o jogador estiver morto ou inativo, permite o despawn natural.
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
        // 1. GERENCIAMENTO DE ALVO E DESPAWN QUANDO TODOS MORREM
        // -------------------------------------------------------------
        if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
        {
            NPC.TargetClosest(true);
        }

        Player targetPlayer = Main.player[NPC.target];

        if (!targetPlayer.active || targetPlayer.dead)
        {
            // Todos os jogadores morreram: sobe para os céus e desaparece
            NPC.velocity.Y -= 0.5f;
            NPC.velocity.X *= 0.95f;
            NPC.EncourageDespawn(10);

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.timeLeft <= 1)
            {
                ThornStormEventSystem.OnBossEscaped();
            }
            return;
        }

        // -------------------------------------------------------------
        // 2. ANIMAÇÃO DE ROTAÇÃO E PARTÍCULAS TEMPESTUOSAS
        // -------------------------------------------------------------
        UpdateAnimation();
        SpawnStormDust();

        // -------------------------------------------------------------
        // 3. FASE ATUAL E DETERMINAÇÃO DE VELOCIDADE
        // -------------------------------------------------------------
        float lifeRatio = (float)NPC.life / NPC.lifeMax;
        BossPhase phase = lifeRatio > 0.70f ? BossPhase.Phase1_Gathering :
                          lifeRatio > 0.40f ? BossPhase.Phase2_Gale :
                          BossPhase.Phase3_Cataclysm;

        // -------------------------------------------------------------
        // 4. MOVIMENTAÇÃO DE PERSEGUIÇÃO E PRESSÃO AÉREA
        // -------------------------------------------------------------
        UpdateMovement(targetPlayer, phase);

        // Apenas o servidor gera projéteis e orquestra ataques
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // -------------------------------------------------------------
        // 5. ATAQUES DE ESPINHOS POR FASE (RESPEITA MAXGLOBALSPIKES = 120)
        // -------------------------------------------------------------
        UpdateAttacks(targetPlayer, phase);
    }

    // =========================================================================
    // MOVIMENTAÇÃO E PRESSÃO
    // =========================================================================

    private void UpdateMovement(Player target, BossPhase phase)
    {
        // Configurações de velocidade baseadas na fase de agressividade
        float maxSpeedX;
        float accelX;
        float hoverOffsetY;

        switch (phase)
        {
            case BossPhase.Phase1_Gathering:
                maxSpeedX = 7.5f;
                accelX = 0.14f;
                hoverOffsetY = -240f;
                break;

            case BossPhase.Phase2_Gale:
                maxSpeedX = 11.5f;
                accelX = 0.24f;
                hoverOffsetY = -200f;
                break;

            case BossPhase.Phase3_Cataclysm:
            default:
                maxSpeedX = 15.5f;
                accelX = 0.38f;
                hoverOffsetY = -160f;
                break;
        }

        // Sistema de Investida Aérea (disponível nas fases 2 e 3)
        if (phase != BossPhase.Phase1_Gathering)
        {
            ChargeTimer++;

            int chargeInterval = (phase == BossPhase.Phase2_Gale) ? 360 : 260; // a cada 6s ou 4.3s

            if (ChargeState == 0 && ChargeTimer >= chargeInterval)
            {
                // Inicia preparação da investida: desacelera e acumula vento
                ChargeState = 1;
                ChargeTimer = 0;
                NPC.velocity *= 0.5f;
                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.4f, Volume = 0.9f }, NPC.Center);
            }
            else if (ChargeState == 1)
            {
                // Preparando por 40 ticks
                if (ChargeTimer >= 40)
                {
                    ChargeState = 2;
                    ChargeTimer = 0;

                    // Lança investida na direção do jogador
                    Vector2 chargeDirection = (target.Center - NPC.Center);
                    if (chargeDirection != Vector2.Zero)
                        chargeDirection.Normalize();

                    float chargeSpeed = (phase == BossPhase.Phase2_Gale) ? 14f : 19f;
                    NPC.velocity = chargeDirection * chargeSpeed;

                    SoundEngine.PlaySound(SoundID.ForceRoar with { Pitch = -0.2f }, NPC.Center);
                }
                return;
            }
            else if (ChargeState == 2)
            {
                // Investindo por 35 ticks
                if (ChargeTimer >= 35)
                {
                    ChargeState = 0;
                    ChargeTimer = 0;
                    NPC.velocity *= 0.4f;
                }
                return;
            }
        }

        // Movimento padrão: pairar estrategicamente sobre o jogador
        Vector2 targetHoverPos = target.Center + new Vector2(0f, hoverOffsetY);
        Vector2 toTarget = targetHoverPos - NPC.Center;

        // Movimentação horizontal
        if (toTarget.X > 40f)
        {
            NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + accelX, -maxSpeedX, maxSpeedX);
        }
        else if (toTarget.X < -40f)
        {
            NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X - accelX, -maxSpeedX, maxSpeedX);
        }
        else
        {
            NPC.velocity.X *= 0.95f;
        }

        // Movimentação vertical suave
        float targetYVel = MathHelper.Clamp(toTarget.Y * 0.04f, -7f, 7f);
        NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, targetYVel, 0.08f);
    }

    // =========================================================================
    // ATAQUES DE ESPINHOS (DISPAROS EQUILIBRADOS E SEGUROS)
    // =========================================================================

    private void UpdateAttacks(Player target, BossPhase phase)
    {
        // 1. Invocação periódica de Dragões de Espinhos (Thorn Dragons)
        UpdateThornDragons(target, phase);

        // 2. Se o limite global de espinhos já foi atingido, não dispara novos espinhos
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
    /// FASE 1: Cortina descendente de espinhos a cada 40 ticks (0.66s).
    /// </summary>
    private void UpdatePhase1Attacks(Player target)
    {
        if (AttackTimer >= 40)
        {
            AttackTimer = 0;

            int count = Math.Min(3, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            for (int i = 0; i < count; i++)
            {
                float offsetX = Main.rand.NextFloat(-NPC.width * 0.4f, NPC.width * 0.4f);
                Vector2 spawnPos = NPC.Center + new Vector2(offsetX, NPC.height * 0.35f);
                Vector2 velocity = new(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(8f, 13f));

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
    /// FASE 2: Rajadas diagonais cruzadas a cada 28 ticks (0.46s).
    /// </summary>
    private void UpdatePhase2Attacks(Player target)
    {
        if (AttackTimer >= 28)
        {
            AttackTimer = 0;
            InternalCounter++;

            int count = Math.Min(4, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            bool alternateSide = (InternalCounter % 2 == 0);
            float baseAngle = alternateSide ? 45f : 135f;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.ToRadians(baseAngle + Main.rand.NextFloat(-20f, 20f));
                float speed = Main.rand.NextFloat(12f, 17f);
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
    /// FASE 3: Tempestade furiosa com rajadas radiais e leques a cada 20 ticks (0.33s).
    /// </summary>
    private void UpdatePhase3Attacks(Player target)
    {
        if (AttackTimer >= 20)
        {
            AttackTimer = 0;
            InternalCounter++;

            int desired = (InternalCounter % 3 == 0) ? 6 : 4;
            int count = Math.Min(desired, SpikeRainSystem.MaxGlobalSpikes - SpikesProjectile.ActiveCount);
            int spikeType = ModContent.ProjectileType<SpikesProjectile>();
            int damage = SpikeRainSystem.GetSpikeDamage();

            if (InternalCounter % 3 == 0)
            {
                // Disparo radial 360 graus
                for (int i = 0; i < count; i++)
                {
                    float angle = i * (MathHelper.TwoPi / desired) + Main.rand.NextFloat(-0.1f, 0.1f);
                    Vector2 velocity = angle.ToRotationVector2() * 13f;

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
                    float spread = MathHelper.ToRadians((i - (count - 1) / 2f) * 16f);
                    Vector2 velocity = (targetAngle + spread).ToRotationVector2() * Main.rand.NextFloat(13f, 18f);

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

        // =========================================================================
        // ETAPA 3: Invocação dos Dragões de Espinhos (Thorn Dragons)
        // =========================================================================
    }

    private void UpdateThornDragons(Player target, BossPhase phase)
    {
        _dragonTimer++;

        int interval;
        int maxDragons;

        switch (phase)
        {
            case BossPhase.Phase1_Gathering:
                interval = 480; // a cada 8 segundos
                maxDragons = 1;
                break;

            case BossPhase.Phase2_Gale:
                interval = 300; // a cada 5 segundos
                maxDragons = 2;
                break;

            case BossPhase.Phase3_Cataclysm:
            default:
                interval = 210; // a cada 3.5 segundos
                maxDragons = 3;
                break;
        }

        if (_dragonTimer >= interval)
        {
            _dragonTimer = 0;

            int dragonType = ModContent.ProjectileType<ThornDragonProjectile>();
            int currentDragons = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == dragonType)
                    currentDragons++;
            }

            if (currentDragons < maxDragons)
            {
                Vector2 toTarget = target.Center - NPC.Center;
                if (toTarget != Vector2.Zero)
                    toTarget.Normalize();
                else
                    toTarget = new Vector2(0f, 1f);

                toTarget *= 14f;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    toTarget,
                    dragonType,
                    ThornDragonProjectile.ThornDragonDamage,
                    4f,
                    Main.myPlayer
                );

                SoundEngine.PlaySound(
                    SoundID.ForceRoar with { Pitch = 0.15f, Volume = 0.9f },
                    NPC.Center
                );
            }
        }
    }

    // =========================================================================
    // ANIMAÇÃO E EFEITOS VISUAIS
    // =========================================================================

    private void UpdateAnimation()
    {
        NPC.frameCounter++;
        if (NPC.frameCounter >= AnimationSpeed)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y += NPC.height;
            if (NPC.frame.Y >= NPC.height * FrameCount)
            {
                NPC.frame.Y = 0;
            }
        }
    }

    private void SpawnStormDust()
    {
        // Partículas atmosféricas do boss: tornado vermelho carmesim, fogo, sangue e eletricidade
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
                Main.rand.NextFloat(1.3f, 2.0f)
            );
            dust.noGravity = true;
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Utiliza o modelo do pet tornado (Tempest) do Duke Fishron desenhado em escala gigante e cor vermelha
        Texture2D texture = TextureAssets.Projectile[ProjectileID.Tempest].Value;

        Rectangle frame = texture.Frame(
            1,
            FrameCount,
            0,
            (int)(NPC.frame.Y / (float)NPC.height) % FrameCount
        );

        Vector2 origin = frame.Size() / 2f;
        Vector2 drawPosition = NPC.Center - screenPos;

        // Tonalidade vermelha intensa e pulsante
        float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.15f;
        Color redTint = new Color(
            (byte)(240 + pulse * 15f),
            (byte)(25 + pulse * 10f),
            (byte)(35 + pulse * 10f)
        );

        Color finalColor = NPC.GetAlpha(redTint * 0.95f);

        // Desenha 7 camadas sobrepostas para dar densidade, profundidade e escala gigantesca ao tornado
        for (int i = 0; i < 7; i++)
        {
            float verticalOffset = (i - 3f) * 55f;
            float scaleX = 4.2f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + i) * 0.35f;
            float scaleY = 1.6f;

            spriteBatch.Draw(
                texture,
                drawPosition + new Vector2(0f, verticalOffset),
                frame,
                finalColor,
                (i % 2 == 0 ? 1 : -1) * Main.GlobalTimeWrappedHourly * 0.7f,
                origin,
                new Vector2(scaleX, scaleY),
                SpriteEffects.None,
                0f
            );
        }

        return false;
    }

    // =========================================================================
    // MORTE E CONCLUSÃO DO EVENTO
    // =========================================================================

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
        for (int i = 0; i < 60; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
            Dust.NewDustPerfect(
                NPC.Center,
                DustID.CrimsonTorch,
                velocity,
                100,
                default,
                Main.rand.NextFloat(1.4f, 2.2f)
            );

            Dust.NewDustPerfect(
                NPC.Center,
                DustID.Blood,
                velocity * 0.8f,
                100,
                default,
                Main.rand.NextFloat(1.2f, 1.8f)
            );
        }

        // Drop garantido (100%) da recompensa do evento: Poder do Dragão
        Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DragonPower>());

        // Notifica o sistema do evento sobre a vitória
        ThornStormEventSystem.OnBossDefeated();
    }
}
