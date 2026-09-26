using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using O2ThornRain.Content.NPCs.Boss;
using O2ThornRain.Content.NPCs.Minions;
using O2ThornRain.Content.NPCs.Pillars;
using Terraria.Audio;

namespace O2ThornRain.Common.Systems;

/// <summary>
/// States for the "Storm of the Four" event.
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
/// Biomes hosting the four ancestral tornadoes.
/// </summary>
public enum TornadoBiome : byte
{
    Jungle,
    Snow,
    Desert,
    Corruption
}

/// <summary>
/// Manages the complete lifecycle of "The Storm of the Four" event.
/// Server-authoritative in multiplayer for spawning, tracking defeated pillars, and boss transition.
/// </summary>
public class ThornStormEventSystem : ModSystem
{
    private const int SummonCheckIntervalTicks = 300;
    private const float MiniTornadoSpawnChance = 0.15f;
    public const int EventMaxDurationTicks = 172800; // 2 in-game days (86,400 ticks * 2)

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
    private static int _eventDurationTimer;
    private static bool _announcedFinalStormWarning;

    public override void ClearWorld()
    {
        CurrentState = ThornStormEventState.Inactive;
        JungleDefeated = false;
        SnowDefeated = false;
        DesertDefeated = false;
        CorruptionDefeated = false;
        _checkTimer = 0;
        _finalStormTimer = 0;
        _eventDurationTimer = 0;
        _announcedFinalStormWarning = false;
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (CurrentState == ThornStormEventState.Inactive)
        {
            UpdateMiniTornadoSpawnCheck();
        }
        else if (CurrentState == ThornStormEventState.Active || CurrentState == ThornStormEventState.FinalStorm)
        {
            _eventDurationTimer++;
            if (_eventDurationTimer >= EventMaxDurationTicks)
            {
                ExpireEvent();
                return;
            }

            if (CurrentState == ThornStormEventState.FinalStorm)
            {
                UpdateFinalStorm();
            }
        }
    }

    private static void UpdateMiniTornadoSpawnCheck()
    {
        if (!Main.raining || SpikeRainSystem.CurrentIntensity != ThornRainIntensity.Storm)
            return;

        _checkTimer++;
        if (_checkTimer < SummonCheckIntervalTicks)
            return;

        _checkTimer = 0;

        if (Main.rand.NextFloat() > MiniTornadoSpawnChance)
            return;

        int summonNpcType = ModContent.NPCType<ThornStormSummonTornado>();
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.type == summonNpcType)
                return;
        }

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

    /// <summary>
    /// Starts the event after the summon tornado is defeated.
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
        _eventDurationTimer = 0;
        _finalStormTimer = 0;
        _announcedFinalStormWarning = false;

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
    /// Records defeat of one of the four biome tornadoes and triggers the final storm once all 4 fall.
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

    private static void UpdateFinalStorm()
    {
        int bossType = ModContent.NPCType<ThornStormBoss>();

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

        // 180 ticks (3 seconds): announce impending approach
        if (_finalStormTimer >= 180 && !_announcedFinalStormWarning)
        {
            _announcedFinalStormWarning = true;
            BroadcastEventMessage(
                "Mods.O2ThornRain.Events.FinalStormIncoming",
                new Color(255, 60, 60)
            );
        }

        // 360 ticks (6 seconds): spawn boss above target player
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
    /// Called when the final boss is defeated.
    /// </summary>
    public static void OnBossDefeated()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        CurrentState = ThornStormEventState.Completed;
        _eventDurationTimer = 0;

        BroadcastEventMessage(
            "Mods.O2ThornRain.Events.FinalStormVictory",
            new Color(120, 255, 160)
        );

        SyncEventState();
    }

    /// <summary>
    /// Called when the boss despawns, resetting the event state for a retry.
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
        _eventDurationTimer = 0;
        _finalStormTimer = 0;
        _announcedFinalStormWarning = false;

        SyncEventState();
    }

    /// <summary>
    /// Despawns all event entities and resets event state if the player fails to complete the event within 2 in-game days.
    /// </summary>
    public static void ExpireEvent()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        int pillarType = ModContent.NPCType<ThornBiomeTornado>();
        int summonType = ModContent.NPCType<ThornStormSummonTornado>();
        int bossType = ModContent.NPCType<ThornStormBoss>();
        int minionType = ModContent.NPCType<ThornMinionTornado>();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && (npc.type == pillarType || npc.type == summonType || npc.type == bossType || npc.type == minionType))
            {
                npc.active = false;
                npc.netSkip = -1;
                npc.life = 0;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }
            }
        }

        CurrentState = ThornStormEventState.Inactive;
        JungleDefeated = false;
        SnowDefeated = false;
        DesertDefeated = false;
        CorruptionDefeated = false;
        _eventDurationTimer = 0;
        _finalStormTimer = 0;
        _announcedFinalStormWarning = false;

        BroadcastEventMessage(
            "Mods.O2ThornRain.Events.EventExpired",
            new Color(130, 210, 240)
        );

        SyncEventState();
    }

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

        // Sample columns to find biome concentration
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
        // Find solid ground surface
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

        return new Vector2(bestX * 16f, groundY * 16f - 120f);
    }

    private static Vector2 FindOceanFloorPosition()
    {
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
