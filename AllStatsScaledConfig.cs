using Newtonsoft.Json;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AllStatsScaled
{
    public class AllStatsScaledConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [JsonIgnore]
        private bool enableSimulatedPlayers;
        [JsonIgnore]
        private int simulatedPlayers;
        [JsonIgnore]
        private int healthPercent;
        [JsonIgnore]
        private int damagePercent;
        [JsonIgnore]
        private int defensePercent;
        [JsonIgnore]
        private int playerSpeedPercent;

        // Number of simulated players (integer)
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Tooltip")]
        [DefaultValue(1)]
        public int SimulatedPlayers
        {
            get => simulatedPlayers;
            set => simulatedPlayers = value <= 0 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnableSimulatedPlayers.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnableSimulatedPlayers.Tooltip")]
        [DefaultValue(false)]
        public bool EnableSimulatedPlayers
        {
            get => enableSimulatedPlayers;
            set => enableSimulatedPlayers = value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        public int HealthPercent
        {
            get => healthPercent;
            set => healthPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamagePercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamagePercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        public int DamagePercent
        {
            get => damagePercent;
            set => damagePercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefensePercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefensePercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        public int DefensePercent
        {
            get => defensePercent;
            set => defensePercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerSpeedPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerSpeedPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        public int PlayerSpeedPercent
        {
            get => playerSpeedPercent;
            set => playerSpeedPercent = value < 1 ? 1 : value;
        }

        [JsonIgnore]
        public float HealthMultiplier => healthPercent * 0.01f;

        [JsonIgnore]
        public float DamageMultiplier => damagePercent * 0.01f;

        [JsonIgnore]
        public float DefenseMultiplier => defensePercent * 0.01f;

        [JsonIgnore]
        public float PlayerSpeedMultiplier => playerSpeedPercent * 0.01f;
    }
}
