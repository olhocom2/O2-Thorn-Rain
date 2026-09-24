using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.Systems;

namespace O2ThornRain.Content.Items.Consumables;

/// <summary>
/// Summon item for "The Storm of Four" event.
/// Summons the ancestral four winds and awakens tornadoes across the world.
/// </summary>
public class LightningRod : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 80;
        Item.useTime = 30;
        Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.consumable = true;
        Item.maxStack = 9999;
        Item.value = Item.buyPrice(gold: 5);
        Item.rare = ItemRarityID.Lime;
        Item.scale = 0.5f;

        Item.UseSound = SoundID.Roar with
        {
            Volume = 0.9f,
            Pitch = -0.3f
        };
    }

    public override bool CanUseItem(Player player)
    {
        return ThornStormEventSystem.CurrentState == ThornStormEventState.Inactive;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            SoundEngine.PlaySound(
                SoundID.Item122 with
                {
                    Volume = 1f,
                    Pitch = -0.2f
                },
                player.Center
            );

            for (int i = 0; i < 30; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(6f, 6f);
                Dust dust = Dust.NewDustPerfect(
                    player.Center + new Vector2(0f, -20f),
                    DustID.CrimsonTorch,
                    dustVel,
                    100,
                    default,
                    Main.rand.NextFloat(1.3f, 2.0f)
                );
                dust.noGravity = true;
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                ThornStormEventSystem.StartEvent();
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MessageType.RequestStartThornStormEvent);
                packet.Send();
            }
        }

        return true;
    }

    public override void AddRecipes()
    {
        CreateRecipe(5)
            .AddIngredient(ItemID.DirtBlock, 1)
            .Register();
    }
}
