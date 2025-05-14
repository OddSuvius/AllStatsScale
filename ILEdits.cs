using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil;
using Mono.Cecil.Cil; // Required for Instruction
using Terraria;
using Terraria.ModLoader;
using System;
using System.Reflection;
using System.Linq; // Needed for FirstOrDefault
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace AllStatsScaled
{
    public static class ILEdits
    {
        private static ILHook _scaleStatsHook;

        public static void ApplyILEdits()
        {
            var scaleStatsMethodInfo = typeof(NPC).GetMethod("ScaleStats", BindingFlags.Instance | BindingFlags.Public);
            if (scaleStatsMethodInfo != null)
            {
                _scaleStatsHook = new ILHook(scaleStatsMethodInfo, HookScaleStats);
            }
            else
            {
                ModContent.GetInstance<AllStatsScaled>().Logger.Error("Could not find NPC.ScaleStats method");
            }

        }

        public static void UnloadILEdits()
        {
            _scaleStatsHook?.Dispose();
            _scaleStatsHook = null;
        }

            private static void HookScaleStats(ILContext il)
        {
            var cursor = new ILCursor(il);
            try
            {
                // --- Part 1: Modify player count for scaling calculations ---

                // We need to find the point where 'num3' (locals[4]) is set.
                // This happens at IL_0112: stloc.s locals[4]
                // This is after 'statsAreScaledForThisManyPlayers' is set (IL_010B).
                // We will inject *after* 'stloc.s locals[4]' to override num3.

                // First, find a stable anchor before this point. The call to NPC.GetActivePlayerCount() or
                // the stfld to statsAreScaledForThisManyPlayers are candidates.
                // Let's find the 'stfld statsAreScaledForThisManyPlayers'.
                if (!cursor.TryGotoNext(MoveType.After, instr => // MoveType.After places cursor after this instruction
                    instr.OpCode == OpCodes.Stfld &&
                    instr.Operand is FieldReference fr &&
                    fr.Name == "statsAreScaledForThisManyPlayers" &&
                    fr.DeclaringType.FullName == "Terraria.NPC"))
                {
                    // This part of the code (expert mode scaling) might not be reached if not in expert/master.
                    // If the hook should only modify stats at the end, this part might be conditional or more robustly targeted.
                    // For now, assume we always want to try this if the hook runs.
                    ModContent.GetInstance<AllStatsScaled>().Logger.Warn("[AllStatsScaled] Could not find 'stfld NPC.statsAreScaledForThisManyPlayers'. Player count override might not apply if not in expert/master mode context.");
                }
                else
                {
                    // Cursor is now after 'stfld NPC.statsAreScaledForThisManyPlayers' (after IL_010B).
                    // The next key instruction is 'stloc.s V_8' then 'ldloc.s V_8' then 'stloc.s num3' (locals[4] at IL_0112).
                    // We want to be after 'stloc.s num3'.
                    if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(4) || (instr.OpCode == OpCodes.Stloc_S && instr.Operand is VariableDefinition vd && vd.Index == 4)))
                    {
                        throw new Exception("Could not find 'stloc.s num3' (locals[4]) after 'statsAreScaledForThisManyPlayers'");
                    }

                    // Cursor is now after 'stloc.s num3'.
                    // The original 'num3' has been stored. Now we override it.
                    // We need the NPC instance (ldarg.0) and the original activePlayersCount method parameter (ldarg.1).

                    // Get the VariableDefinition for num3 (locals[4])
                    var num3Variable = il.Body.Variables.FirstOrDefault(v => v.Index == 4);
                    if (num3Variable == null)
                    {
                        throw new Exception("Could not find local variable 'num3' (index 4).");
                    }

                    cursor.Emit(OpCodes.Ldarg_0); // NPC instance for delegate
                    cursor.Emit(OpCodes.Ldarg_1); // activePlayersCount (method argument ldarg.1) for delegate

                    cursor.EmitDelegate<Func<NPC, Nullable<int>, int>>((npc, activePlayersCountArg) =>
                    {
                        int determinedPlayerCount;
                        var cfg = ModContent.GetInstance<AllStatsScaledConfig>();

                        if (cfg != null && cfg.EnableSimulatedPlayers)
                        {
                            if (cfg.SimulatedPlayers <= 0)
                            {
                                // If SimulatedPlayers is invalid, use actual active player count
                                determinedPlayerCount = NPC.GetActivePlayerCount();
                            }
                            else
                            {
                                determinedPlayerCount = cfg.SimulatedPlayers;
                            }
                            // Update the NPC field directly with our chosen count
                            npc.statsAreScaledForThisManyPlayers = determinedPlayerCount;
                        }
                        else
                        {
                            // Not using simulated players (or config is null), so num3 should reflect vanilla logic.
                            // npc.statsAreScaledForThisManyPlayers was already set by vanilla code before our injection point.
                            // We need to return the value that vanilla would have used for num3.
                            if (!activePlayersCountArg.HasValue)
                            {
                                determinedPlayerCount = NPC.GetActivePlayerCount();
                            }
                            else
                            {
                                determinedPlayerCount = activePlayersCountArg.Value;
                            }
                            // In this case, npc.statsAreScaledForThisManyPlayers already holds this 'determinedPlayerCount'
                            // due to the vanilla code that ran before this delegate.
                        }
                        return determinedPlayerCount; // This value will be stored into num3
                    });

                    cursor.Emit(OpCodes.Stloc_S, num3Variable); // Store the delegate's result back into 'num3'
                }


                // --- Part 2: Apply custom stat multipliers at the end of the method ---
                // Reset cursor to find the *final* return.
                // It's important that Part 1's cursor manipulations don't affect this search.
                // Creating a new cursor for this part or carefully resetting Index is an option.
                // The current approach reuses the cursor, which is fine as long as Part 1
                // doesn't leave it in an unexpected state for the loop below.
                // For safety, explicitly set to start of method if Part 1 might not find its target.
                if (cursor.Next == null) // If Part 1 failed to find its target and didn't move cursor.
                {
                    cursor.Index = 0;
                }


                Instruction finalRetInstruction = null;
                // Find the last 'ret' instruction by iterating from the end
                for (int i = il.Instrs.Count - 1; i >= 0; i--)
                {
                    if (il.Instrs[i].OpCode == OpCodes.Ret)
                    {
                        finalRetInstruction = il.Instrs[i];
                        break;
                    }
                }

                if (finalRetInstruction == null)
                {
                    throw new Exception("Could not find any method return (ret instruction) for Part 2");
                }

                cursor.Goto(finalRetInstruction, MoveType.Before);

                cursor.Emit(OpCodes.Ldarg_0); // Load 'this' (the NPC instance)
                cursor.EmitDelegate<Action<NPC>>(npc =>
                {
                    var cfg = ModContent.GetInstance<AllStatsScaledConfig>();
                    if (cfg == null)
                    {
                        ModContent.GetInstance<AllStatsScaled>()?.Logger.Warn("AllStatsScaledConfig is null in HookScaleStats (Part 2 delegate)");
                        return;
                    }

                    npc.lifeMax = Math.Max(1, (int)Math.Min((long)(npc.lifeMax * cfg.HealthMultiplier), int.MaxValue));
                    npc.damage = Math.Max(1, (int)Math.Min((long)(npc.damage * cfg.DamageMultiplier), int.MaxValue));
                    npc.defense = Math.Max(1, (int)Math.Min((long)(npc.defense * cfg.DefenseMultiplier), int.MaxValue));

                    npc.life = npc.lifeMax;
                    npc.defDamage = npc.damage;
                    npc.defDefense = npc.defense;
                });
            }
            catch (Exception ex)
            {
                var logger = ModContent.GetInstance<AllStatsScaled>()?.Logger;
                if (logger != null)
                {
                    logger.Error($"[AllStatsScaled] IL hook failed in HookScaleStats: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    if (il != null)
                    {
                        logger.Error($"Current IL instructions for {il.Method.FullName}:\n{il.ToString()}");
                    }
                }
                throw;
            }
        }
    }
}