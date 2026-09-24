using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace O2ThornRain.Common.GlobalItems;

/// <summary>
/// Durability states for the umbrella item.
/// </summary>
public enum UmbrellaState
{
    New,
    Damaged,
    NearBroken,
    Broken
}

/// <summary>
/// Intercepts the vanilla Umbrella item to manage durability, UI indicators, and multiplayer sync.
/// </summary>
public class UmbrellaGlobalItem : GlobalItem
{
    public const int MaxDurability = 100;

    public int Durability = MaxDurability;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return entity.type == ItemID.Umbrella;
    }

    public override void SetDefaults(Item entity)
    {
        Durability = MaxDurability;
    }

    public UmbrellaState CurrentState => Durability switch
    {
        <= 0 => UmbrellaState.Broken,
        <= 14 => UmbrellaState.NearBroken,
        <= 49 => UmbrellaState.Damaged,
        _ => UmbrellaState.New
    };

    public bool IsBroken => Durability <= 0;

    public override GlobalItem Clone(Item from, Item to)
    {
        UmbrellaGlobalItem clone = (UmbrellaGlobalItem)base.Clone(from, to);
        clone.Durability = Durability;
        return clone;
    }

    public override void SaveData(Item item, TagCompound tag)
    {
        tag["durability"] = Durability;
    }

    public override void LoadData(Item item, TagCompound tag)
    {
        if (tag.ContainsKey("durability"))
        {
            Durability = tag.GetInt("durability");
        }
        else
        {
            Durability = MaxDurability;
        }
    }

    public override void NetSend(Item item, BinaryWriter writer)
    {
        writer.Write((byte)Math.Clamp(Durability, 0, MaxDurability));
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        Durability = reader.ReadByte();
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        string stateName = CurrentState switch
        {
            UmbrellaState.New => "Pristine",
            UmbrellaState.Damaged => "Damaged",
            UmbrellaState.NearBroken => "Near Broken",
            UmbrellaState.Broken => "Broken",
            _ => ""
        };

        Color stateColor = CurrentState switch
        {
            UmbrellaState.New => new Color(50, 205, 50),
            UmbrellaState.Damaged => new Color(255, 165, 0),
            UmbrellaState.NearBroken => new Color(220, 20, 60),
            UmbrellaState.Broken => new Color(130, 130, 130),
            _ => Color.White
        };

        tooltips.Add(new TooltipLine(Mod, "UmbrellaDurability", $"Durability against Thorns: {Durability}% ({stateName})")
        {
            OverrideColor = stateColor
        });

        if (IsBroken)
        {
            tooltips.Add(new TooltipLine(Mod, "UmbrellaBrokenWarning", "Broken: provides no protection against Thorn Rain.")
            {
                OverrideColor = new Color(180, 80, 80)
            });
        }
    }

    public override void PostDrawInInventory(
        Item item,
        SpriteBatch spriteBatch,
        Vector2 position,
        Rectangle frame,
        Color drawColor,
        Color itemColor,
        Vector2 origin,
        float scale)
    {
        float barWidth = 30f * Main.inventoryScale;
        float barHeight = 4f * Main.inventoryScale;

        Vector2 barTopLeft = position + new Vector2(-barWidth / 2f, 13f * Main.inventoryScale);
        float factor = Math.Clamp((float)Durability / MaxDurability, 0f, 1f);

        Color barColor = CurrentState switch
        {
            UmbrellaState.New => new Color(50, 205, 50),
            UmbrellaState.Damaged => new Color(255, 165, 0),
            UmbrellaState.NearBroken => new Color(220, 20, 60),
            UmbrellaState.Broken => new Color(90, 90, 90),
            _ => Color.White
        };

        Rectangle bgRect = new(
            (int)barTopLeft.X - 1,
            (int)barTopLeft.Y - 1,
            (int)barWidth + 2,
            (int)barHeight + 2
        );
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, bgRect, Color.Black * 0.75f);

        int fillWidth = (int)Math.Round(barWidth * factor);
        if (fillWidth > 0)
        {
            Rectangle fillRect = new(
                (int)barTopLeft.X,
                (int)barTopLeft.Y,
                fillWidth,
                (int)barHeight
            );
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, barColor);
        }
    }
}
