using System;
using Halite2.hlt;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Halite2.AStar;

namespace Halite2
{

    public class MyBot
    {
        private enum Direction
        {
            N,
            S,
            W,
            E,
            NW,
            NE,
            SW,
            SE
        }

        private static int _currentTurn = 0;
        private static AStar.AStar _aStar;
        private const double Tolerance = 1E-3;

        private static IList<int> _gotShipIds = new List<int>();

        private static Direction GetDirection(ASquare s1, ASquare s2)
        {
            if (Math.Abs(s1.CenterY - s2.CenterY) < Tolerance)
            {
                return s2.CenterX < s1.CenterX ? Direction.W : Direction.E;
            }
            if (Math.Abs(s1.CenterX - s2.CenterX) < Tolerance)
            {
                return s2.CenterY < s1.CenterY ? Direction.N : Direction.S;
            }
            if (s2.CenterX < s1.CenterX && s2.CenterY < s1.CenterY) return Direction.NW;
            if (s2.CenterX > s1.CenterX && s2.CenterY < s1.CenterY) return Direction.NE;
            if (s2.CenterX > s1.CenterX && s2.CenterY > s1.CenterY) return Direction.SE;
            if (s2.CenterX < s1.CenterX && s2.CenterY > s1.CenterY) return Direction.SW;
            throw new Exception("Unknown direction");
        }

        private static IList<ASquare> GetPlanetDockingSquares(Planet planet)
        {
            var result = new List<ASquare>();
            var leftI = Math.Max(_aStar.GetSquareI(planet.GetXPos() - planet.GetRadius() - Constants.SHIP_RADIUS) - 1,
                0);
            var rightI = Math.Min(_aStar.GetSquareI(planet.GetXPos() + planet.GetRadius() + Constants.SHIP_RADIUS) + 1,
                _aStar.N - 1);

            var topJ = Math.Max(_aStar.GetSquareJ(planet.GetYPos() - planet.GetRadius() - Constants.SHIP_RADIUS) - 1,
                0);
            var bottomJ = Math.Min(_aStar.GetSquareI(planet.GetYPos() + planet.GetRadius() + Constants.SHIP_RADIUS) + 1,
                _aStar.M - 1);

            for (var i = leftI; i <= rightI; ++i)
            {
                for (var j = topJ; j <= bottomJ; ++j)
                {
                    var square = _aStar.Table[i, j];
                    if (square.Weight >= AStar.AStar.BigWeight) continue;
                    var position = new Position(square.CenterX, square.CenterY);
                    if (position.CanDock(planet))
                    {
                        result.Add(square);
                    }
                }
            }
            return result;
        }

        private static Move GetPlanetCommand(Ship ship, Planet planet, IDictionary<int, Ship> myShips)
        {
            var squareI = _aStar.GetSquareI(ship.GetXPos());
            var squareJ = _aStar.GetSquareJ(ship.GetYPos());
            var startPoint = _aStar.Table[squareI, squareJ];

            if (ship.CanDock(planet))
            {
                //if (!myShips.Any(s =>
                //    s.Key != ship.GetId() && _aStar.GetSquareI(s.Value.GetXPos()) == squareI &&
                //    _aStar.GetSquareJ(s.Value.GetYPos()) == squareJ))
                //{
                _aStar.AddBigWeight(startPoint);
                //}

                return new DockMove(ship, planet);
            }

            var dockingSquares = GetPlanetDockingSquares(planet);
            var endPoint = dockingSquares.OrderBy(s => ship.GetDistanceTo(new Position(s.CenterX, s.CenterY)))
                .FirstOrDefault();

            //if (ship.GetId() == 11)
            //{
            //    foreach (var sq in dockingSquares)
            //    {
            //        Log.LogMessage("Docking: " + sq.CenterX + " " + sq.CenterY);
            //    }
            //    Log.LogMessage("");
            //}

            if (endPoint == null)
            {

                Log.LogMessage("No planet " + ship.GetId());
                _aStar.AddBigWeight(startPoint);
                return new ThrustMove(ship, 0, 0);
            }
            var path = Calculator.GetPath(startPoint, endPoint, _aStar.Squares);
            if (ship.GetId() == 2)
            {
                Log.LogMessage("2:");
                foreach (ASquare step in path)
                {
                    Log.LogMessage(step.CenterX + " " + step.CenterY);
                }
                Log.LogMessage("");
            }

            if (ship.GetId() == 0)
            {
                Log.LogMessage("0:");
                foreach (ASquare step in path)
                {
                    Log.LogMessage(step.CenterX + " " + step.CenterY);
                }
                Log.LogMessage("");
            }

            if (path.Count >= 2)
            {
                int counter = 1;
                var targetPoint = path[counter] as ASquare;
                var direction = GetDirection(startPoint, targetPoint);
                while (counter < path.Count - 1)
                {
                    counter++;
                    if (GetDirection(targetPoint, path[counter] as ASquare) != direction) break;
                    targetPoint = path[counter] as ASquare;
                }

                var targetPosition = new Position(targetPoint.CenterX, targetPoint.CenterY);
                var angle = ship.OrientTowardsInDeg(targetPosition);
                var dist = ship.GetDistanceTo(targetPosition);
                var speed = dist > Constants.MAX_SPEED ? Constants.MAX_SPEED : (int) dist;

                for (var i = 0; i < counter; ++i)
                {
                    _aStar.AddBigWeight(path[i] as ASquare);
                }

                //if (!myShips.Any(s =>
                //    s.Key != ship.GetId() && _aStar.GetSquareI(s.Value.GetXPos()) == squareI &&
                //    _aStar.GetSquareJ(s.Value.GetYPos()) == squareJ && !_gotShipIds.Contains(s.Key)))
                //{
                //    _aStar.AddBigWeight(path[0] as ASquare);
                //}


                return new ThrustMove(ship, angle, speed);
            }
            else
            {
                var targetPoint = path[0] as ASquare;
                var targetPosition = new Position(targetPoint.CenterX, targetPoint.CenterY);

                var angle = ship.OrientTowardsInDeg(targetPosition);
                var dist = ship.GetDistanceTo(targetPosition);
                var speed = dist > Constants.MAX_SPEED ? Constants.MAX_SPEED : (int) dist + 1;
                _aStar.AddBigWeight(targetPoint);
                return new ThrustMove(ship, angle, speed);

            }


            //Log.LogMessage("Stay: " + _currentTurn + " id=" + ship.GetId());
            //_aStar.AddBigWeight(startPoint);
            ////}
            //return new ThrustMove(ship, 0, 0);

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
                Math.PI / 180.0) ?? new ThrustMove(ship, 0, 0);

            //return new ThrustMove(ship, 0, 0);
        }

        public static void Main(string[] args)
        {
            try
            {

                IDictionary<int, IList<int>> planetsShips = new Dictionary<int, IList<int>>();
                IDictionary<int, int> shipsPlanets = new Dictionary<int, int>();

                IDictionary<int, IList<int>> enemyShipsMyShips = new Dictionary<int, IList<int>>();
                IDictionary<int, int> myShipsEnemyShips = new Dictionary<int, int>();



                string name = args.Length > 0 ? args[0] : "Sharpie";

                Networking networking = new Networking();
                GameMap gameMap = networking.Initialize(name);
                _aStar = new AStar.AStar(gameMap);

                foreach (var p in gameMap.GetAllPlanets().Keys)
                {
                    planetsShips.Add(p, new List<int>());
                }

                List<Move> moveList = new List<Move>();
                for (;;)
                {
                    _gotShipIds.Clear();
                    moveList.Clear();
                    gameMap.UpdateMap(Networking.ReadLineIntoMetadata());
                    _aStar.UpdateAStar();

                    var ships = gameMap.GetAllShips();
                    var planets = gameMap.GetAllPlanets();


                    var enemyShipsList = enemyShipsMyShips.Keys.ToList();
                    foreach (var shipId in enemyShipsList)
                    {
                        if (!ships.Any(s => s.GetId() == shipId)) enemyShipsMyShips.Remove(shipId);
                    }

                    var myShipList = myShipsEnemyShips.Keys.ToList();
                    foreach (var shipId in myShipList)
                    {
                        if (!ships.Any(s => s.GetId() == shipId) &&
                            enemyShipsMyShips.ContainsKey(myShipsEnemyShips[shipId]))
                            enemyShipsMyShips[myShipsEnemyShips[shipId]].Remove(shipId);
                    }

                    var planetsList = planetsShips.Keys.ToList();
                    foreach (var planetId in planetsList)
                    {
                        if (!planets.ContainsKey(planetId))
                        {
                            planetsShips.Remove(planetId);
                        }
                    }

                    foreach (var ship in ships.Where(s => s.GetOwner() != gameMap.GetMyPlayerId()))
                    {
                        if (!enemyShipsMyShips.ContainsKey(ship.GetId()))
                        {
                            enemyShipsMyShips.Add(ship.GetId(), new List<int>());
                        }
                    }

                    foreach (var ship in gameMap.GetMyPlayer().GetShips())
                    {
                        if (ship.Value.GetDockingStatus() != Ship.DockingStatus.Undocked)
                        {
                            var square = _aStar.Table[_aStar.GetSquareI(ship.Value.GetXPos()),
                                _aStar.GetSquareJ(ship.Value.GetYPos())];
                            _aStar.AddBigWeight(square);
                        }
                    }

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
                        var id = shipsIds[i];
                        var ship = myShips[id];

                        if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
                        {
                            continue;
                        }

                        var entities = gameMap.NearbyEntitiesByDistance(ship);
                        var sortedEntities = new SortedDictionary<double, Entity>(entities);

                        bool gotShip = false;
                        foreach (var dist in sortedEntities.Keys)
                        {
                            var unit = sortedEntities[dist];
                            var enemyShip = unit as Ship;
                            if (enemyShip != null && enemyShip.GetOwner() != gameMap.GetMyPlayerId())
                            {
                                //if (enemyShip.GetDockingStatus() == Ship.DockingStatus.Undocked) break;
                                if (ship.GetDistanceTo(enemyShip) > Constants.MAX_SPEED * 3) break;

                                if (shipsPlanets.ContainsKey(id))
                                {
                                    planetsShips[shipsPlanets[id]].Remove(id);
                                    shipsPlanets.Remove(id);
                                }

                                if (myShipsEnemyShips.ContainsKey(id) && enemyShipsMyShips.ContainsKey(myShipsEnemyShips[id]))
                                {
                                    enemyShipsMyShips[myShipsEnemyShips[id]].Remove(id);
                                }

                                enemyShipsMyShips[enemyShip.GetId()].Add(id);
                                myShipsEnemyShips[id] = enemyShip.GetId();

                                gotShip = true;
                                break;
                            }
                        }

                        if (gotShip) continue;

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
                                //command = GetPlanetCommand(ship, planet, gameMap);

                                //if (command != null)
                                //{
                                if (shipsPlanets.ContainsKey(id))
                                {
                                    planetsShips[shipsPlanets[id]].Remove(id);
                                }
                                if (myShipsEnemyShips.ContainsKey(id) && enemyShipsMyShips.ContainsKey(myShipsEnemyShips[id]))
                                {
                                    enemyShipsMyShips[myShipsEnemyShips[id]].Remove(id);
                                    myShipsEnemyShips.Remove(id);
                                }


                                planetsShips[planet.GetId()].Add(id);
                                shipsPlanets[id] = planet.GetId();
                                break;
                                //}
                            }

                            var enemyShip = unit as Ship;
                            if (enemyShip != null && enemyShip.GetOwner() != gameMap.GetMyPlayerId())
                            {
                                if (shipsPlanets.ContainsKey(id))
                                {
                                    planetsShips[shipsPlanets[id]].Remove(id);
                                    shipsPlanets.Remove(id);
                                }

                                if (myShipsEnemyShips.ContainsKey(id) && enemyShipsMyShips.ContainsKey(myShipsEnemyShips[id]))
                                {
                                    enemyShipsMyShips[myShipsEnemyShips[id]].Remove(id);
                                }
                               
                                enemyShipsMyShips[enemyShip.GetId()].Add(id);
                                myShipsEnemyShips[id] = enemyShip.GetId();

                                break;
                            }

                        }
                    }

                    
                    foreach (var planetId in planetsShips.Keys)
                    {
                        var planet = planets[planetId];
                        var orderedShips = planetsShips[planetId].Select(shipId => myShips[shipId])
                            .OrderBy(s => s.GetDistanceTo(planet));

                        foreach (var ship in orderedShips)
                        {
                            var command = GetPlanetCommand(ship, planet, myShips);
                            moveList.Add(command);
                            _gotShipIds.Add(ship.GetId());
                        }
                    }

                    foreach (var enemyShipId in enemyShipsMyShips.Keys)
                    {
                        var enemyShip = gameMap.GetAllShips().SingleOrDefault(s => s.GetId() == enemyShipId);

                        var orderedShips = enemyShipsMyShips[enemyShipId]
                            .Select(shipId => myShips[shipId])
                            .OrderBy(s => s.GetDistanceTo(enemyShip));

                        foreach (var ship in orderedShips)
                        {
                            var command = GetShipCommand(ship, enemyShip, gameMap);
                            moveList.Add(command);
                            _gotShipIds.Add(ship.GetId());
                        }
                    }

                    _currentTurn++;
                    Networking.SendMoves(moveList);
                }
            }
            catch (Exception e)
            {
                Log.LogMessage("Exception: " + e.Message + " " + e.InnerException);
            }
        }
    }
}
