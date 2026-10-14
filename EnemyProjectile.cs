using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AllStatsScale
{
    // Enemy attacks: hostile projectiles spawned by an NPC, plus projectiles those spawn (splits, fragments).
    // Vanilla sets their damage per attack at about 50+ call sites, not from npc.damage, so one OnSpawn hook covers them all.
    // OnSpawn runs where the projectile is created (normally the server), before vanilla sends it to clients. The FiredByEnemy
    // mark goes along in the projectile's sync data, so every machine applies Enemy Projectile Speed (EnemyProjectileSpeedEdit)
    // to the same projectiles.
    public class EnemyProjectile : GlobalProjectile
    {
        public bool FiredByEnemy;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => lateInstantiation && entity.hostile;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is not EntitySource_Parent parent)
                return;

            if (parent.Entity is NPC)
            {
                FiredByEnemy = true;
                var cfg = WorldSettings.Current;
                // Vanilla syncs projectile damage as a 16-bit number, so cap it there or clients would read a wrapped value.
                projectile.damage = Math.Min(ScaleStatsEdit.Scale(projectile.damage, cfg.ProjectileDamagePercent), short.MaxValue);
            }
            else if (parent.Entity is Projectile p && p.TryGetGlobalProjectile(out EnemyProjectile parentMark) && parentMark.FiredByEnemy)
            {
                FiredByEnemy = true; // speed only; their damage is set by the parent's code
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(FiredByEnemy);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            FiredByEnemy = bitReader.ReadBit();
        }
    }
}
