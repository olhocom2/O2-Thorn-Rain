using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Mounts;

namespace O2ThornRain.Content.Buffs;

/// <summary>
/// Buff da montaria Poder do Dragão.
/// Concede a montaria e gerencia sua persistência.
/// </summary>
public class DragonMountBuff : ModBuff
{
    // Reutiliza o ícone do buff da montaria do Fishron do vanilla
    public override string Texture =>
        $"Terraria/Images/Buff_{BuffID.CuteFishronMount}";

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
