using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.GameContent.Creative;

namespace AllStatsScaled
{
    public class AllStatsScaledModSystem : ModSystem
    {
        public static NPCSpawnParams spawnparams;
        public static GameModeData gameModeData;
        public static float? strengthOverride;

        public AllStatsScaledModSystem()
        {
            spawnparams = default;
            gameModeData = spawnparams.gameModeData ?? Main.GameModeInfo;
            strengthOverride = spawnparams.strengthMultiplierOverride;
        }

        public override void Load()
        {
            Terraria.On_NPC.ScaleStats += ModifyScaledStats;
        }

        private void ModifyScaledStats(On_NPC.orig_ScaleStats orig, NPC npc, int? activePlayersCount, GameModeData gameModeData, float? strengthOverride)
        {
            {
                if ((!NPCID.Sets.NeedsExpertScaling.IndexInRange(npc.type) || !NPCID.Sets.NeedsExpertScaling[npc.type]) && (npc.lifeMax <= 5 || npc.damage == 0 || npc.friendly || npc.townNPC))
                {
                    return;
                }
                float num = 1f;
                if (strengthOverride.HasValue)
                {
                    num = strengthOverride.Value;
                }
                else if (gameModeData.IsJourneyMode)
                {
                    CreativePowers.DifficultySliderPower power = CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>();
                    if (power != null && power.GetIsUnlocked())
                    {
                        num = power.StrengthMultiplierToGiveNPCs;
                    }
                }
                float num2 = num;
                if (gameModeData.IsJourneyMode && Main.getGoodWorld)
                {
                    num += 1f;
                }
                NPCStrengthHelper nPCStrengthHelper = new NPCStrengthHelper(gameModeData, num, Main.getGoodWorld);
                if (nPCStrengthHelper.IsExpertMode)
                {
                    (Mod as AllStatsScaled).ScaledStats_ApplyExpertTweaks(npc);
                }
                (Mod as AllStatsScaled).ScaledStats_ApplyGameMode(gameModeData, npc);
                if (Main.getGoodWorld && nPCStrengthHelper.ExtraDamageForGetGoodWorld)
                {
                    npc.damage += npc.damage / 3;
                }
                if (nPCStrengthHelper.IsExpertMode)
                {
                    int num3 = (npc.statsAreScaledForThisManyPlayers = (ModContent.GetInstance<AllStatsScaledConfig>().SimulatedPlayers <= 0) ? NPC.GetActivePlayerCount(): ModContent.GetInstance<AllStatsScaledConfig>().SimulatedPlayers);
                    NPC.GetStatScalingFactors(num3, out var balance, out var boost);

                    float bossAdjustment = 1f;
                    if (nPCStrengthHelper.IsMasterMode)
                    {
                        bossAdjustment = 0.85f;
                    }
                    (Mod as AllStatsScaled).ScaledStats_ApplyMultiplayerStats(num3, balance, boost, bossAdjustment, npc);
                }
                npc.ScaleStats_UseStrengthMultiplier(num);
                npc.strengthMultiplier = num2;
                if ((npc.type < NPCID.None || !NPCID.Sets.ProjectileNPC[npc.type]) && npc.lifeMax < 6)
                {
                    npc.lifeMax = 6;
                }
                npc.lifeMax *= ModContent.GetInstance<AllStatsScaledConfig>().HealthMultiplier;
                npc.damage *= ModContent.GetInstance<AllStatsScaledConfig>().DamageMultiplier;
                npc.defense *= ModContent.GetInstance<AllStatsScaledConfig>().DefenseMultiplier;
                npc.life = npc.lifeMax;
                npc.defDamage = npc.damage;
                npc.defDefense = npc.defense;
            }
        }
    }
}