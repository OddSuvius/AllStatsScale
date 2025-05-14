using Terraria.ModLoader;

namespace AllStatsScale
{
    public class Player : ModPlayer
    {
        public override void PostUpdateRunSpeeds()
        {
            var cfg = ModContent.GetInstance<AllStatsScaledConfig>();
            if (cfg != null && cfg.PlayerSpeedMultiplier > 0)
            {
                Player.maxRunSpeed *= cfg.PlayerSpeedMultiplier;
                Player.accRunSpeed *= cfg.PlayerSpeedMultiplier;
            }
        }
    }
}