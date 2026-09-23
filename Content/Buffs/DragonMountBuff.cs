using Terraria;
using Terraria.ModLoader;
using O2ThornRain.Content.Mounts;

namespace O2ThornRain.Content.Buffs;

/// <summary>
/// Buff da montaria Poder do Dragão.
/// Concede a montaria do dragão celestial e gerencia sua persistência ativa.
/// </summary>
public class DragonMountBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.mount.SetMount(ModContent.MountType<DragonMount>(), player);
        player.buffTime[buffIndex] = 10;
    }
}
