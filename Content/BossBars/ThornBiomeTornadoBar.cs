using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using O2ThornRain.Content.NPCs.Pillars;

namespace O2ThornRain.Content.BossBars;

/// <summary>
/// Boss health bar displayed in the HUD for biome pillars (ThornBiomeTornado).
/// </summary>
public class ThornBiomeTornadoBar : ModBossBar
{
    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        return null;
    }

    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        NPC npc = Main.npc[info.npcIndexToAimAt];
        if (!npc.active || npc.type != ModContent.NPCType<ThornBiomeTornado>())
            return false;

        life    = npc.life;
        lifeMax = npc.lifeMax;

        return true;
    }
}
