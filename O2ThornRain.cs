using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;

namespace O2ThornRain;

public enum MessageType : byte
{
    SyncThornRainIntensity,
    SyncThornStormEventState,
    RequestStartThornStormEvent
}

public class O2ThornRain : Mod
{
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        MessageType msgType = (MessageType)reader.ReadByte();

        switch (msgType)
        {
            case MessageType.SyncThornRainIntensity:
                byte intensity = reader.ReadByte();
                SpikeRainSystem.SetIntensityFromNet((ThornRainIntensity)intensity);
                break;

            case MessageType.SyncThornStormEventState:
                ThornStormEventState state = (ThornStormEventState)reader.ReadByte();
                bool jungle = reader.ReadBoolean();
                bool snow = reader.ReadBoolean();
                bool desert = reader.ReadBoolean();
                bool corruption = reader.ReadBoolean();
                ThornStormEventSystem.SetStateFromNet(state, jungle, snow, desert, corruption);
                break;

            case MessageType.RequestStartThornStormEvent:
                if (Main.netMode == NetmodeID.Server && ThornStormEventSystem.CurrentState == ThornStormEventState.Inactive)
                {
                    ThornStormEventSystem.StartEvent();
                }
                break;
        }
    }
}
