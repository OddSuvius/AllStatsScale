using System;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AllStatsScale
{
    // Enemy Invincibility Frames Percent. Vanilla sets an enemy's immunity after a hit in 40+ places (each weapon and
    // projectile type), but counts it down in only three loops, so the countdown speed is what gets scaled: each tick the
    // timers drop by Steps (100 / percent on average) instead of 1, so every immunity lasts percent% as long.
    //   NPC.immune[player]           projectiles with default immunity, dashes, special attacks
    //   Projectile.localNPCImmunity  yoyos, whips, most minions and piercing projectiles
    //   Player.meleeNPCHitCooldown   sword swings and melee projectiles
    // Per-type immunity stores an absolute expiry tick instead of a countdown, so it is rescaled when it is set (EnemyImmunityHits).
    // Player hits are decided on the attacking player's machine, which runs all of this with the synced config.
    public class EnemyImmunityEdit : ModSystem
    {
        private static int steps = 1;
        private static double carry;

        public override void PreUpdateEntities() // once per game tick, before NPCs, projectiles and players update
        {
            int percent = WorldSettings.Current?.EnemyImmunityPercent ?? 100;
            if (percent == 100)
            {
                steps = 1;
                carry = 0;
                return;
            }
            carry += 100.0 / percent;
            steps = (int)carry;
            carry -= steps;
        }

        public override void Load()
        {
            IL_NPC.UpdateNPC_Inner += PatchNPCImmunity;
            IL_Projectile.Update += PatchProjectileImmunity;
            IL_Player.UpdateMeleeHitCooldowns += PatchMeleeCooldowns;
        }

        // C# 88778: immune[j]--;  inside if (immune[j] > 0)
        private void PatchNPCImmunity(ILContext il)
        {
            var c = new ILCursor(il);
            FindCountdown(c, il, typeof(NPC), nameof(NPC.immune));
            c.EmitLdarg0();
            c.EmitDelegate(static (int decremented, NPC npc) => Countdown(decremented, npc, clampAtZero: true));
        }

        // C# 14923: localNPCImmunity[k]--;  inside if (localNPCImmunity[k] > 0), k = the NPC's index
        private void PatchProjectileImmunity(ILContext il)
        {
            var c = new ILCursor(il);
            int npcIndex = FindCountdown(c, il, typeof(Projectile), nameof(Projectile.localNPCImmunity));
            c.EmitLdloc(npcIndex);
            c.EmitDelegate(static (int decremented, int index) => Countdown(decremented, Main.npc[index], clampAtZero: true));
        }

        // C# 40607: meleeNPCHitCooldown[i]--;  no > 0 check, vanilla lets it go negative (a hit is allowed at <= 0)
        private void PatchMeleeCooldowns(ILContext il)
        {
            var c = new ILCursor(il);
            int npcIndex = FindCountdown(c, il, typeof(Player), nameof(Player.meleeNPCHitCooldown));
            c.EmitLdloc(npcIndex);
            c.EmitDelegate(static (int decremented, int index) => Countdown(decremented, Main.npc[index], clampAtZero: false));
        }

        // All three loops compile `array[i]--` to: ldfld array, ldloc i, ldelema int32, dup, ldind.i4, ldc.i4.1, sub, stind.i4.
        // Leaves the cursor after the sub (value - 1 on the stack) and returns the index local.
        private int FindCountdown(ILCursor c, ILContext il, Type type, string field)
        {
            int index = -1;
            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdfld(type, field),
                    i => i.MatchLdloc(out index),
                    i => i.MatchLdelema(out _),
                    i => i.MatchDup(),
                    i => i.MatchLdindI4(),
                    i => i.MatchLdcI4(1),
                    i => i.MatchSub()))
                throw new ILPatchFailureException(Mod, il, null);
            return index;
        }

        // Turns vanilla's value - 1 into value - Steps. NPC and projectile timers stop at 0, because a negative value there
        // would make the enemy immune to that attacker for good.
        private static int Countdown(int decremented, NPC npc, bool clampAtZero)
        {
            if (steps == 1 || !Applies(npc))
                return decremented;
            int value = decremented + 1 - steps;
            return clampAtZero && value < 0 ? 0 : value;
        }

        // Enemies only; projectile NPCs (Burning Sphere, spores...) are enemy attacks and keep vanilla immunity.
        internal static bool Applies(NPC npc) => npc.active && EnemySpeedEdit.IsHostile(npc) && !NPCID.Sets.ProjectileNPC[npc.type];
    }

    public class EnemyImmunityHits : GlobalNPC
    {
        // Projectile.Damage stores per-type immunity as an expiry tick (perIDStaticNPCImmunity) before this hook runs. A projectile
        // can only hit when that tick has passed, so a value in the future here was set by this hit.
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            int percent = WorldSettings.Current?.EnemyImmunityPercent ?? 100;
            if (percent == 100 || !projectile.usesIDStaticNPCImmunity || !EnemyImmunityEdit.Applies(npc))
                return;
            ref uint expires = ref Projectile.perIDStaticNPCImmunity[projectile.type][npc.whoAmI];
            uint now = Main.GameUpdateCount;
            if (expires > now)
                expires = now + (uint)Math.Min((ulong)(expires - now) * (ulong)percent / 100, uint.MaxValue - now);
        }
    }
}
