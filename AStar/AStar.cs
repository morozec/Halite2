using System;
using System.Collections.Generic;
using Halite2.hlt;

namespace Halite2.AStar
{
    public class AStar
    {
        public static double SquareSize = Constants.MAX_SPEED/3d;
        public static double BigWeight = 999999;

        private const double StartX = 0;
        private const double StartY = 0;

        public ASquare[,] Table { get; private set; }
        public IList<ASquare> Squares { get; private set; }
        public int N { get; private set; }
        public int M { get; private set; }

        private IList<ASquare> _currentStrepBigWeights = new List<ASquare>();
        
        public AStar(GameMap gameMap)
        {
            Squares = new List<ASquare>();
            N = (int)(gameMap.GetWidth() / SquareSize);
            M = (int)(gameMap.GetHeight() / SquareSize);

            Table = new ASquare[N, M];

            for (var i = 0; i < N; ++i)
            {
                for (var j = 0; j < M; ++j)
                {
                    var square = new ASquare(
                        SquareSize,
                        StartX + i * SquareSize,
                        StartY + j * SquareSize,
                        1d,
                        i + ":" + j);

                    Squares.Add(square);

                    Table[i, j] = square;
                }
            }

            for (var i = 0; i < N; ++i)
            {
                for (var j = 0; j < M; ++j)
                {
                    var square = Table[i, j];

                    //var leftI = Math.Max(GetSquareI(square.CenterX - Constants.MAX_SPEED), 0);
                    //var rightI = Math.Min(GetSquareI(square.CenterX + Constants.MAX_SPEED), N-1);

                    //var topJ = Math.Max(GetSquareJ(square.CenterY - Constants.MAX_SPEED), 0);
                    //var bottomJ = Math.Min(GetSquareJ(square.CenterY + Constants.MAX_SPEED), M - 1);
                    
                    var neighbors = new List<ASquare>();

                    //for (var i1 = leftI; i1 <= rightI; ++i1)
                    //{
                    //    for (var j1 = topJ; j1 <= bottomJ; ++j1)
                    //    {
                    //        if (i == i1 && j == j1) continue;
                    //        if (square.GetHeuristicCost(Table[i1, j1]) <= Constants.MAX_SPEED)
                    //        {
                    //            neighbors.Add(Table[i1, j1]);
                    //        }
                    //    }
                    //}

                    if (i != 0)
                    {
                        neighbors.Add(Table[i - 1, j]);
                    }
                    if (i != N - 1)
                    {
                        neighbors.Add(Table[i + 1, j]);
                    }
                    if (j != 0)
                    {
                        neighbors.Add(Table[i, j - 1]);
                    }
                    if (j != M - 1)
                    {
                        neighbors.Add(Table[i, j + 1]);
                    }

                    if (i != 0 && j != 0)
                    {
                        neighbors.Add(Table[i - 1, j - 1]);
                    }

                    if (i != N - 1 && j != M - 1)
                    {
                        neighbors.Add(Table[i + 1, j + 1]);
                    }

                    if (i != 0 && j != M - 1)
                    {
                        neighbors.Add(Table[i - 1, j + 1]);
                    }

                    if (i != N - 1 && j != 0)
                    {
                        neighbors.Add(Table[i + 1, j - 1]);
                    }


                    square.Neighbors = neighbors;
                }
            }
            Log.LogMessage("Planet bw:");
            foreach (var p in gameMap.GetAllPlanets())
            {
                var planet = p.Value;
                for (var i = 0; i < N; ++i)
                {
                    for (var j = 0; j < M; ++j)
                    {
                        var square = Table[i, j];
                        var dist = planet.GetDistanceTo(new Position(square.CenterX, square.CenterY));
                        if (dist < planet.GetRadius() + Constants.SHIP_RADIUS * 2)
                        {
                            Log.LogMessage(square.CenterX + " " + square.CenterY);
                            square.Weight = BigWeight;
                        }

                    }
                }
            }
        }

        public void AddBigWeight(double x, double y, double radius)
        {
            var startSquareI = GetSquareI(x - radius);
            var endSquareI = GetSquareI(x + radius);
            var startSquareJ = GetSquareJ(y - radius);
            var endSquareJ = GetSquareJ(y + radius);

            for (var i = startSquareI; i <= endSquareI; ++i)
            {
                for (var j = startSquareJ; j <= endSquareJ; ++j)
                {
                    var aSquare = Table[i, j];
                    if (aSquare.Weight < BigWeight)
                    {
                        _currentStrepBigWeights.Add(aSquare);
                    }

                    aSquare.Weight = BigWeight;
                }
            }
           

        }

        public void AddBigWeight(ASquare aSquare)
        {
            if (aSquare.Weight < BigWeight)
            {
                _currentStrepBigWeights.Add(aSquare);
            }

            aSquare.Weight = BigWeight;
        }

        public void UpdateAStar()
        {
            foreach (var square in _currentStrepBigWeights)
            {
                square.Weight = 1d;
            }
            _currentStrepBigWeights = new List<ASquare>();
        }

        //public void UpdateDynamicAStar(
        //    int groupIndex, IList<Vehicle> vehicles, IDictionary<int, VehicleType> reversedGroupIndexes, MyStrategy ms)
        //{
        //    var centerX = vehicles.Average(v => v.X);
        //    var centerY = vehicles.Average(v => v.Y);

        //    var myLeftX = centerX - SquareSize / 2;
        //    var leftN = (int)(myLeftX / SquareSize);

        //    var myTopY = centerY - SquareSize / 2;
        //    var topM = (int)(myTopY / SquareSize);

        //    for (var i = 0; i < _n; ++i)
        //    {
        //        for (var j = 0; j < _m; ++j)
        //        {
        //            var square = _table[i, j];
        //            square.X = _startX + i * SquareSize;
        //            square.Y = _startY + j * SquareSize;
        //            square.Weight = 1d;
        //        }
        //    }

        //    _startSquare = _table[leftN, topM];

        //    foreach (var index in reversedGroupIndexes.Keys)
        //    {
        //        if (index == groupIndex) continue;
        //        if (!MyStrategy.IsSameTypes(reversedGroupIndexes[groupIndex], reversedGroupIndexes[index]))
        //            continue; // они друг другу не мешают

        //        var currVeichles = ms.GetVehicles(index, MyStrategy.Ownership.ALLY);
        //        if (!currVeichles.Any()) continue; // все сдохли

        //        var currCenterX = currVeichles.Average(v => v.X);
        //        var currCenterY = currVeichles.Average(v => v.Y);

        //        var currLeftX = currCenterX - SquareSize / 2;
        //        var currLeftN = (int)(currLeftX / SquareSize);

        //        var currTopY = currCenterY - SquareSize / 2;
        //        var currTopM = (int)(currTopY / SquareSize);

        //        _table[currLeftN, currTopM].Weight = BigWeight;
        //    }


        //    //if (leftN > 0 && _table[leftN - 1, topM].Weight == BigWeight)
        //    //{
        //    //    if (topM > 0) _table[leftN - 1, topM - 1].Weight = BigWeight;
        //    //    if (topM < _m - 1) _table[leftN - 1, topM + 1].Weight = BigWeight;
        //    //}

        //    //if (topM > 0 && _table[leftN, topM - 1].Weight == BigWeight)
        //    //{
        //    //    if (leftN > 0) _table[leftN - 1, topM - 1].Weight = BigWeight;
        //    //    if (leftN < _n - 1) _table[leftN + 1, topM - 1].Weight = BigWeight;
        //    //}

        //    //if (leftN < _n - 1 && _table[leftN + 1, topM].Weight == BigWeight)
        //    //{
        //    //    if (topM > 0) _table[leftN + 1, topM - 1].Weight = BigWeight;
        //    //    if (topM < _m - 1) _table[leftN + 1, topM + 1].Weight = BigWeight;
        //    //}

        //    //if (topM < _m - 1 && _table[leftN, topM + 1].Weight == BigWeight)
        //    //{
        //    //    if (leftN > 0) _table[leftN - 1, topM + 1].Weight = BigWeight;
        //    //    if (leftN < _n - 1) _table[leftN + 1, topM + 1].Weight = BigWeight;
        //    //}
        //}

        public int GetSquareI(double x)
        {
            var res = (int)((x - StartX) / SquareSize);
            if (res < 0) return 0;
            if (res > N - 1) return N - 1;
            return res;
        }

        public int GetSquareJ(double y)
        {
            var res = (int)((y - StartY) / SquareSize);
            if (res < 0) return 0;
            if (res > M - 1) return M - 1;
            return res;
        }

        //public ASquare GetSquare(double x, double y)
        //{
        //    return Table[GetSquareI(x), GetSquareJ(y)];
        //}

        public IList<APoint> GetPath(int startI, int startJ, int goalI, int goalJ)
        {
            var path = Calculator.GetPath(Table[startI, startJ], Table[goalI, goalJ], Squares);
            return path;
        }

        public ASquare GetSupportAPoint(int goalI, int goalJ)
        {
            ASquare resSquare = null;
            if (goalI > 0 && Table[goalI - 1, goalJ].Weight < BigWeight)
            {
                resSquare = Table[goalI - 1, goalJ];
            }
            else if (goalJ > 0 && Table[goalI, goalJ - 1].Weight < BigWeight)
            {
                resSquare = Table[goalI, goalJ - 1];
            }
            else if (goalI > 0 && goalJ > 0 && Table[goalI - 1, goalJ - 1].Weight < BigWeight)
            {
                resSquare = Table[goalI - 1, goalJ - 1];
            }
            return resSquare;
        }

        public IList<APoint> GetStraightPath(IList<APoint> path)
        {
            var result = new List<APoint>() {path[0]};
         
            var dx = (path[1] as ASquare).X - (path[0] as ASquare).X;
            var dy = (path[1] as ASquare).Y - (path[0] as ASquare).Y;

            for (var i = 2; i < path.Count; ++i)
            {
                var currDx = (path[i] as ASquare).X - (path[i - 1] as ASquare).X;
                var currDy = (path[i] as ASquare).Y - (path[i - 1] as ASquare).Y;
                if (dx != currDx || dy != currDy)
                {
                    result.Add(path[i-1]);
                    dx = currDx;
                    dy = currDy;
                }
            }

            result.Add(path[path.Count - 1]);
            return result;
        }
    }
}

