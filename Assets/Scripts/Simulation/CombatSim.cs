using System;
using System.Collections.Generic;

namespace ShengXi.Simulation
{
    /// <summary>
    /// 战斗结算：每 tick 先让箭塔射击射程内最近的敌人；
    /// 再让存活敌人优先锁定最近的建筑（曼哈顿距离），贴身后持续攻击直到摧毁；
    /// 场上没有建筑时才转向据点，到达据点造成伤害并消失。
    /// </summary>
    public static class CombatSim
    {
        public static void Tick(GridMap map, ResourcePool pool, Base baseDefense, List<Enemy> enemies)
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

            // 2. 敌人行动：优先攻击最近的建筑，无建筑则进攻据点
            foreach (var enemy in enemies)
            {
                var buildingTarget = FindNearestBuilding(map, enemy.Position);
                if (buildingTarget.HasValue)
                {
                    var buildingPos = buildingTarget.Value;
                    if (Pathfinding.IsAdjacent(enemy.Position, buildingPos))
                    {
                        AttackBuilding(map, pool, buildingPos, enemy.Damage);
                        continue;
                    }

                    var step = Pathfinding.NextStepToBuilding(map, enemy.Position, buildingPos);
                    if (!step.HasValue)
                    {
                        continue;
                    }

                    enemy.Position = step.Value;
                    GameEvents.RaiseEnemyMoved(enemy);
                    continue;
                }

                // 场上没有其他建筑：转向据点（保持原有行为）
                var next = Pathfinding.NextStep(map, enemy.Position, baseDefense.Position);
                if (next == null)
                {
                    continue;
                }

                if (next.Value == baseDefense.Position)
                {
                    baseDefense.TakeDamage(enemy.Damage);
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

        /// <summary>
        /// 锁定距敌人最近的建筑（曼哈顿距离，据点除外，仅统计存活建筑）。
        /// 没有其他建筑时返回 null，敌人转攻据点。
        /// </summary>
        private static GridPos? FindNearestBuilding(GridMap map, GridPos from)
        {
            GridPos? nearest = null;
            var bestDistance = int.MaxValue;

            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(new GridPos(x, y));
                    if (tile == null ||
                        tile.Building == BuildingType.None ||
                        tile.Building == BuildingType.Base ||
                        tile.BuildingHp <= 0)
                    {
                        continue;
                    }

                    var dx = Math.Abs(x - from.X);
                    var dy = Math.Abs(y - from.Y);
                    var distance = dx + dy;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = new GridPos(x, y);
                    }
                }
            }

            return nearest;
        }

        private static void AttackBuilding(GridMap map, ResourcePool pool, GridPos pos, int damage)
        {
            var tile = map.GetTile(pos);
            if (tile == null || tile.Building == BuildingType.None)
            {
                return;
            }

            tile.BuildingHp -= damage;
            if (tile.BuildingHp <= 0)
            {
                DemolishService.DestroyBuilding(map, pool, pos);
            }
        }
    }
}
