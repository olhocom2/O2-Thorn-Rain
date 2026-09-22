using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.GlobalItems;
using O2ThornRain.Common.Systems;
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
    /// Indica se o jogador possui qualquer proteção ativa contra os espinhos.
    /// </summary>
    public bool IsProtectedFromSpikes => HasIronskinProtection || HasUmbrellaProtection;

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
