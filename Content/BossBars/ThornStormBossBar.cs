using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace O2ThornRain.Content.BossBars;

/// <summary>
/// Health bar displayed in the HUD for the Eye of the Thorn Storm boss.
/// </summary>
public class ThornStormBossBar : ModBossBar
{
    private int _bossHeadIndex = -1;

    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        if (_bossHeadIndex != -1)
        {
            return TextureAssets.NpcHeadBoss[_bossHeadIndex];
        }
        return null;
    }

    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        NPC npc = Main.npc[info.npcIndexToAimAt];
        if (!npc.active)
            return false;

        _bossHeadIndex = npc.GetBossHeadTextureIndex();
        life = npc.life;
        lifeMax = npc.lifeMax;

        return true;
    }
}
