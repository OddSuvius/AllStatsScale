using Terraria.ModLoader;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.Config;


namespace AllStatsScaled
{
    public class AllStatsScaled : Mod
    {
        public override void Load()
        {
            ILEdits.ApplyILEdits();
        }

        public override void Unload()
        {
            ILEdits.UnloadILEdits();
        }
    }
}