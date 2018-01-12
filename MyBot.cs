using System;
using Halite2.hlt;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Halite2
{
    public class MyBot
    {
        

        private static Move GetPlanetCommand(Ship ship, Planet planet, GameMap gameMap)
        {
            if (ship.CanDock(planet)) return new DockMove(ship, planet);
            return Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED);
        }

        private static Move GetShipCommand(Ship ship, Ship enemyShip, GameMap gameMap)
        {
            var isWeakEnemy = enemyShip.GetHealth() <= ship.GetHealth() ||
                              enemyShip.GetDockingStatus() != Ship.DockingStatus.Undocked;
            var destPoint = isWeakEnemy ? ship.GetClosestPoint(enemyShip, Constants.MIN_DISTANCE_FOR_CLOSEST_POINT) : enemyShip;
            return Navigation.NavigateShipTowardsTarget(gameMap,
                ship,
                destPoint,
                Constants.MAX_SPEED,
                true,
                Constants.MAX_NAVIGATION_CORRECTIONS, 
                Math.PI / 180.0);
        }

        public static void Main(string[] args)
        {
            try
            {
                int currentTurn = 0;
                IDictionary<int, IList<int>> planetsShips = new Dictionary<int, IList<int>>();
                IDictionary<int, int> shipsPlanets = new Dictionary<int, int>();

                string name = args.Length > 0 ? args[0] : "Sharpie";

                Networking networking = new Networking();
                GameMap gameMap = networking.Initialize(name);

                foreach (var p in gameMap.GetAllPlanets().Keys)
                {
                    planetsShips.Add(p, new List<int>());
                }

                List<Move> moveList = new List<Move>();
                for (;;)
                {
                    moveList.Clear();
                    gameMap.UpdateMap(Networking.ReadLineIntoMetadata());

                    var myShips = gameMap.GetMyPlayer().GetShips();
                    foreach (var shipId in shipsPlanets.Keys.ToList())
                    {
                        if (myShips.All(s => s.Key != shipId))
                        {
                            planetsShips[shipsPlanets[shipId]].Remove(shipId);
                            shipsPlanets.Remove(shipId);
                        }
                    }

                    var shipsIds = myShips.Keys.ToList();
                    for (var i = 0; i < shipsIds.Count; ++i)
                    {
                        if (i < 5 && i > currentTurn)
                        {
                            continue;
                        }

                        var id = shipsIds[i];
                        var ship = myShips[id];

                        if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked) continue;

                        var entities = gameMap.NearbyEntitiesByDistance(ship);
                        var sortedEntities = new SortedDictionary<double, Entity>(entities);

                        Move command = null;

                        foreach (var dist in sortedEntities.Keys)
                        {
                            var unit = sortedEntities[dist];
                            var enemyShip = unit as Ship;
                            if (enemyShip != null && enemyShip.GetOwner() != gameMap.GetMyPlayerId())
                            {
                                //if (enemyShip.GetDockingStatus() == Ship.DockingStatus.Undocked) break;
                                if (ship.GetDistanceTo(enemyShip) > Constants.MAX_SPEED * 3) break;

                                command = GetShipCommand(ship, enemyShip, gameMap);
                                if (command != null)
                                {
                                    if (shipsPlanets.ContainsKey(id))
                                    {
                                        planetsShips[shipsPlanets[id]].Remove(id);
                                        shipsPlanets.Remove(id);
                                    }
                                    break;
                                }
                            }
                        }
                        if (command == null)
                        {
                            foreach (var dist in sortedEntities.Keys)
                            {
                                var unit = sortedEntities[dist];
                                var planet = unit as Planet;
                                if (planet != null &&
                                    (!planet.IsOwned() || planet.GetOwner() == gameMap.GetMyPlayerId()) &&
                                    (planetsShips[planet.GetId()].Count < planet.GetDockingSpots() ||
                                     shipsPlanets.ContainsKey(id) &&
                                     shipsPlanets[id] == planet.GetId()))
                                {
                                    command = GetPlanetCommand(ship, planet, gameMap);

                                    if (command != null)
                                    {
                                        if (shipsPlanets.ContainsKey(id))
                                        {
                                            planetsShips[shipsPlanets[id]].Remove(id);
                                        }
                                        planetsShips[planet.GetId()].Add(id);
                                        //var str = string.Format("{0}:{1}",
                                        //    planet.GetId(),
                                        //    planetsShips[planet.GetId()].Count);
                                        //Log.LogMessage(str);

                                        shipsPlanets[id] = planet.GetId();
                                        break;
                                    }
                                }

                                var enemyShip = unit as Ship;
                                if (enemyShip != null && enemyShip.GetOwner() != gameMap.GetMyPlayerId())
                                {
                                    command = GetShipCommand(ship, enemyShip, gameMap);
                                    if (command != null)
                                    {
                                        if (shipsPlanets.ContainsKey(id))
                                        {
                                            planetsShips[shipsPlanets[id]].Remove(id);
                                            shipsPlanets.Remove(id);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        if (command == null)
                        {
                            command = Navigation.NavigateShipTowardsTarget(gameMap,
                                ship,
                                ship,
                                0,
                                false,
                                Constants.MAX_NAVIGATION_CORRECTIONS,
                                5);
                        }
                        moveList.Add(command);
                    }

                    currentTurn++;
                    Networking.SendMoves(moveList);
                }
            }
            catch (Exception e)
            {
               
            }
        }
    }
}
