using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using O2ThornRain.Common.GlobalItems;
using O2ThornRain.Common.Systems;
using O2ThornRain.Content.Items;
using O2ThornRain.Content.Mounts;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Players;

/// <summary>
/// Gerencia as proteções do jogador contra a Chuva de Espinhos (Pele de Ferro e Guarda-chuva)
/// e o consumo gradual de durabilidade do Guarda-chuva com sincronização multiplayer.
/// </summary>
public class ThornRainPlayer : ModPlayer
{
    // =========================================================
    // CONSTANTES DE BALANCEAMENTO
    // =========================================================

    // Intervalo de ticks para perda de 1 ponto de durabilidade.
    // 60 ticks = 1 segundo.
    // 30 ticks = 0,5 segundo (100 pontos = 50 segundos de exposição contínua).
    public const int ExposureTicksPerDurabilityLoss = 30;

    private int _durabilityTimer;

    // =========================================================
    // PRIORIDADE DE PROTEÇÃO
    // =========================================================

    /// <summary>
    /// Prioridade 1: Pele de Ferro ativa concede imunidade total aos espinhos.
    /// </summary>
    public bool HasIronskinProtection => Player.HasBuff(BuffID.Ironskin);

    /// <summary>
    /// Prioridade 2: Guarda-chuva empunhado/aberto com durabilidade > 0.
    /// </summary>
    public bool HasUmbrellaProtection
    {
        get
        {
            if (!Player.active || Player.dead)
                return false;

            Item heldItem = Player.HeldItem;
            if (heldItem == null || heldItem.IsAir || heldItem.type != ItemID.Umbrella)
                return false;

            if (heldItem.TryGetGlobalItem(out UmbrellaGlobalItem umbrella))
            {
                return umbrella.Durability > 0;
            }

            return false;
        }
    }

    /// <summary>
    /// Prioridade 3: Montaria "Poder do Dragão" ativa concede imunidade permanente contra SpikesProjectile.
    /// </summary>
    public bool HasDragonMountProtection =>
        Player.mount != null && Player.mount.Active && Player.mount.Type == ModContent.MountType<DragonMount>();

    /// <summary>
    /// Indica se o jogador possui qualquer proteção ativa contra os espinhos.
    /// </summary>
    public bool IsProtectedFromSpikes => HasIronskinProtection || HasUmbrellaProtection || HasDragonMountProtection;

    // =========================================================
    // ITENS INICIAIS E PERSISTÊNCIA
    // =========================================================

    public bool HasReceivedBonusItems;

    public override void SaveData(TagCompound tag)
    {
        tag["HasReceivedBonusItems"] = HasReceivedBonusItems;
    }

    public override void LoadData(TagCompound tag)
    {
        HasReceivedBonusItems = tag.GetBool("HasReceivedBonusItems");
    }

    public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
    {
        yield return new Item(ItemID.FishronWings);
        yield return new Item(ItemID.Zenith);
        yield return new Item(ModContent.ItemType<LightningRod>(), 20);
    }

    public override void OnEnterWorld()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;

        if (!HasReceivedBonusItems)
        {
            if (!HasItemInInventoryOrArmor(ItemID.FishronWings))
            {
                GiveItemToPlayer(ItemID.FishronWings);
            }

            if (!HasItemInInventoryOrArmor(ItemID.Zenith))
            {
                GiveItemToPlayer(ItemID.Zenith);
            }

            HasReceivedBonusItems = true;
        }

        // Facilidade de teste: se o jogador não tiver nenhum LightningRod no inventário, concede 20 unidades
        if (!Player.HasItem(ModContent.ItemType<LightningRod>()))
        {
            Player.QuickSpawnItem(Player.GetSource_Misc("TestSetup"), ModContent.ItemType<LightningRod>(), 20);
            Main.NewText(
                "⚡ [O2ThornRain] Foram adicionados 20x Para-raios da Tempestade ao seu inventário para facilitar os testes!",
                255, 215, 0
            );
        }
    }

    private bool HasItemInInventoryOrArmor(int itemType)
    {
        if (Player.HasItem(itemType))
            return true;

        if (Player.armor != null)
        {
            for (int i = 0; i < Player.armor.Length; i++)
            {
                if (Player.armor[i] != null && Player.armor[i].type == itemType)
                    return true;
            }
        }

        return false;
    }

    private void GiveItemToPlayer(int itemType)
    {
        // Procura o primeiro slot vazio no inventário principal (slots 0 a 49)
        for (int i = 0; i < 50; i++)
        {
            if (Player.inventory[i] == null || Player.inventory[i].IsAir)
            {
                Player.inventory[i] = new Item();
                Player.inventory[i].SetDefaults(itemType);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Player.whoAmI, i);
                }
                return;
            }
        }

        // Se o inventário estiver cheio, gera o item no chão próximo ao jogador de forma segura
        Player.QuickSpawnItem(Player.GetSource_Misc("StarterBonus"), itemType);
    }

    // =========================================================
    // ATUALIZAÇÃO DO JOGADOR
    // =========================================================

    public override void PostUpdate()
    {
        UpdateUmbrellaDurability();
    }

    private void UpdateUmbrellaDurability()
    {
        // A autoridade sobre o desgaste do item empunhado é do cliente proprietário do jogador.
        if (Player.whoAmI != Main.myPlayer)
            return;

        if (!Player.active || Player.dead)
        {
            _durabilityTimer = 0;
            return;
        }

        // Se a Pele de Ferro estiver ativa (prioridade 1), o jogador não desgasta o guarda-chuva.
        if (HasIronskinProtection)
        {
            _durabilityTimer = 0;
            return;
        }

        // Verifica se o jogador está segurando o Guarda-chuva.
        Item heldItem = Player.HeldItem;
        if (heldItem == null || heldItem.IsAir || heldItem.type != ItemID.Umbrella)
        {
            _durabilityTimer = 0;
            return;
        }

        if (!heldItem.TryGetGlobalItem(out UmbrellaGlobalItem umbrella))
        {
            _durabilityTimer = 0;
            return;
        }

        // Se o item já estiver quebrado, nada a perder.
        if (umbrella.Durability <= 0)
        {
            _durabilityTimer = 0;
            return;
        }

        // Verifica se o jogador está exposto à zona de chuva e se há espinhos ativos ameaçando.
        bool exposedToRain = SpikeRainSystem.IsPlayerInRainZone(Player);
        bool spikesThreatening = SpikesProjectile.ActiveCount > 0;

        if (!exposedToRain || !spikesThreatening)
        {
            // Fora da chuva ou sem espinhos ameaçando: não perde durabilidade.
            _durabilityTimer = 0;
            return;
        }

        // Incrementa o tempo de exposição contínua.
        _durabilityTimer++;

        if (_durabilityTimer >= ExposureTicksPerDurabilityLoss)
        {
            _durabilityTimer = 0;
            umbrella.Durability--;

            if (umbrella.Durability < 0)
            {
                umbrella.Durability = 0;
            }

            // Sincroniza a atualização do item com o servidor no multiplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Player.whoAmI, Player.selectedItem);
            }
        }
    }
}
