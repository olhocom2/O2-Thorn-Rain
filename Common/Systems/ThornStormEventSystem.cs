using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using O2ThornRain.Content.NPCs;
using Terraria.Audio;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// Estados do evento "A Tempestade dos Quatro".
/// </summary>
public enum ThornStormEventState : byte
{
    Inactive,
    Summoned,
    Active,
    FinalStorm,
    Completed
}

/// <summary>
/// Biomas dos quatro tornados ancestrais.
/// </summary>
public enum TornadoBiome : byte
{
    Jungle,
    Snow,
    Desert,
    Corruption
}

/// <summary>
/// Gerencia o ciclo de vida completo do evento "A Tempestade dos Quatro".
///
/// Autoridade exclusiva do servidor em multiplayer para:
/// - Spawn do Mini Tornado de invocação no oceano durante Storm
/// - Início do evento e spawn dos 4 tornados de bioma
/// - Rastreamento dos tornados derrotados (0/4 a 4/4)
/// - Transição para a FinalStorm
/// </summary>
public class ThornStormEventSystem : ModSystem
{
    // =========================================================
    // CONSTANTES DE BALANCEAMENTO
    // =========================================================

    // Intervalo de verificação de spawn do Mini Tornado (300 ticks = 5 segundos)
    private const int SummonCheckIntervalTicks = 300;

    // Chance de tentar o spawn do Mini Tornado a cada verificação durante Storm (15%)
    private const float MiniTornadoSpawnChance = 0.15f;

    // =========================================================
    // ESTADO DO EVENTO
    // =========================================================

    public static ThornStormEventState CurrentState { get; private set; } = ThornStormEventState.Inactive;

    public static bool JungleDefeated { get; private set; }
    public static bool SnowDefeated { get; private set; }
    public static bool DesertDefeated { get; private set; }
    public static bool CorruptionDefeated { get; private set; }

    public static int DefeatedCount =>
        (JungleDefeated ? 1 : 0) +
        (SnowDefeated ? 1 : 0) +
        (DesertDefeated ? 1 : 0) +
        (CorruptionDefeated ? 1 : 0);

    private static int _checkTimer;
    private static int _finalStormTimer;
    private static bool _announcedFinalStormWarning;

    // =========================================================
    // CICLO DE VIDA DO MUNDO
    // =========================================================

    public override void ClearWorld()
    {
        CurrentState = ThornStormEventState.Inactive;
        JungleDefeated = false;
        SnowDefeated = false;
        DesertDefeated = false;
        CorruptionDefeated = false;
        _checkTimer = 0;
        _finalStormTimer = 0;
        _announcedFinalStormWarning = false;
    }

    // =========================================================
    // ATUALIZAÇÃO DO MUNDO
    // =========================================================

    public override void PostUpdateWorld()
    {
        // Apenas servidor e singleplayer tomam decisões sobre o evento.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // Se o evento estiver inativo, verifica se o Mini Tornado pode surgir no oceano
        if (CurrentState == ThornStormEventState.Inactive)
        {
            UpdateMiniTornadoSpawnCheck();
        }
        else if (CurrentState == ThornStormEventState.FinalStorm)
        {
            UpdateFinalStorm();
        }
    }

    // =========================================================
    // VERIFICAÇÃO DE SPAWN DO MINI TORNADO
    // =========================================================

    private static void UpdateMiniTornadoSpawnCheck()
    {
        // =========================================================================
        // MODO DE TESTES: Mini Tornado ativo o tempo todo
        // (Condições de clima e chance comentadas para permitir spawn imediato em testes)
        // Descomente o bloco abaixo para restaurar a exigência de chuva e tempestade.
        // =========================================================================
        /*
        // Apenas durante chuva ativa e intensidade Storm
        if (!Main.raining || SpikeRainSystem.CurrentIntensity != ThornRainIntensity.Storm)
            return;
        */

        _checkTimer++;
        // Para testes: verificação rápida (60 ticks = 1 segundo)
        if (_checkTimer < 60) // Original: SummonCheckIntervalTicks (300 ticks)
            return;

        _checkTimer = 0;

        // Verifica se já existe um Mini Tornado ativo no mundo
        int summonNpcType = ModContent.NPCType<ThornStormSummonTornado>();
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.type == summonNpcType)
                return;
        }

        /*
        // Rola a chance de spawn (original: 15% por verificação)
        if (Main.rand.NextFloat() > MiniTornadoSpawnChance)
            return;
        */

        // Procura uma posição no fundo do oceano
        Vector2 spawnPosition = FindOceanFloorPosition();
        if (spawnPosition == Vector2.Zero)
            return;

        int npcIndex = NPC.NewNPC(
            Entity.GetSource_NaturalSpawn(),
            (int)spawnPosition.X,
            (int)spawnPosition.Y,
            summonNpcType
        );

        if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
        {
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.MiniTornadoSpawned",
                new Color(90, 180, 240)
            );
        }
    }

    // =========================================================
    // CONTROLE DO EVENTO
    // =========================================================

    /// <summary>
    /// Chamado quando o Mini Tornado é destruído pelo jogador.
    /// </summary>
    public static void StartEvent()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        CurrentState = ThornStormEventState.Active;
        JungleDefeated = false;
        SnowDefeated = false;
        DesertDefeated = false;
        CorruptionDefeated = false;

        // Spawna os 4 tornados de bioma
        SpawnBiomeTornado(TornadoBiome.Jungle);
        SpawnBiomeTornado(TornadoBiome.Snow);
        SpawnBiomeTornado(TornadoBiome.Desert);
        SpawnBiomeTornado(TornadoBiome.Corruption);

        BroadcastEventMessage(
            "Mods.O2ThornRain.Events.StormOfFourStarted",
            new Color(255, 120, 50)
        );

        SyncEventState();
    }

    /// <summary>
    /// Registra a destruição de um dos 4 tornados de bioma.
    /// </summary>
    public static void RegisterTornadoDefeated(TornadoBiome biome)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        string biomeNameKey;
        switch (biome)
        {
            case TornadoBiome.Jungle:
                if (JungleDefeated) return;
                JungleDefeated = true;
                biomeNameKey = "Mods.O2ThornRain.Biomes.Jungle";
                break;

            case TornadoBiome.Snow:
                if (SnowDefeated) return;
                SnowDefeated = true;
                biomeNameKey = "Mods.O2ThornRain.Biomes.Snow";
                break;

            case TornadoBiome.Desert:
                if (DesertDefeated) return;
                DesertDefeated = true;
                biomeNameKey = "Mods.O2ThornRain.Biomes.Desert";
                break;

            case TornadoBiome.Corruption:
                if (CorruptionDefeated) return;
                CorruptionDefeated = true;
                biomeNameKey = "Mods.O2ThornRain.Biomes.Corruption";
                break;

            default:
                return;
        }

        string biomeName = Language.GetTextValue(biomeNameKey);
        BroadcastFormattedMessage(
            "Mods.O2ThornRain.Events.TornadoDefeated",
            new Color(100, 240, 160),
            biomeName,
            DefeatedCount
        );

        SyncEventState();

        // Se os 4 foram derrotados, prepara a tempestade final!
        if (DefeatedCount >= 4)
        {
            CurrentState = ThornStormEventState.FinalStorm;
            _finalStormTimer = 0;
            _announcedFinalStormWarning = false;

            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.AllFourDefeated",
                new Color(255, 180, 60)
            );

            SyncEventState();
        }
    }

    // =========================================================
    // CONTROLE DA TEMPESTADE FINAL E DO BOSS
    // =========================================================

    private static void UpdateFinalStorm()
    {
        int bossType = ModContent.NPCType<ThornStormBoss>();

        // Verifica se o boss já está ativo no mundo
        bool bossAlive = false;
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
            return;

        _finalStormTimer++;

        // 180 ticks (3 segundos): aviso da aproximação colossal
        if (_finalStormTimer >= 180 && !_announcedFinalStormWarning)
        {
            _announcedFinalStormWarning = true;
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.FinalStormIncoming",
                new Color(255, 60, 60)
            );
        }

        // 360 ticks (6 segundos): spawn do Boss nos céus acima do jogador
        if (_finalStormTimer >= 360)
        {
            _finalStormTimer = 0;

            Player target = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead)
                {
                    target = p;
                    break;
                }
            }

            if (target == null)
                return;

            Vector2 spawnPos = target.Center + new Vector2(0f, -600f);

            int npcIndex = NPC.NewNPC(
                Entity.GetSource_NaturalSpawn(),
                (int)spawnPos.X,
                (int)spawnPos.Y,
                bossType
            );

            if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
            {
                Main.npc[npcIndex].netUpdate = true;
                SoundEngine.PlaySound(SoundID.Roar, target.Center);
            }
        }
    }

    /// <summary>
    /// Chamado quando o Boss final é derrotado pelo jogador.
    /// </summary>
    public static void OnBossDefeated()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        CurrentState = ThornStormEventState.Completed;

        BroadcastEventMessage(
            "Mods.O2ThornRain.Events.FinalStormVictory",
            new Color(120, 255, 160)
        );

        SyncEventState();
    }

    /// <summary>
    /// Chamado caso todos os jogadores morram e o Boss escape para os céus.
    /// Reseta o evento para que possa ser tentado novamente.
    /// </summary>
    public static void OnBossEscaped()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        CurrentState = ThornStormEventState.Inactive;
        JungleDefeated = false;
        SnowDefeated = false;
        DesertDefeated = false;
        CorruptionDefeated = false;
        _finalStormTimer = 0;
        _announcedFinalStormWarning = false;

        SyncEventState();
    }

    // =========================================================
    // CRIAÇÃO DOS TORNADOS NOS BIOMAS
    // =========================================================

    private static void SpawnBiomeTornado(TornadoBiome biome)
    {
        Vector2 position = FindBiomeSurfacePosition(biome);
        int npcType = ModContent.NPCType<ThornBiomeTornado>();

        int npcIndex = NPC.NewNPC(
            Entity.GetSource_NaturalSpawn(),
            (int)position.X,
            (int)position.Y,
            npcType,
            ai0: (int)biome
        );

        if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
        {
            Main.npc[npcIndex].netUpdate = true;
        }
    }

    // =========================================================
    // LOCALIZAÇÃO DE POSIÇÕES DE BIOMA NO MUNDO
    // =========================================================

    public static Vector2 FindBiomeSurfacePosition(TornadoBiome biome)
    {
        int targetTileType;
        float defaultFraction;

        switch (biome)
        {
            case TornadoBiome.Jungle:
                targetTileType = TileID.JungleGrass;
                defaultFraction = 0.78f;
                break;

            case TornadoBiome.Snow:
                targetTileType = TileID.SnowBlock;
                defaultFraction = 0.22f;
                break;

            case TornadoBiome.Desert:
                targetTileType = TileID.Sand;
                defaultFraction = 0.38f;
                break;

            case TornadoBiome.Corruption:
                targetTileType = TileID.CorruptGrass;
                defaultFraction = 0.62f;
                break;

            default:
                targetTileType = TileID.Grass;
                defaultFraction = 0.5f;
                break;
        }

        // Amostragem de colunas para encontrar a área de maior concentração do bioma
        int bestX = (int)(Main.maxTilesX * defaultFraction);
        int startX = (int)(Main.maxTilesX * 0.1f);
        int endX = (int)(Main.maxTilesX * 0.9f);
        int step = 20;

        for (int x = startX; x < endX; x += step)
        {
            int surfaceY = (int)Main.worldSurface;
            for (int y = surfaceY - 80; y < surfaceY + 120; y++)
            {
                if (!WorldGen.InWorld(x, y)) continue;

                Tile tile = Main.tile[x, y];
                if (tile.HasTile && tile.TileType == targetTileType)
                {
                    bestX = x;
                    goto FoundBiomeX;
                }
            }
        }

    FoundBiomeX:
        // Encontra o topo da superfície sólida nessa coluna
        int groundY = (int)Main.worldSurface;
        for (int y = (int)(Main.worldSurface * 0.4f); y < (int)Main.worldSurface + 150; y++)
        {
            if (!WorldGen.InWorld(bestX, y)) continue;

            Tile tile = Main.tile[bestX, y];
            if (tile.HasTile && Main.tileSolid[tile.TileType])
            {
                groundY = y;
                break;
            }
        }

        // Flutuando aproximadamente 120px acima do chão
        return new Vector2(bestX * 16f, groundY * 16f - 120f);
    }

    private static Vector2 FindOceanFloorPosition()
    {
        // Escolhe o oceano esquerdo ou direito aleatoriamente
        bool useLeft = Main.rand.NextBool();
        int oceanX = useLeft
            ? Main.rand.Next(60, 150)
            : Main.rand.Next(Main.maxTilesX - 150, Main.maxTilesX - 60);

        int groundY = (int)Main.worldSurface;
        for (int y = (int)(Main.worldSurface * 0.5f); y < (int)Main.worldSurface + 180; y++)
        {
            if (!WorldGen.InWorld(oceanX, y)) continue;

            Tile tile = Main.tile[oceanX, y];
            if (tile.HasTile && (tile.TileType == TileID.Sand || Main.tileSolid[tile.TileType]))
            {
                groundY = y;
                break;
            }
        }

        return new Vector2(oceanX * 16f, groundY * 16f - 40f);
    }

    // =========================================================
    // MULTIPLAYER E SINCRONIZAÇÃO
    // =========================================================

    public static void SyncEventState(int toWho = -1, int fromWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket packet = ModContent.GetInstance<O2ThornRain>().GetPacket();
        packet.Write((byte)MessageType.SyncThornStormEventState);
        packet.Write((byte)CurrentState);
        packet.Write(JungleDefeated);
        packet.Write(SnowDefeated);
        packet.Write(DesertDefeated);
        packet.Write(CorruptionDefeated);
        packet.Send(toWho, fromWho);
    }

    public static void SetStateFromNet(
        ThornStormEventState state,
        bool jungle,
        bool snow,
        bool desert,
        bool corruption)
    {
        CurrentState = state;
        JungleDefeated = jungle;
        SnowDefeated = snow;
        DesertDefeated = desert;
        CorruptionDefeated = corruption;
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)CurrentState);
        writer.Write(JungleDefeated);
        writer.Write(SnowDefeated);
        writer.Write(DesertDefeated);
        writer.Write(CorruptionDefeated);
    }

    public override void NetReceive(BinaryReader reader)
    {
        CurrentState = (ThornStormEventState)reader.ReadByte();
        JungleDefeated = reader.ReadBoolean();
        SnowDefeated = reader.ReadBoolean();
        DesertDefeated = reader.ReadBoolean();
        CorruptionDefeated = reader.ReadBoolean();
    }

    private static void BroadcastEventMessage(string translationKey, Color color)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(Language.GetTextValue(translationKey), color);
            return;
        }

        if (Main.netMode == NetmodeID.Server)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromKey(translationKey), color);
        }
    }

    private static void BroadcastFormattedMessage(string translationKey, Color color, params object[] args)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(Language.GetTextValue(translationKey, args), color);
            return;
        }

        if (Main.netMode == NetmodeID.Server)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromKey(translationKey, args), color);
        }
    }
}
