using Newtonsoft.Json;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AllStatsScaled
{
    public class AllStatsScaledConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [JsonIgnore]
        private int simulatedPlayers;
        [JsonIgnore]
        private int healthMultiplier;
        [JsonIgnore]
        private int damageMultiplier;
        [JsonIgnore]
        private int defenseMultiplier;


        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Tooltip")]
        [DefaultValue(1)]
        public int SimulatedPlayers
        {
            get { return simulatedPlayers; }
            set
            {
                if (value <= 0)
                {
                    simulatedPlayers = 1;
                }
                else
                {
                    simulatedPlayers = value;
                }
            }
        }


        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthMultiplier.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthMultiplier.Tooltip")]
        [DefaultValue(1)]
        public int HealthMultiplier
        {
            get { return healthMultiplier; }
            set
            {
                if (value <= 0)
                {
                    healthMultiplier = 1;
                }
                else
                {
                    healthMultiplier = value;
                }
            }
        }
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamageMultiplier.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamageMultiplier.Tooltip")]
        [DefaultValue(1)]
        public int DamageMultiplier
        {
            get { return damageMultiplier; }
            set
            {
                if (value <= 0)
                {
                    damageMultiplier = 1;
                }
                else
                {
                    damageMultiplier = value;
                }
            }
        }
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefenseMultiplier.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefenseMultiplier.Tooltip")]
        [DefaultValue(1)]
        public int DefenseMultiplier
        {
            get { return defenseMultiplier; }
            set
            {
                if (value <= 0)
                {
                    defenseMultiplier = 1;
                }
                else
                {
                    defenseMultiplier = value;
                }
            }
        }
    }
}
