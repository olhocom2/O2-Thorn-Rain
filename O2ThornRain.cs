using System.IO;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;

namespace O2ThornRain;

public enum MessageType : byte
{
    SyncThornRainIntensity
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
        }
    }
}
