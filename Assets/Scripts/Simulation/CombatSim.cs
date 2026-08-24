using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 战斗结算：每 tick 先让箭塔射击射程内最近的敌人，再让存活敌人向据点走一步；
    /// 敌人到达据点则造成伤害并消失。
    /// </summary>
    public static class CombatSim
    {
        public const int TowerRange = 3;
        public const int TowerDamage = 1;

        public static void Tick(GridMap map, Base baseDefense, List<Enemy> enemies)
        {
            if (map == null || baseDefense == null || enemies == null)
            {
                return;
            }

            // 1. 箭塔攻击
            foreach (var towerPos in map.FindBuildingPositions(BuildingType.ArrowTower))
            {
                var target = FindNearestEnemy(towerPos, enemies, TowerRange);
                if (target == null)
                {
                    continue;
                }

                target.HP -= TowerDamage;
                if (target.IsDead)
                {
                    GameEvents.RaiseEnemyDied(target);
                }
            }

            enemies.RemoveAll(e => e.IsDead);

            // 2. 敌人移动
            foreach (var enemy in enemies)
            {
                var next = Pathfinding.NextStep(map, enemy.Position, baseDefense.Position);
                if (next == null)
                {
                    continue;
                }

                if (next.Value == baseDefense.Position)
                {
                    baseDefense.TakeDamage(enemy.DamageToBase);
                    enemy.HP = 0;
                    GameEvents.RaiseEnemyDied(enemy);
                    continue;
                }

                enemy.Position = next.Value;
                GameEvents.RaiseEnemyMoved(enemy);
            }

            enemies.RemoveAll(e => e.IsDead);
        }

        private static Enemy FindNearestEnemy(GridPos towerPos, List<Enemy> enemies, int range)
        {
            Enemy nearest = null;
            var bestDistanceSq = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                var dx = enemy.Position.X - towerPos.X;
                var dy = enemy.Position.Y - towerPos.Y;
                var distSq = dx * dx + dy * dy;
                if (distSq <= range * range && distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
