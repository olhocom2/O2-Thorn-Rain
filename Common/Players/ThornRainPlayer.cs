using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using O2ThornRain.Common.GlobalItems;
using O2ThornRain.Common.Systems;
using O2ThornRain.Content.Mounts;
using O2ThornRain.Content.Projectiles;

namespace O2ThornRain.Common.Players;

/// <summary>
/// Manages player protections against Thorn Rain (Ironskin, Umbrella, Dragon Mount)
/// and umbrella durability consumption with multiplayer synchronization.
/// </summary>
public class ThornRainPlayer : ModPlayer
{
    // Exposure duration per 1 point durability loss (30 ticks = 0.5s)
    public const int ExposureTicksPerDurabilityLoss = 30;

    private int _durabilityTimer;

    /// <summary>
    /// Priority 1: Active Ironskin potion buff grants full immunity to thorn spikes.
    /// </summary>
    public bool HasIronskinProtection => Player.HasBuff(BuffID.Ironskin);

    /// <summary>
    /// Priority 2: Held open umbrella with durability > 0.
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
    /// Priority 3: Dragon Mount grants immunity against SpikesProjectile.
    /// </summary>
    public bool HasDragonMountProtection =>
        Player.mount != null && Player.mount.Active && Player.mount.Type == ModContent.MountType<DragonMount>();

    /// <summary>
    /// Returns true if the player has any active protection against spikes.
    /// </summary>
    public bool IsProtectedFromSpikes => HasIronskinProtection || HasUmbrellaProtection || HasDragonMountProtection;

    public override void PostUpdate()
    {
        UpdateUmbrellaDurability();
    }

    private void UpdateUmbrellaDurability()
    {
        // Durability loss is authoritative on the owning client
        if (Player.whoAmI != Main.myPlayer)
            return;

        if (!Player.active || Player.dead)
        {
            _durabilityTimer = 0;
            return;
        }

        // Priority 1: Ironskin active prevents umbrella wear
        if (HasIronskinProtection)
        {
            _durabilityTimer = 0;
            return;
        }

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

        if (umbrella.Durability <= 0)
        {
            _durabilityTimer = 0;
            return;
        }

        bool exposedToRain = SpikeRainSystem.IsPlayerInRainZone(Player);
        bool spikesThreatening = SpikesProjectile.ActiveCount > 0;

        if (!exposedToRain || !spikesThreatening)
        {
            _durabilityTimer = 0;
            return;
        }

        _durabilityTimer++;

        if (_durabilityTimer >= ExposureTicksPerDurabilityLoss)
        {
            _durabilityTimer = 0;
            umbrella.Durability--;

            if (umbrella.Durability < 0)
            {
                umbrella.Durability = 0;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Player.whoAmI, Player.selectedItem);
            }
        }
    }
}
