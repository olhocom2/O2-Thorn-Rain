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
        // ─── MOEDAS ─────────────────────────────────────────────────
        // 1 Moeda de Platina e 50 Moedas de Ouro garantidas
        itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumCoin, 1, 1, 1));
        itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 50, 50));

        // ─── BARRAS DE LUMINITA ──────────────────────────────────────
        // 25 a 40 barras garantidas
        itemLoot.Add(ItemDropRule.Common(ItemID.LunarBar, 1, 25, 40));

        // ─── MONTARIA (DragonPower) – chance escalonada por dificuldade ─
        // For the Worthy / Legendary: 15% (1 em 7 aprox.)
        // Master Mode:                10% (1 em 10)
        // Expert Mode:                 5% (1 em 20)
        // (Modo Clássico não recebe bolsa, então não dropa aqui)

        // FTW tem prioridade: getGoodWorld sobrepõe masterMode
        // Usa um drop rule encadeado para FTW/Master/Expert
        // FTW/Legendary: ~14% | Master: 10% | Expert: 5%
        // Nota: cada Leading rule é avaliada independentemente.
        // FTW cobre também o masterMode, então a ordem importa (FTW primeiro).

        // FTW – condição customizada inline via DropBasedOnMasterMode workaround
        if (Main.getGoodWorld)
        {
            // Mundo FTW: 1 em 7 (~14.3%)
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonPower>(), 7));
        }
        else
        {
            var masterRule = new LeadingConditionRule(new Conditions.IsMasterMode());
            masterRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonPower>(), 10)); // 10%
            itemLoot.Add(masterRule);

            var expertRule = new LeadingConditionRule(new Conditions.IsExpert());
            expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonPower>(), 20)); // 5%
            itemLoot.Add(expertRule);
        }
    }
}
