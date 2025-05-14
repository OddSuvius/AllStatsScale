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
        //switched from decimal multipliers to percent integers to avoid float precision errors
        [JsonIgnore]
        private int healthPercent;
        [JsonIgnore]
        private int damagePercent;
        [JsonIgnore]
        private int defensePercent;

        // Number of simulated players (integer)
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Tooltip")]
        [DefaultValue(1)]
        public int SimulatedPlayers
        {
            get => simulatedPlayers;
            set => simulatedPlayers = value <= 0 ? 1 : value;
        }

        // Health multiplier as percentage (text input)
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthMultiplier.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthMultiplier.Tooltip")]
        [DefaultValue(100)]
        [Range(1, 10000)] 
        public int HealthPercent
        {
            get => healthPercent;
            set => healthPercent = value < 1 ? 1 : value;
        }

        // Damage multiplier as percentage
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamageMultiplier.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamageMultiplier.Tooltip")]
        [DefaultValue(100)]
        [Range(1, 10000)]
        public int DamagePercent
        {
            get => damagePercent;
            set => damagePercent = value < 1 ? 1 : value;
        }

        // Defense multiplier as percentage
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefenseMultiplier.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefenseMultiplier.Tooltip")]
        [DefaultValue(100)]
        [Range(1, 10000)]
        public int DefensePercent
        {
            get => defensePercent;
            set => defensePercent = value < 1 ? 1 : value;
        }

        // Computed multipliers
        [JsonIgnore]
        public float HealthMultiplier => healthPercent * 0.01f;

        [JsonIgnore]
        public float DamageMultiplier => damagePercent * 0.01f;

        [JsonIgnore]
        public float DefenseMultiplier => defensePercent * 0.01f;
    }
}
