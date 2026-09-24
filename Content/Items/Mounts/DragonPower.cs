using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Mounts;

namespace O2ThornRain.Content.Items.Mounts;

/// <summary>
/// Mount summon item for the storm dragon, obtained from the boss bag.
/// Grants free flight and immunity to Thorn Rain spikes.
/// </summary>
public class DragonPower : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.value = Item.buyPrice(gold: 20);
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = SoundID.Item122;
        Item.noMelee = true;
        Item.scale = 1f;

        Item.mountType = ModContent.MountType<DragonMount>();
    }
}
