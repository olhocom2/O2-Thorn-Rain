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
/// Estados conceituais do Guarda-chuva baseados em sua durabilidade atual.
/// </summary>
public enum UmbrellaState
{
    Novo,           // 100% até 50%
    Danificado,     // 49% até 15%
    QuaseQuebrado,  // 14% até 1%
    Quebrado        // 0%
}

/// <summary>
/// Intercepta o item vanilla Umbrella (ItemID.Umbrella), atribuindo-lhe durabilidade,
/// estados visuais, barra de durabilidade e sincronização em multiplayer.
/// </summary>
public class UmbrellaGlobalItem : GlobalItem
{
    // =========================================================
    // CONSTANTES E ESTADO
    // =========================================================

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

    // =========================================================
    // ESTADOS
    // =========================================================

    public UmbrellaState CurrentState => Durability switch
    {
        <= 0 => UmbrellaState.Quebrado,
        <= 14 => UmbrellaState.QuaseQuebrado,
        <= 49 => UmbrellaState.Danificado,
        _ => UmbrellaState.Novo
    };

    public bool IsBroken => Durability <= 0;

    // =========================================================
    // CLONAGEM E PERSISTÊNCIA
    // =========================================================

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

    // =========================================================
    // SINCRONIZAÇÃO MULTIPLAYER
    // =========================================================

    public override void NetSend(Item item, BinaryWriter writer)
    {
        writer.Write((byte)Math.Clamp(Durability, 0, MaxDurability));
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        Durability = reader.ReadByte();
    }

    // =========================================================
    // TOOLTIPS
    // =========================================================

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        string stateName = CurrentState switch
        {
            UmbrellaState.Novo => "Novo",
            UmbrellaState.Danificado => "Danificado",
            UmbrellaState.QuaseQuebrado => "Quase Quebrado",
            UmbrellaState.Quebrado => "Quebrado",
            _ => ""
        };

        Color stateColor = CurrentState switch
        {
            UmbrellaState.Novo => new Color(50, 205, 50),       // Verde
            UmbrellaState.Danificado => new Color(255, 165, 0),   // Laranja
            UmbrellaState.QuaseQuebrado => new Color(220, 20, 60),// Vermelho
            UmbrellaState.Quebrado => new Color(130, 130, 130),   // Cinza
            _ => Color.White
        };

        tooltips.Add(new TooltipLine(Mod, "UmbrellaDurability", $"Durabilidade contra Espinhos: {Durability}% ({stateName})")
        {
            OverrideColor = stateColor
        });

        if (IsBroken)
        {
            tooltips.Add(new TooltipLine(Mod, "UmbrellaBrokenWarning", "Quebrado: não oferece proteção contra a Chuva de Espinhos.")
            {
                OverrideColor = new Color(180, 80, 80)
            });
        }
    }

    // =========================================================
    // BARRA DE DURABILIDADE NO INVENTÁRIO
    // =========================================================

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
        // Largura e altura da barra proporcionais à escala do slot do inventário.
        float barWidth = 30f * Main.inventoryScale;
        float barHeight = 4f * Main.inventoryScale;

        // Posição ligeiramente abaixo do centro do item dentro do slot.
        Vector2 barTopLeft = position + new Vector2(-barWidth / 2f, 13f * Main.inventoryScale);

        float factor = Math.Clamp((float)Durability / MaxDurability, 0f, 1f);

        Color barColor = CurrentState switch
        {
            UmbrellaState.Novo => new Color(50, 205, 50),       // Verde
            UmbrellaState.Danificado => new Color(255, 165, 0),   // Laranja
            UmbrellaState.QuaseQuebrado => new Color(220, 20, 60),// Vermelho
            UmbrellaState.Quebrado => new Color(90, 90, 90),      // Cinza escuro
            _ => Color.White
        };

        // Fundo / Borda preta
        Rectangle bgRect = new(
            (int)barTopLeft.X - 1,
            (int)barTopLeft.Y - 1,
            (int)barWidth + 2,
            (int)barHeight + 2
        );
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, bgRect, Color.Black * 0.75f);

        // Preenchimento da barra
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
