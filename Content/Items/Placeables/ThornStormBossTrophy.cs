using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Tiles;

namespace O2ThornRain.Content.Items.Placeables;

/// <summary>
/// Wall trophy item for defeating the Eye of the Thorn Storm boss.
/// </summary>
public class ThornStormBossTrophy : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<ThornStormBossTrophyTile>());

        Item.width = 32;
        Item.height = 32;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 1);
    }
}
