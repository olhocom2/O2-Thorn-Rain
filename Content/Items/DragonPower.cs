using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Mounts;

namespace O2ThornRain.Content.Items;

/// <summary>
/// Item de invocação da montaria "Poder do Dragão", obtido ao derrotar o Boss final.
/// Invoca o dragão da tempestade que concede voo livre e imunidade aos espinhos da Thorn Rain.
/// </summary>
public class DragonPower : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 46;
        Item.height = 46;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.value = Item.buyPrice(gold: 20);
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = SoundID.Item122;
        Item.noMelee = true;
        Item.scale = 0.4f;

        Item.mountType = ModContent.MountType<DragonMount>();
    }
}
