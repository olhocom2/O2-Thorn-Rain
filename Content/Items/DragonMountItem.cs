using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Mounts;

namespace O2ThornRain.Content.Items;

/// <summary>
/// Item de invocação da montaria do Dragão do Cultista, obtido exclusivamente como drop do boss final.
/// Invoca o dragão celestial que concede voo livre infinito e imunidade aos espinhos da Thorn Rain.
/// </summary>
public class DragonMountItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.value = Item.buyPrice(gold: 25);
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = SoundID.Item122;
        Item.noMelee = true;
        Item.scale = 1f;

        Item.mountType = ModContent.MountType<DragonMount>();
    }
}
