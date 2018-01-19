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

        private static Move GetNavigateCommand(Ship ship, ASquare startPoint, ASquare endPoint, bool addOne)
        {
            var resPos = new Position(endPoint.CenterX, endPoint.CenterY);
            var resAngle = ship.OrientTowardsInRad(resPos);
            var resDist = ship.GetDistanceTo(resPos);
            var endStepPos = new Position(ship.GetXPos() + Constants.MAX_SPEED * Math.Cos(resAngle),
                ship.GetYPos() + Constants.MAX_SPEED * Math.Sin(resAngle));

            var intersects = new List<Tuple<int, int>>();

            var intersects1 = IntersectCalculator.GetLineSquares(
                new Position(ship.GetXPos() - Constants.SHIP_RADIUS, ship.GetYPos()),
                new Position(endStepPos.GetXPos() - Constants.SHIP_RADIUS, endStepPos.GetYPos()),
                AStar.AStar.SquareSize);
            intersects.AddRange(intersects1.Where(x =>
                x.Item1 > 0 && x.Item2 > 0 && x.Item1 < _aStar.N && x.Item2 < _aStar.M));

            var intersects2 = IntersectCalculator.GetLineSquares(new Position(ship.GetXPos() + Constants.SHIP_RADIUS, ship.GetYPos()),
                new Position(endStepPos.GetXPos() + Constants.SHIP_RADIUS, endStepPos.GetYPos()),
                AStar.AStar.SquareSize);
            intersects.AddRange(intersects2.Where(x =>
                !intersects.Contains(x) && x.Item1 > 0 && x.Item2 > 0 && x.Item1 < _aStar.N && x.Item2 < _aStar.M));

            var intersects3 = IntersectCalculator.GetLineSquares(new Position(ship.GetXPos(), ship.GetYPos() - Constants.SHIP_RADIUS),
                new Position(endStepPos.GetXPos(), endStepPos.GetYPos() - Constants.SHIP_RADIUS),
                AStar.AStar.SquareSize);
            intersects.AddRange(intersects3.Where(x =>
                !intersects.Contains(x) && x.Item1 > 0 && x.Item2 > 0 && x.Item1 < _aStar.N && x.Item2 < _aStar.M));

            var intersects4 = IntersectCalculator.GetLineSquares(new Position(ship.GetXPos(), ship.GetYPos() + Constants.SHIP_RADIUS),
                new Position(endStepPos.GetXPos(), endStepPos.GetYPos() + Constants.SHIP_RADIUS),
                AStar.AStar.SquareSize);
            intersects.AddRange(intersects4.Where(x =>
                !intersects.Contains(x) && x.Item1 > 0 && x.Item2 > 0 && x.Item1 < _aStar.N && x.Item2 < _aStar.M));

            if (ship.GetId() == 25)
            {
                Log.LogMessage("Ship pos: " + ship.GetXPos() + " " + ship.GetYPos());
                Log.LogMessage("End pos: " + endStepPos.GetXPos() + " " + endStepPos.GetYPos());
            }
            if (ship.GetId() == 25)
            {
                foreach (var item in intersects)
                {
                    Log.LogMessage(_aStar.Table[item.Item1, item.Item2].CenterX + " " +
                                   _aStar.Table[item.Item1, item.Item2].CenterY + " " +
                                   _aStar.Table[item.Item1, item.Item2].Weight);
                }
                Log.LogMessage("");
            }

            var allOk = intersects.All(item => _aStar.Table[item.Item1, item.Item2].Weight < AStar.AStar.BigWeight);

            if (allOk)
            {
                int speed;
                if (resDist >= Constants.MAX_SPEED)
                {
                    speed = Constants.MAX_SPEED;
                }
                else
                {
                    speed = (int) resDist;
                    if (addOne) speed += 1;
                }
                var angle = ship.OrientTowardsInDeg(resPos);
                foreach (var item in intersects)
                {
                    _aStar.AddBigWeight(_aStar.Table[item.Item1, item.Item2]);
                }
                return new ThrustMove(ship, angle, speed);
            }

            var path = Calculator.GetPath(startPoint, endPoint, _aStar.Squares);

            if (path.Count >= 2)
            {
                int counter = 1;
                var targetPoint = path[counter] as ASquare;
                var direction = GetDirection(startPoint, targetPoint);
                var targetPosition = new Position(targetPoint.CenterX, targetPoint.CenterY);
                var dist = ship.GetDistanceTo(targetPosition);

                while (++counter < path.Count - 1 && dist < Constants.MAX_SPEED)
                {
                    if (GetDirection(targetPoint, path[counter] as ASquare) != direction) break;
                    targetPoint = path[counter] as ASquare;
                    targetPosition = new Position(targetPoint.CenterX, targetPoint.CenterY);
                    dist = ship.GetDistanceTo(targetPosition);
                }

                var angle = ship.OrientTowardsInDeg(targetPosition);
                int speed;
                if (dist >= Constants.MAX_SPEED)
                {
                    speed = Constants.MAX_SPEED;
                }
                else
                {
                    speed = (int)dist;
                    if (addOne) speed += 1;
                }

                if (ship.GetId() == 25)
                {
                    Log.LogMessage("Path:");
                }

                for (var i = 0; i < path.Count; ++i)
                {
                    if (ship.GetId() == 25)
                    {
                        Log.LogMessage((path[i] as ASquare).CenterX + " " +
                                       (path[i] as ASquare).CenterY + " " +
                                       (path[i] as ASquare).Weight);
                    }

                    if (i < counter)
                        _aStar.AddBigWeight(path[i] as ASquare);

                }

                if (ship.GetId() == 25)
                {
                    Log.LogMessage("");
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
                if (ship.GetId() == 25)
                {
                    Log.LogMessage("Short path");
                }

                var targetPoint = path[0] as ASquare;
                var targetPosition = new Position(targetPoint.CenterX, targetPoint.CenterY);

                var angle = ship.OrientTowardsInDeg(targetPosition);
                var dist = ship.GetDistanceTo(targetPosition);
                var speed = dist > Constants.MAX_SPEED ? Constants.MAX_SPEED : (int)dist + 1;

                _aStar.AddBigWeight(ship.GetXPos(), ship.GetYPos(), Constants.SHIP_RADIUS);
                return new ThrustMove(ship, angle, speed);
            }
        }

        private static Move GetPlanetCommand(Ship ship, Planet planet)
        {
            if (ship.CanDock(planet))
            {
                //if (!myShips.Any(s =>
                //    s.Key != ship.GetId() && _aStar.GetSquareI(s.Value.GetXPos()) == squareI &&
                //    _aStar.GetSquareJ(s.Value.GetYPos()) == squareJ))
                //{
                _aStar.AddBigWeight(ship.GetXPos(), ship.GetYPos(), Constants.SHIP_RADIUS);
                //}

                return new DockMove(ship, planet);
            }

            var dockingSquares = GetPlanetDockingSquares(planet);
            var endPoint = dockingSquares.OrderBy(s => ship.GetDistanceTo(new Position(s.CenterX, s.CenterY)))
                .FirstOrDefault();

            if (endPoint == null)
            {

                Log.LogMessage("No planet " + ship.GetId());
                _aStar.AddBigWeight(ship.GetXPos(), ship.GetYPos(), Constants.SHIP_RADIUS);
                return new ThrustMove(ship, 0, 0);
            }

            var squareI = _aStar.GetSquareI(ship.GetXPos());
            var squareJ = _aStar.GetSquareJ(ship.GetYPos());
            var startPoint = _aStar.Table[squareI, squareJ];

            return GetNavigateCommand(ship, startPoint, endPoint, true);




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

            //if (isWeakEnemy && ship.GetDistanceTo(enemyShip) <= Constants.WEAPON_RADIUS)
            //{
            //    _aStar.AddBigWeight(ship.GetXPos(), ship.GetYPos(), Constants.SHIP_RADIUS);
            //    return new ThrustMove(ship, 0, 0);
            //}

            //if (!isWeakEnemy)
            //{
            //    var command = Navigation.NavigateShipTowardsTarget(gameMap,
            //        ship,
            //        enemyShip,
            //        Constants.MAX_SPEED,
            //        true,
            //        Constants.MAX_NAVIGATION_CORRECTIONS,
            //        1d);
            //    if (command != null) return command;
            //}

            //var i = _aStar.GetSquareI(destPoint.GetXPos());
            //var j = _aStar.GetSquareJ(destPoint.GetYPos());
            //var endPoint = _aStar.Table[i, j];

            //var squareI = _aStar.GetSquareI(ship.GetXPos());
            //var squareJ = _aStar.GetSquareJ(ship.GetYPos());
            //var startPoint = _aStar.Table[squareI, squareJ];

            //if (startPoint.Weight >= AStar.AStar.BigWeight || endPoint.Weight >= AStar.AStar.BigWeight) //для сокращения времени
            //{
            //    var command = Navigation.NavigateShipTowardsTarget(gameMap,
            //        ship,
            //        destPoint,
            //        Constants.MAX_SPEED,
            //        true,
            //        Constants.MAX_NAVIGATION_CORRECTIONS,
            //        Math.PI / 180.0);
            //    if (command != null) return command;
            //}

            //return GetNavigateCommand(ship, startPoint, endPoint, !isWeakEnemy);

            return Navigation.NavigateShipTowardsTarget(gameMap,
                       ship,
                       destPoint,
                       Constants.MAX_SPEED,
                       true,
                       Constants.MAX_NAVIGATION_CORRECTIONS,
                       Math.PI / 180.0) ?? new ThrustMove(ship, 0, 0);


        }

        public static void Main(string[] args)
        {
            //try
            //{

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
                    try
                    {
                        gameMap.UpdateMap(Networking.ReadLineIntoMetadata());
                    }
                    catch (Exception)
                    {
                        return;
                    }
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
                            _aStar.AddBigWeight(ship.Value.GetXPos(), ship.Value.GetYPos(), Constants.SHIP_RADIUS);
                        }
                    }

                    var myShips = gameMap.GetMyPlayer().GetShips();
                    foreach (var shipId in shipsPlanets.Keys.ToList())
                    {
                        if (myShips.All(s => s.Key != shipId) && planetsShips.ContainsKey(shipsPlanets[shipId]))
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

                        //bool gotShip = false;
                        //foreach (var dist in sortedEntities.Keys)
                        //{
                        //    var unit = sortedEntities[dist];
                        //    var enemyShip = unit as Ship;
                        //    if (enemyShip != null && enemyShip.GetOwner() != gameMap.GetMyPlayerId())
                        //    {
                        //        //if (enemyShip.GetDockingStatus() == Ship.DockingStatus.Undocked) break;
                        //        if (ship.GetDistanceTo(enemyShip) > Constants.MAX_SPEED * 3) break;
                        //        var enemyI = _aStar.GetSquareI(enemyShip.GetXPos());
                        //        var enemyJ = _aStar.GetSquareJ(enemyShip.GetYPos());
                        //        if (_aStar.Table[enemyI, enemyJ].Weight >= AStar.AStar.BigWeight) continue;

                        //        if (shipsPlanets.ContainsKey(id))
                        //        {
                        //            planetsShips[shipsPlanets[id]].Remove(id);
                        //            shipsPlanets.Remove(id);
                        //        }

                        //        if (myShipsEnemyShips.ContainsKey(id) && enemyShipsMyShips.ContainsKey(myShipsEnemyShips[id]))
                        //        {
                        //            enemyShipsMyShips[myShipsEnemyShips[id]].Remove(id);
                        //        }

                        //        enemyShipsMyShips[enemyShip.GetId()].Add(id);
                        //        myShipsEnemyShips[id] = enemyShip.GetId();

                        //        gotShip = true;
                        //        break;
                        //    }
                        //}

                        //if (gotShip) continue;

                        var hasCloseEnemyPlanets = planets.Values.Any(p => p.IsOwned() &&
                                                                           p.GetOwner() != gameMap.GetMyPlayerId() &&
                                                                           ship.GetDistanceTo(p) <=
                                                                           Constants.MAX_SPEED * 3);

                        foreach (var dist in sortedEntities.Keys)
                        {
                            var unit = sortedEntities[dist];
                            var planet = unit as Planet;
                            if (planet != null && !hasCloseEnemyPlanets &&
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
                                //var enemyI = _aStar.GetSquareI(enemyShip.GetXPos());
                                //var enemyJ = _aStar.GetSquareJ(enemyShip.GetYPos());
                                //if (_aStar.Table[enemyI, enemyJ].Weight >= AStar.AStar.BigWeight) continue;

                                if (shipsPlanets.ContainsKey(id))
                                {
                                    planetsShips[shipsPlanets[id]].Remove(id);
                                    shipsPlanets.Remove(id);
                                }

                                if (myShipsEnemyShips.ContainsKey(id) &&
                                    enemyShipsMyShips.ContainsKey(myShipsEnemyShips[id]))
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
                            var command = GetPlanetCommand(ship, planet);
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
            //}
            //catch (Exception e)
            //{
            //    Log.LogMessage("Exception: " + e.Message + " " + e.InnerException);
            //}
        }
    }
}
