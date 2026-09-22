using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// Estados do ciclo de vida do evento Thorn Tornado.
/// </summary>
public enum TornadoEventState
{
    None,

    Warning,

    Active,

    Cooldown
}


/// <summary>
/// Gerencia o ciclo de vida do Thorn Tornado.
///
/// O servidor possui autoridade exclusiva sobre:
/// - transições de estado
/// - escolha do jogador
/// - direção
/// - spawn do tornado
/// - mensagens multiplayer
/// </summary>
public class ThornTornadoSystem : ModSystem
{
    // =========================================================
    // CONFIGURAÇÃO CENTRALIZADA
    // =========================================================

    // A chance de spawn agora é dinâmica e determinada pela intensidade
    // da chuva de espinhos em SpikeRainSystem.GetCurrentTornadoChance().


    // 600 ticks = 10 segundos.
    public const int CheckIntervalTicks = 600;


    // 150 ticks = 2,5 segundos.
    public const int WarningSecondMessageDelayTicks = 150;


    // 240 ticks = 4 segundos.
    public const int WarningTotalDurationTicks = 240;


    // 3600 ticks = 60 segundos.
    public const int CooldownDurationTicks = 3600;


    // Distância horizontal do jogador.
    public const float SpawnDistanceX = 1200f;


    // =========================================================
    // DEBUG
    // =========================================================

    public static bool DebugForceTornado;


    // =========================================================
    // ESTADO
    // =========================================================

    public static TornadoEventState CurrentState
    {
        get;
        private set;
    } = TornadoEventState.None;


    private static int _stateTimer;

    private static int _checkTimer;


    // -1 = esquerda
    // +1 = direita
    private static float _chosenDirection = 1f;


    // Jogador usado como referência.
    private static int _targetPlayerIndex = -1;


    // =========================================================
    // UPDATE
    // =========================================================

    public override void PostUpdateWorld()
    {
        // Apenas servidor/singleplayer.
        if (
            Main.netMode ==
            NetmodeID.MultiplayerClient
        )
        {
            return;
        }


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (DebugForceTornado)
        {
            DebugForceTornado = false;

            StartTornadoWarning();

            return;
        }


        // -----------------------------------------------------
        // ESTADO ATUAL
        // -----------------------------------------------------

        switch (CurrentState)
        {
            case TornadoEventState.None:

                UpdateNoneState();

                break;


            case TornadoEventState.Warning:

                UpdateWarningState();

                break;


            case TornadoEventState.Active:

                UpdateActiveState();

                break;


            case TornadoEventState.Cooldown:

                UpdateCooldownState();

                break;
        }
    }


    // =========================================================
    // ESTADO NONE
    // =========================================================

    private static void UpdateNoneState()
    {
        // Só pode ocorrer durante a chuva.
        if (!Main.raining)
            return;


        _checkTimer++;


        if (
            _checkTimer <
            CheckIntervalTicks
        )
        {
            return;
        }


        _checkTimer = 0;


        // -----------------------------------------------------
        // PROCURA JOGADOR
        // -----------------------------------------------------

        int eligiblePlayer =
            FindEligibleRainPlayer();


        if (eligiblePlayer < 0)
            return;


        // -----------------------------------------------------
        // CHANCE BASEADA NA INTENSIDADE CLIMÁTICA
        // -----------------------------------------------------

        float currentTornadoChance =
            SpikeRainSystem.GetCurrentTornadoChance();

        if (
            Main.rand.NextFloat() <=
            currentTornadoChance
        )
        {
            _targetPlayerIndex =
                eligiblePlayer;


            StartTornadoWarning();
        }
    }


    // =========================================================
    // INICIA AVISO
    // =========================================================

    public static void StartTornadoWarning()
    {
        CurrentState =
            TornadoEventState.Warning;


        _stateTimer = 0;


        // Se o jogador não for válido,
        // procura outro.
        if (
            _targetPlayerIndex < 0 ||
            !IsPlayerValidTarget(
                _targetPlayerIndex
            )
        )
        {
            _targetPlayerIndex =
                FindEligibleRainPlayer();
        }


        // -----------------------------------------------------
        // DIREÇÃO
        // -----------------------------------------------------

        _chosenDirection =
            Main.rand.NextBool()
                ? 1f
                : -1f;


        // -----------------------------------------------------
        // MENSAGEM 1
        // -----------------------------------------------------

        BroadcastEventMessage(
            "Mods.O2ThornRain.Events.TornadoWarning",
            new Color(
                80,
                200,
                160
            )
        );


        // -----------------------------------------------------
        // SOM
        // -----------------------------------------------------

        SoundEngine.PlaySound(
            SoundID.Item121 with
            {
                Volume = 0.5f,
                Pitch = -0.3f
            }
        );
    }


    // =========================================================
    // ESTADO WARNING
    // =========================================================

    private static void UpdateWarningState()
    {
        _stateTimer++;


        // -----------------------------------------------------
        // SEGUNDA MENSAGEM
        // -----------------------------------------------------

        if (
            _stateTimer ==
            WarningSecondMessageDelayTicks
        )
        {
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.TornadoIncoming",
                new Color(
                    255,
                    120,
                    80
                )
            );


            SoundEngine.PlaySound(
                SoundID.Item122 with
                {
                    Volume = 0.75f,
                    Pitch = -0.1f
                }
            );
        }


        // -----------------------------------------------------
        // SPAWN
        // -----------------------------------------------------

        if (
            _stateTimer >=
            WarningTotalDurationTicks
        )
        {
            SpawnThornTornado();


            CurrentState =
                TornadoEventState.Active;


            _stateTimer = 0;
        }
    }


    // =========================================================
    // ESTADO ACTIVE
    // =========================================================

    private static void UpdateActiveState()
    {
        int tornadoType =
            ModContent.ProjectileType<
                ThornTornadoProjectile
            >();


        bool tornadoExists = false;


        for (
            int i = 0;
            i < Main.maxProjectiles;
            i++
        )
        {
            Projectile projectile =
                Main.projectile[i];


            if (
                projectile.active &&
                projectile.type ==
                tornadoType
            )
            {
                tornadoExists = true;

                break;
            }
        }


        // -----------------------------------------------------
        // TERMINOU
        // -----------------------------------------------------

        if (!tornadoExists)
        {
            CurrentState =
                TornadoEventState.Cooldown;


            _stateTimer = 0;

            _targetPlayerIndex = -1;
        }
    }


    // =========================================================
    // COOLDOWN
    // =========================================================

    private static void UpdateCooldownState()
    {
        _stateTimer++;


        if (
            _stateTimer >=
            CooldownDurationTicks
        )
        {
            CurrentState =
                TornadoEventState.None;


            _stateTimer = 0;

            _checkTimer = 0;
        }
    }


    // =========================================================
    // SPAWN DO TORNADO
    // =========================================================

    private static void SpawnThornTornado()
    {
        Vector2 spawnPosition =
            GetTornadoSpawnPosition();


        int tornadoType =
            ModContent.ProjectileType<
                ThornTornadoProjectile
            >();


        Projectile.NewProjectile(
            Entity.GetSource_NaturalSpawn(),
            spawnPosition,
            new Vector2(
                _chosenDirection *
                ThornTornadoProjectile.HorizontalSpeed,
                0f
            ),
            tornadoType,
            0,
            0f,
            Main.myPlayer,
            ai0: _chosenDirection
        );
    }


    // =========================================================
    // POSIÇÃO DO TORNADO
    // =========================================================

    private static Vector2 GetTornadoSpawnPosition()
    {
        Player targetPlayer = null;


        // -----------------------------------------------------
        // JOGADOR ALVO
        // -----------------------------------------------------

        if (
            _targetPlayerIndex >= 0 &&
            IsPlayerValidTarget(
                _targetPlayerIndex
            )
        )
        {
            targetPlayer =
                Main.player[
                    _targetPlayerIndex
                ];
        }
        else
        {
            int index =
                FindEligibleRainPlayer();


            if (index >= 0)
            {
                targetPlayer =
                    Main.player[index];
            }
        }


        // -----------------------------------------------------
        // FALLBACK
        // -----------------------------------------------------

        if (targetPlayer == null)
        {
            return new Vector2(
                Main.maxTilesX * 8f,
                (float)Main.worldSurface * 16f - 100f
            );
        }


        // -----------------------------------------------------
        // POSIÇÃO HORIZONTAL
        // -----------------------------------------------------

        float spawnX =
            targetPlayer.Center.X -
            (
                _chosenDirection *
                SpawnDistanceX
            );


        // -----------------------------------------------------
        // ALTURA
        // -----------------------------------------------------

        // O centro do tornado fica um pouco acima
        // do jogador.
        float spawnY =
            targetPlayer.Center.Y -
            60f;


        return new Vector2(
            spawnX,
            spawnY
        );
    }


    // =========================================================
    // PROCURA JOGADOR
    // =========================================================

    private static int FindEligibleRainPlayer()
    {
        for (
            int i = 0;
            i < Main.maxPlayers;
            i++
        )
        {
            if (
                IsPlayerValidTarget(i)
            )
            {
                return i;
            }
        }


        return -1;
    }


    // =========================================================
    // VALIDA JOGADOR
    // =========================================================

    private static bool IsPlayerValidTarget(
        int playerIndex)
    {
        if (
            playerIndex < 0 ||
            playerIndex >= Main.maxPlayers
        )
        {
            return false;
        }


        Player player =
            Main.player[playerIndex];


        if (
            !player.active ||
            player.dead
        )
        {
            return false;
        }


        return SpikeRainSystem
            .IsPlayerInRainZone(player);
    }


    // =========================================================
    // MENSAGEM
    // =========================================================

    private static void BroadcastEventMessage(
        string translationKey,
        Color color)
    {
        // -----------------------------------------------------
        // SINGLEPLAYER
        // -----------------------------------------------------

        if (
            Main.netMode ==
            NetmodeID.SinglePlayer
        )
        {
            Main.NewText(
                Language.GetTextValue(
                    translationKey
                ),
                color
            );

            return;
        }


        // -----------------------------------------------------
        // MULTIPLAYER SERVER
        // -----------------------------------------------------

        if (
            Main.netMode ==
            NetmodeID.Server
        )
        {
            ChatHelper.BroadcastChatMessage(
                NetworkText.FromKey(
                    translationKey
                ),
                color
            );
        }
    }
}