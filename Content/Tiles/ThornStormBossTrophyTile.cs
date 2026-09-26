using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace O2ThornRain.Content.Tiles;

/// <summary>
/// Tile de parede 3x3 do troféu do boss Olho da Tempestade de Espinhos.
/// Pode ser fixado em paredes de fundo, assim como os troféus de bosses do Terraria vanilla.
/// </summary>
public class ThornStormBossTrophyTile : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.FramesOnKillWall[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.addTile(Type);

        AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Trophy"));
        DustType = DustID.WoodFurniture;
    }
}
