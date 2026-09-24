using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Content.Items.Mounts;

namespace O2ThornRain.Content.Items.Consumables;

/// <summary>
/// Treasure bag for the Eye of the Thorn Storm boss.
/// Dropped upon defeating the boss in Expert/Master/Journey (power 5+) modes.
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
        // Guaranteed currencies: 1 Platinum Coin + 50 Gold Coins
        itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumCoin, 1, 1, 1));
        itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 50, 50));

        // Guaranteed Luminite bars: 25 to 40
        itemLoot.Add(ItemDropRule.Common(ItemID.LunarBar, 1, 25, 40));

        // DragonPower mount summon item with scaled drop rates:
        // - For the Worthy / Legendary: ~14.3% (1 in 7)
        // - Master Mode (or Journey slider >= 3x): 10% (1 in 10)
        // - Expert Mode / Journey Mode / Normal: 5% (1 in 20)
        LeadingConditionRule ftwRule = new(new FTWMountDropCondition());
        ftwRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonPower>(), 7));
        itemLoot.Add(ftwRule);

        LeadingConditionRule masterRule = new(new MasterMountDropCondition());
        masterRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonPower>(), 10));
        itemLoot.Add(masterRule);

        LeadingConditionRule defaultRule = new(new DefaultMountDropCondition());
        defaultRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DragonPower>(), 20));
        itemLoot.Add(defaultRule);
    }
}

public class FTWMountDropCondition : IItemDropRuleCondition
{
    public bool CanDrop(DropAttemptInfo info) => Main.getGoodWorld;
    public bool CanShowItemDropInUI() => true;
    public string GetConditionDescription() => null;
}

public class MasterMountDropCondition : IItemDropRuleCondition
{
    public bool CanDrop(DropAttemptInfo info) => !Main.getGoodWorld && (Main.masterMode || (Main.GameModeInfo.IsJourneyMode && Main.GameModeInfo.EnemyDamageMultiplier >= 3f));
    public bool CanShowItemDropInUI() => true;
    public string GetConditionDescription() => null;
}

public class DefaultMountDropCondition : IItemDropRuleCondition
{
    public bool CanDrop(DropAttemptInfo info) => !Main.getGoodWorld && !(Main.masterMode || (Main.GameModeInfo.IsJourneyMode && Main.GameModeInfo.EnemyDamageMultiplier >= 3f));
    public bool CanShowItemDropInUI() => true;
    public string GetConditionDescription() => null;
}
