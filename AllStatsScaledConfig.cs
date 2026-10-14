using System;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace AllStatsScale
{
    public class AllStatsScaledConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // Only the host (the Host & Play client) may change these settings in multiplayer. On a dedicated server
        // no client counts as the host, so the settings are changed in the server's config file instead.
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
        {
            if (NetMessage.DoesPlayerSlotCountAsAHost(whoAmI))
                return true;
            message = NetworkText.FromKey("tModLoader.ModConfigRejectChangesNotHost");
            return false;
        }

        // Settings are frozen per world session (WorldSettings), so a change made inside a world only applies once the world is
        // loaded again. Tell everyone in the world; on clients this runs when the server applies the host's change.
        public override void OnChanged()
        {
            if (Main.netMode != NetmodeID.Server && !Main.gameMenu)
                Main.NewText(Language.GetTextValue("Mods.AllStatsScaled.Configs.AllStatsScaledConfig.ChangesApplyOnWorldLoad"), 255, 213, 79);
        }

        [JsonIgnore]
        private bool enableSimulatedPlayers;
        [JsonIgnore]
        private int simulatedPlayers;
        [JsonIgnore]
        private int healthPercent;
        [JsonIgnore]
        private int damagePercent;
        [JsonIgnore]
        private int projectileDamagePercent;
        [JsonIgnore]
        private int defensePercent;
        [JsonIgnore]
        private int enemySpeedPercent;
        [JsonIgnore]
        private int enemyProjectileSpeedPercent;
        [JsonIgnore]
        private int enemyImmunityPercent;
        [JsonIgnore]
        private int playerMaxHealthPercent;
        [JsonIgnore]
        private int playerDefensePercent;
        [JsonIgnore]
        private int playerLifeRegenPercent;
        [JsonIgnore]
        private int playerImmunityPercent;
        [JsonIgnore]
        private int playerKnockbackPercent;
        [JsonIgnore]
        private int playerRespawnPercent;
        [JsonIgnore]
        private int playerBossRespawnPercent;
        [JsonIgnore]
        private bool respawnWhenBossFightEnds;
        [JsonIgnore]
        private int playerSpeedPercent;

        [Header("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.Headers.EnemyStats")]
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.HealthPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(120, 70, 100)]
        [SliderColor(255, 60, 60)]
        public int HealthPercent
        {
            get => healthPercent;
            set => healthPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefensePercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DefensePercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(120, 70, 100)]
        [SliderColor(170, 170, 170)]
        public int DefensePercent
        {
            get => defensePercent;
            set => defensePercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnemyImmunityPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnemyImmunityPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(120, 70, 100)]
        [SliderColor(255, 240, 150)]
        public int EnemyImmunityPercent
        {
            get => enemyImmunityPercent;
            set => enemyImmunityPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamagePercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.DamagePercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(120, 70, 100)]
        [SliderColor(255, 140, 40)]
        public int DamagePercent
        {
            get => damagePercent;
            set => damagePercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnemySpeedPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnemySpeedPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(120, 70, 100)]
        [SliderColor(90, 220, 90)]
        public int EnemySpeedPercent
        {
            get => enemySpeedPercent;
            set => enemySpeedPercent = value < 1 ? 1 : value;
        }

        [Header("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.Headers.HostileProjectiles")]
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.ProjectileDamagePercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.ProjectileDamagePercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(125, 85, 70)]
        [SliderColor(255, 220, 60)]
        public int ProjectileDamagePercent
        {
            get => projectileDamagePercent;
            set => projectileDamagePercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnemyProjectileSpeedPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnemyProjectileSpeedPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(125, 85, 70)]
        [SliderColor(80, 220, 220)]
        public int EnemyProjectileSpeedPercent
        {
            get => enemyProjectileSpeedPercent;
            set => enemyProjectileSpeedPercent = value < 1 ? 1 : value;
        }

        [Header("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.Headers.BossesAndRespawn")]
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnableSimulatedPlayers.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.EnableSimulatedPlayers.Tooltip")]
        [DefaultValue(false)]
        [BackgroundColor(95, 75, 150)]
        public bool EnableSimulatedPlayers
        {
            get => enableSimulatedPlayers;
            set => enableSimulatedPlayers = value;
        }

        // Capped at 255 (Terraria's player limit): vanilla sends the count to clients as a byte, and
        // NPC.GetStatScalingFactors loops once per player, so a huge value would freeze every NPC spawn.
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.SimulatedPlayers.Tooltip")]
        [DefaultValue(1)]
        [Range(1, 255)]
        [BackgroundColor(95, 75, 150)]
        public int SimulatedPlayers
        {
            get => simulatedPlayers;
            set => simulatedPlayers = Math.Clamp(value, 1, 255);
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerRespawnPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerRespawnPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(95, 75, 150)]
        [SliderColor(170, 120, 255)]
        public int PlayerRespawnPercent
        {
            get => playerRespawnPercent;
            set => playerRespawnPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerBossRespawnPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerBossRespawnPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(95, 75, 150)]
        [SliderColor(170, 120, 255)]
        public int PlayerBossRespawnPercent
        {
            get => playerBossRespawnPercent;
            set => playerBossRespawnPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.RespawnWhenBossFightEnds.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.RespawnWhenBossFightEnds.Tooltip")]
        [DefaultValue(true)]
        [BackgroundColor(95, 75, 150)]
        public bool RespawnWhenBossFightEnds
        {
            get => respawnWhenBossFightEnds;
            set => respawnWhenBossFightEnds = value;
        }

        [Header("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.Headers.Player")]
        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerMaxHealthPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerMaxHealthPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(60, 105, 140)]
        [SliderColor(255, 60, 60)]
        public int PlayerMaxHealthPercent
        {
            get => playerMaxHealthPercent;
            set => playerMaxHealthPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerDefensePercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerDefensePercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(60, 105, 140)]
        [SliderColor(170, 170, 170)]
        public int PlayerDefensePercent
        {
            get => playerDefensePercent;
            set => playerDefensePercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerLifeRegenPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerLifeRegenPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(60, 105, 140)]
        [SliderColor(255, 120, 180)]
        public int PlayerLifeRegenPercent
        {
            get => playerLifeRegenPercent;
            set => playerLifeRegenPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerImmunityPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerImmunityPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(60, 105, 140)]
        [SliderColor(255, 240, 150)]
        public int PlayerImmunityPercent
        {
            get => playerImmunityPercent;
            set => playerImmunityPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerKnockbackPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerKnockbackPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(60, 105, 140)]
        [SliderColor(200, 140, 80)]
        public int PlayerKnockbackPercent
        {
            get => playerKnockbackPercent;
            set => playerKnockbackPercent = value < 1 ? 1 : value;
        }

        [LabelKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerSpeedPercent.Label"), TooltipKey("$Mods.AllStatsScaled.Configs.AllStatsScaledConfig.PlayerSpeedPercent.Tooltip")]
        [DefaultValue(100)]
        [Range(1, int.MaxValue)]
        [CustomModConfigItem(typeof(PercentElement))]
        [BackgroundColor(60, 105, 140)]
        [SliderColor(80, 160, 255)]
        public int PlayerSpeedPercent
        {
            get => playerSpeedPercent;
            set => playerSpeedPercent = value < 1 ? 1 : value;
        }

        [JsonIgnore]
        public float PlayerSpeedMultiplier => playerSpeedPercent * 0.01f;
    }

    // The settings the current world runs with. Frozen when the world loads (single player, or the server) and sent to every
    // client with the world data, which arrives before any NPC, so a player who joins late computes enemy stats exactly like
    // the server did. Changes made during the session reach the live config but only apply the next time the world loads.
    public class WorldSettings : ModSystem
    {
        private static AllStatsScaledConfig frozen;

        // Outside a world (menus, mod loading) this is the live config, which is null while mods are still loading.
        public static AllStatsScaledConfig Current => frozen ?? ModContent.GetInstance<AllStatsScaledConfig>();

        public override void OnWorldLoad()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) // clients get the server's copy in NetReceive
                frozen = Copy();
        }

        public override void OnWorldUnload() => frozen = null;

        public override void NetSend(BinaryWriter writer) => writer.Write(JsonConvert.SerializeObject(Current, ConfigManager.serializerSettings));

        public override void NetReceive(BinaryReader reader)
        {
            var copy = Copy();
            JsonConvert.PopulateObject(reader.ReadString(), copy, ConfigManager.serializerSettings);
            frozen = copy;
        }

        private static AllStatsScaledConfig Copy() => (AllStatsScaledConfig)ConfigManager.GeneratePopulatedClone(ModContent.GetInstance<AllStatsScaledConfig>());
    }
}
