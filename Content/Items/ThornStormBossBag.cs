using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace O2ThornRain.Content.Items;

/// <summary>
/// Bolsa de tesouro do boss Olho da Tempestade de Espinhos.
/// Dropada ao derrotar o boss final. Ao ser aberta com o botão direito,
/// concede moedas de platina e ouro, além dos itens e montarias lendárias.
/// </summary>
public class ThornStormBossBag : ModItem
{
    public override void SetStaticDefaults()
    {
        ItemID.Sets.BossBag[Type] = true;
        ItemID.Sets.PreHardmodeLikeBossBag[Type] = false;
        Item.ResearchUnlockCount = 3;
    }

    public override void SetDefaults()
    {
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.width = 24;
        Item.height = 24;
        Item.rare = ItemRarityID.Expert;
        Item.expert = true;
    }

    public override bool CanRightClick()
    {
        return true;
    }

    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        // 1 Moeda de Platina e 50 Moedas de Ouro garantidas
        itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumCoin, 1, 1, 1));
        itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 50, 50));

        // Adaga de Espinhos (100% garantida ao abrir a bolsa)
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonPower>()));

        // Troféu do Boss (100% garantido ao abrir a bolsa)
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ThornStormBossTrophy>()));
    }
}
