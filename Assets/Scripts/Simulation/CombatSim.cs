using System;
using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 战斗结算：每 tick 先让箭塔射击射程内最近的敌人，再让存活敌人向据点走一步；
    /// 敌人到达据点则造成伤害并消失。
    /// </summary>
    public static class CombatSim
    {
        public static void Tick(GridMap map, Base baseDefense, List<Enemy> enemies)
        {
            if (map == null || baseDefense == null || enemies == null)
            {
                return;
            }

            // 1. 箭塔攻击
            var towerDef = BuildingCatalog.Get(BuildingType.ArrowTower);
            if (towerDef != null && towerDef.Range > 0 && towerDef.Damage > 0)
            {
                foreach (var towerPos in map.FindBuildingPositions(BuildingType.ArrowTower))
                {
                    var target = FindNearestEnemy(towerPos, enemies, towerDef.Range);
                    if (target == null)
                    {
                        continue;
                    }

                    target.HP -= towerDef.Damage;
                    if (target.IsDead)
                    {
                        GameEvents.RaiseEnemyDied(target);
                    }
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
            var bestDistance = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                // 曼哈顿距离：与敌人四方向寻路的步数一致（|dx|+|dy|）
                var dx = Math.Abs(enemy.Position.X - towerPos.X);
                var dy = Math.Abs(enemy.Position.Y - towerPos.Y);
                var distance = dx + dy;
                if (distance <= range && distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
