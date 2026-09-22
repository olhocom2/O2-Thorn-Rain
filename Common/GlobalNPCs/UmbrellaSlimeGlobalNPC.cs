using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace O2ThornRain.Common.GlobalNPCs;

/// <summary>
/// Adiciona uma chance de drop do Guarda-chuva vanilla (ItemID.Umbrella)
/// ao derrotar a Geleia com Guarda-chuva vanilla (NPCID.UmbrellaSlime).
/// </summary>
public class UmbrellaSlimeGlobalNPC : GlobalNPC
{
    // =========================================================
    // CONFIGURAÇÃO DE DROP
    // =========================================================

    // Chance de 5% de drop (1 em cada 20 slimes).
    private const float UmbrellaDropChance = 0.05f;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type == NPCID.UmbrellaSlime;
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (npc.type != NPCID.UmbrellaSlime)
            return;

        // Converte a probabilidade em denominador (ex: 0.05f => 20).
        int chanceDenominator = (int)Math.Round(1f / UmbrellaDropChance);

        // Adiciona o item vanilla ao loot sem modificar as demais regras existentes.
        // O sistema de ItemDropRule é processado no servidor em multiplayer, evitando duplicação.
        npcLoot.Add(ItemDropRule.Common(
            ItemID.Umbrella,
            chanceDenominator: chanceDenominator,
            minimumDropped: 1,
            maximumDropped: 1
        ));
    }
}
