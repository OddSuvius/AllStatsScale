using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AllStatsScale
{
    // Edits NPC.ScaleStats, which runs once per NPC inside NPC.SetDefaults on every machine
    // (server, and each client when it receives the NPC), so all machines compute the same stats from the synced server-side config.
    public class ScaleStatsEdit : ModSystem
    {
        public override void Load()
        {
            IL_NPC.ScaleStats += PatchScaleStats; // tModLoader removes IL hooks on unload
        }

        private void PatchScaleStats(ILContext il)
        {
            var c = new ILCursor(il);

            // Part 1, C# 13195 (expert/master only):
            //   num3 = (statsAreScaledForThisManyPlayers = (activePlayersCount.HasValue ? activePlayersCount.Value : GetActivePlayerCount()));
            // Both ternary branches meet at the dup (IL_0108) with the player count on the stack. Replace it there, so the
            // field (sent to clients, used by GetMyBalance) and num3 (used for the scaling) both get the simulated count.
            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchCall<NPC>(nameof(NPC.GetActivePlayerCount)),
                    i => i.MatchDup()))
                throw new ILPatchFailureException(Mod, il, null);
            c.Index--;           // before the dup
            c.MoveAfterLabels(); // the HasValue branch jumps to the dup; it must run our code too
            c.EmitDelegate(static (int playerCount) =>
            {
                var cfg = WorldSettings.Current;
                return cfg != null && cfg.EnableSimulatedPlayers ? cfg.SimulatedPlayers : playerCount;
            });

            // Part 2, C# 13212: defDefense = defense;  (last statement, IL_0198), then the final ret (IL_019d).
            // Search for the ret instead of requiring it to be adjacent, so code another mod appends there doesn't break the match.
            // No MoveAfterLabels: the early ret at IL_0041 (critters, town NPCs, lifeMax <= 5, damage == 0) stays unscaled, like vanilla's expert scaling.
            if (!c.TryGotoNext(MoveType.After, i => i.MatchStfld<NPC>(nameof(NPC.defDefense))))
                throw new ILPatchFailureException(Mod, il, null);
            int lastStatement = c.Index;
            if (!c.TryGotoNext(i => i.MatchRet()) || c.Index - lastStatement > 10)
                throw new ILPatchFailureException(Mod, il, null);
            c.EmitLdarg0();
            c.EmitDelegate(static (NPC npc) =>
            {
                var cfg = WorldSettings.Current;
                if (cfg == null) // ScaleStats can run while mods are still loading
                    return;

                if (npc.type >= 0 && NPCID.Sets.ProjectileNPC[npc.type])
                {
                    // Projectile NPCs (Burning Sphere, Plantera's spores, Fishron's bubbles...) are enemy attacks: only their
                    // hit damage scales, with the projectile setting.
                    npc.damage = Scale(npc.damage, cfg.ProjectileDamagePercent);
                }
                else
                {
                    npc.lifeMax = Scale(npc.lifeMax, cfg.HealthPercent);
                    npc.damage = Scale(npc.damage, cfg.DamagePercent);
                    npc.defense = Scale(npc.defense, cfg.DefensePercent);
                }

                npc.life = npc.lifeMax;
                npc.defDamage = npc.damage;
                npc.defDefense = npc.defense;
            });
        }

        // Exact integer percent (100% leaves the value untouched). A positive stat never drops to 0, and 0 or negative stays as it is.
        internal static int Scale(int value, int percent)
        {
            if (value <= 0)
                return value;
            return (int)Math.Clamp((long)value * percent / 100, 1, int.MaxValue);
        }
    }

    // Edits NPC.UpdateNPC_Inner to scale how far enemies move each tick. Vanilla has no enemy speed stat (every AI hard-codes its
    // own speeds), but all NPCs share one movement step after AI(). The velocity is scaled only around that step and restored
    // afterwards, so the AI keeps seeing its own values and the scale never compounds. Runs on every machine that simulates the
    // NPC (server and clients) with the synced server config, so they all move NPCs the same way.
    public class EnemySpeedEdit : ModSystem
    {
        public override void Load()
        {
            IL_NPC.UpdateNPC_Inner += PatchUpdateNPC;
        }

        private void PatchUpdateNPC(ILContext il)
        {
            var c = new ILCursor(il);

            // C# 88817-88825: if (!noTileCollide) UpdateCollision(); else { oldPosition = position; oldDirection = direction; position += velocity; ... }
            // IL_0775 ldarg.0 / ldfld noTileCollide / brtrue.s IL_0788 / ldarg.0 / call UpdateCollision / br IL_0811 (both branches meet at IL_0811)
            ILLabel join = null;
            if (!c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<NPC>(nameof(NPC.noTileCollide)),
                    i => i.MatchBrtrue(out _),
                    i => i.MatchLdarg0(),
                    i => i.MatchCall<NPC>("UpdateCollision"),
                    i => i.MatchBr(out join)))
                throw new ILPatchFailureException(Mod, il, null);

            var speed = new VariableDefinition(il.Import(typeof(float)));
            il.Body.Variables.Add(speed);

            c.MoveAfterLabels(); // three branches jump to this if; they must all scale
            c.EmitLdarg0();
            c.EmitDelegate(static (NPC npc) =>
            {
                float scale = SpeedScale(npc);
                npc.velocity *= scale;
                return scale;
            });
            c.EmitStloc(speed);

            c.GotoLabel(join); // AfterLabel: both branches now run the restore before IL_0811
            c.EmitLdarg0();
            c.EmitLdloc(speed);
            c.EmitDelegate(static (NPC npc, float scale) =>
            {
                npc.velocity /= scale;
                if (!npc.noTileCollide)
                    npc.oldVelocity /= scale; // UpdateCollision copied the scaled velocity; bouncing AIs read it next tick
            });
        }

        // Enemies only: friendly NPCs, town NPCs and critters keep vanilla movement.
        // Projectile NPCs are enemy attacks, so they follow the projectile speed setting instead.
        private static float SpeedScale(NPC npc)
        {
            if (!IsHostile(npc))
                return 1f;
            var cfg = WorldSettings.Current;
            if (cfg == null)
                return 1f;
            return (NPCID.Sets.ProjectileNPC[npc.type] ? cfg.EnemyProjectileSpeedPercent : cfg.EnemySpeedPercent) / 100f;
        }

        // Everything that isn't friendly, a town NPC or a critter: enemies, bosses and their parts, and projectile NPCs.
        internal static bool IsHostile(NPC npc) => !npc.friendly && !npc.townNPC && !NPCID.Sets.CountsAsCritter[npc.type];
    }

    // Edits Projectile.Update to scale how far enemy projectiles move each tick, the same way EnemySpeedEdit does for NPCs:
    // the velocity is scaled only around HandleMovement (tile collision + moving) and restored afterwards, so projectile AI
    // (homing, gravity, acceleration) keeps its own values. Runs once per update, so projectiles with extra updates scale per update.
    public class EnemyProjectileSpeedEdit : ModSystem
    {
        public override void Load()
        {
            IL_Projectile.Update += PatchUpdate;
        }

        private void PatchUpdate(ILContext il)
        {
            var c = new ILCursor(il);

            // C# 15176: HandleMovement(wetVelocity, out overrideWidth, out var overrideHeight);  (IL_121b ldarg.0 ... IL_1221 call)
            if (!c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloca(out _),
                    i => i.MatchLdloca(out _),
                    i => i.MatchCall<Projectile>("HandleMovement")))
                throw new ILPatchFailureException(Mod, il, null);

            var speed = new VariableDefinition(il.Import(typeof(float)));
            il.Body.Variables.Add(speed);

            c.EmitLdarg0();
            c.EmitDelegate(static (Projectile projectile) =>
            {
                float scale = SpeedScale(projectile);
                projectile.velocity *= scale;
                return scale;
            });
            c.EmitStloc(speed);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchCall<Projectile>("HandleMovement")))
                throw new ILPatchFailureException(Mod, il, null);
            c.EmitLdarg0();
            c.EmitLdloc(speed);
            c.EmitDelegate(static (Projectile projectile, float scale) => { projectile.velocity /= scale; });
        }

        private static float SpeedScale(Projectile projectile)
        {
            if (!projectile.TryGetGlobalProjectile(out EnemyProjectile mark) || !mark.FiredByEnemy)
                return 1f;
            var cfg = WorldSettings.Current;
            return cfg == null ? 1f : cfg.EnemyProjectileSpeedPercent / 100f;
        }
    }
}
