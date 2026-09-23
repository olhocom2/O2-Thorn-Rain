using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Tiles;

namespace O2ThornRain.Content.Items;

/// <summary>
/// Item do troféu do boss Olho da Tempestade de Espinhos.
/// Pode ser colocado em paredes como decoração memorial de conquista.
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
