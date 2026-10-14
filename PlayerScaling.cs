using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AllStatsScale
{
    // Player stat settings. Each one runs on every machine that simulates the player (the owner's client, the server and
    // other clients) with the synced server config, and the owner's client is the one that decides damage, healing,
    // knockback and respawning for its own player, so single player and multiplayer behave the same.
    // These use tModLoader hooks because each hook hands over exactly the value to change.
    public class PlayerScaling : ModPlayer
    {
        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            base.ModifyMaxStats(out health, out mana);
            int percent = WorldSettings.Current?.PlayerMaxHealthPercent ?? 100;
            int baseLife = Player.statLifeMax; // crystals and fruit; tModLoader resets it right before this hook
            if (percent == 100 || baseLife <= 0)
                return;
            // tModLoader truncates the result to an int, and 100 * 0.01f is 0.99999994, so aim half a point above the exact target.
            int target = Math.Max(1, (int)Math.Min((long)baseLife * percent / 100, int.MaxValue));
            health *= (target + 0.5f) / baseLife;
        }

        public override void PostUpdateEquips()
        {
            int percent = WorldSettings.Current?.PlayerDefensePercent ?? 100;
            if (percent != 100)
                Player.statDefense *= percent / 100f; // final multiplier, so defense added later this tick is scaled too
        }

        public override void PostUpdateRunSpeeds()
        {
            var cfg = WorldSettings.Current;
            if (cfg != null && cfg.PlayerSpeedMultiplier > 0)
            {
                Player.maxRunSpeed *= cfg.PlayerSpeedMultiplier;
                Player.accRunSpeed *= cfg.PlayerSpeedMultiplier;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            int percent = WorldSettings.Current?.PlayerKnockbackPercent ?? 100;
            if (percent != 100)
                modifiers.Knockback *= percent / 100f;
        }

        // Set by RespawnTimeEdit at every death: true when vanilla gave the boss-fight respawn time and a real boss
        // (not an event boss) was nearby. Only possible in multiplayer, like vanilla's boss respawn time.
        internal bool DiedInBossFight;

        // Respawn When Boss Fight Ends: the dying player's own machine decides when it respawns (vanilla does the same), and NPC
        // deaths and despawns reach it through normal NPC sync. Runs before vanilla's countdown, so a 0 respawns this tick.
        public override void UpdateDead()
        {
            if (DiedInBossFight && Player.whoAmI == Main.myPlayer && Player.respawnTimer > 0
                && WorldSettings.Current?.RespawnWhenBossFightEnds == true && !RespawnTimeEdit.AnyFightBossAlive())
                Player.respawnTimer = 0;
        }

        // Player.Hurt sets the invincibility timer for this hit (immuneTime, or one hurtCooldowns slot) before PostHurt runs.
        public override void PostHurt(Player.HurtInfo info)
        {
            int percent = WorldSettings.Current?.PlayerImmunityPercent ?? 100;
            if (percent == 100)
                return;
            if (info.CooldownCounter == ImmunityCooldownID.General)
                Player.immuneTime = (int)((long)Player.immuneTime * percent / 100);
            else if (info.CooldownCounter >= 0 && info.CooldownCounter < Player.hurtCooldowns.Length)
                Player.hurtCooldowns[info.CooldownCounter] = (int)((long)Player.hurtCooldowns[info.CooldownCounter] * percent / 100);
        }

    }

    // Edits Player.UpdateLifeRegen so Player Life Regen Percent scales every source of healing over time (natural regen,
    // accessories, buffs, campfire...) but never debuff damage. Vanilla zeroes positive regen when a debuff is active and
    // subtracts the debuff damage, then adds the remaining positive sources and natural regen. So the debuff part is
    // snapshotted once its mitigations are done, and only what was added on top of it is scaled.
    public class PlayerLifeRegenEdit : ModSystem
    {
        public override void Load()
        {
            IL_Player.UpdateLifeRegen += PatchUpdateLifeRegen;
        }

        private void PatchUpdateLifeRegen(ILContext il)
        {
            var c = new ILCursor(il);

            // C# 17025: PlayerLoader.UpdateBadLifeRegen(this);  then honey, nebula and resting soften debuff damage.
            // C# 17042: lifeRegenTime += 1f;  (IL_02de, jumped to by the two branches before it)
            if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(typeof(PlayerLoader), nameof(PlayerLoader.UpdateBadLifeRegen))))
                throw new ILPatchFailureException(Mod, il, null);
            if (!c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdarg0(),
                    i => i.MatchLdarg0(),
                    i => i.MatchLdfld<Player>(nameof(Player.lifeRegenTime)),
                    i => i.MatchLdcR4(1f),
                    i => i.MatchAdd(),
                    i => i.MatchStfld<Player>(nameof(Player.lifeRegenTime))))
                throw new ILPatchFailureException(Mod, il, null);

            var debuffRegen = new VariableDefinition(il.Import(typeof(int)));
            il.Body.Variables.Add(debuffRegen);
            c.MoveAfterLabels();
            c.EmitLdarg0();
            c.EmitDelegate(static (Player player) => Math.Min(player.lifeRegen, 0));
            c.EmitStloc(debuffRegen);

            // C# 17199: lifeRegen += (int)Math.Round(num5);  (natural regen, IL_0711 call Round ... IL_0718 stfld)
            // C# 17200: lifeRegenCount += lifeRegen;  comes next, so the scaled total is what heals.
            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchCall(typeof(Math), nameof(Math.Round)),
                    i => i.MatchConvI4(),
                    i => i.MatchAdd(),
                    i => i.MatchStfld<Player>(nameof(Player.lifeRegen))))
                throw new ILPatchFailureException(Mod, il, null);
            c.EmitLdarg0();
            c.EmitLdloc(debuffRegen);
            c.EmitDelegate(static (Player player, int debuff) => ScaleRegen(player, debuff));
        }

        private static void ScaleRegen(Player player, int debuffRegen)
        {
            int percent = WorldSettings.Current?.PlayerLifeRegenPercent ?? 100;
            if (percent == 100 || player.starving) // Starving overwrites lifeRegen after the snapshot; it stops all healing anyway
                return;
            int healing = player.lifeRegen - debuffRegen;
            if (healing > 0)
                player.lifeRegen = debuffRegen + (int)((long)healing * percent / 100);
            if (player.palladiumRegen)
                player.lifeRegenCount += 4 * percent / 100 - 4; // Palladium's Rapid Healing adds 4 to the counter right after this
        }
    }

    // Respawn time. Edits Player.GetRespawnTime, which vanilla uses for every death: 10 s, +10 s in multiplayer when a boss
    // (or Eater of Worlds segment) is within 4000 px of where you died, x1.5 in Expert/Master, x2 in multiplayer For the Worthy
    // boss fights. The result is scaled with Player Boss Fight Respawn Time Percent when vanilla's own boss flag is set (so only
    // in multiplayer, like vanilla) and the boss isn't an event NPC, otherwise with Player Respawn Time Percent. Also edits Player.UpdateDead, whose countdown
    // clamps the timer to 3600 ticks (60 s) every tick, so long settings work. The new cap is the limit the save file already
    // applies to a dead player's timer; vanilla never sets the timer above 3600, so the cap changes nothing at 100%.
    public class RespawnTimeEdit : ModSystem
    {
        public const int MaxRespawnTicks = 60000;

        public override void Load()
        {
            IL_Player.GetRespawnTime += PatchGetRespawnTime;
            IL_Player.UpdateDead += PatchUpdateDead;
        }

        private void PatchGetRespawnTime(ILContext il)
        {
            var c = new ILCursor(il);
            int bossFight = -1;

            // C# 35346-35347: int num = 600; bool flag = false;  (IL_0000-IL_0007)
            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(600),
                    i => i.MatchStloc(out _),
                    i => i.MatchLdcI4(0),
                    i => i.MatchStloc(out bossFight)))
                throw new ILPatchFailureException(Mod, il, null);

            // C# 35383: return num;  (IL_0138 ldloc.0, jumped to by four branches / IL_0139 ret, the only ret)
            if (!c.TryGotoNext(MoveType.Before, i => i.MatchRet()))
                throw new ILPatchFailureException(Mod, il, null);
            c.MoveAfterLabels();
            c.EmitLdarg0();
            c.EmitLdloc(bossFight);
            c.EmitDelegate(static (int respawnTime, Player player, bool vanillaBossFight) => Scale(respawnTime, player, vanillaBossFight));
        }

        private static int Scale(int respawnTime, Player player, bool vanillaBossFight)
        {
            // Vanilla's flag also fires for the two event NPCs that carry the boss flag; those deaths use the normal setting.
            bool bossFight = vanillaBossFight && FightBossNear(player);
            player.GetModPlayer<PlayerScaling>().DiedInBossFight = bossFight;

            var cfg = WorldSettings.Current;
            int percent = cfg == null ? 100 : bossFight ? cfg.PlayerBossRespawnPercent : cfg.PlayerRespawnPercent;
            if (percent == 100)
                return respawnTime;
            return (int)Math.Min((long)respawnTime * percent / 100, MaxRespawnTicks);
        }

        // What vanilla's respawn check counts (the boss flag, or an Eater of Worlds segment), minus event NPCs. Event minibosses
        // like the Pumpking, Ice Queen, Mothron or the lunar pillars don't have the boss flag, so only these two need excluding.
        private static bool IsFightBoss(NPC npc) =>
            npc.active
            && (npc.boss || npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
            && npc.type is not (NPCID.MartianSaucerCore or NPCID.TorchGod);

        // Vanilla's distance rule: |dx| + |dy| < 4000 px from where the player died.
        private static bool FightBossNear(Player player)
        {
            foreach (NPC npc in Main.npc)
                if (IsFightBoss(npc) && Math.Abs(player.Center.X - npc.Center.X) + Math.Abs(player.Center.Y - npc.Center.Y) < 4000f)
                    return true;
            return false;
        }

        // The fight counts as over when no boss is left anywhere. Tracking only the boss that killed you would end too early
        // for the Twins (one dies, the other is still up) and the Eater of Worlds (it splits into new worms).
        internal static bool AnyFightBossAlive()
        {
            foreach (NPC npc in Main.npc)
                if (IsFightBoss(npc))
                    return true;
            return false;
        }

        private void PatchUpdateDead(ILContext il)
        {
            var c = new ILCursor(il);
            int patched = 0;

            // C# 15254 (Hardcore) and 15263 (others): respawnTimer = Utils.Clamp(respawnTimer - 1, 0, 3600);
            // (IL_041a / IL_0453: ldc.i4 3600, call Utils::Clamp<int32>, stfld respawnTimer)
            while (c.TryGotoNext(MoveType.After,
                       i => i.MatchLdcI4(3600),
                       i => i.MatchCall(typeof(Utils), nameof(Utils.Clamp)),
                       i => i.MatchStfld<Player>(nameof(Player.respawnTimer))))
            {
                c.Index -= 2; // between the 3600 and the Clamp call
                c.EmitDelegate(static (int max) => Math.Max(max, MaxRespawnTicks));
                c.Index += 2;
                patched++;
            }
            if (patched != 2)
                throw new ILPatchFailureException(Mod, il, new Exception($"expected 2 respawn clamps, patched {patched}"));
        }
    }
}
