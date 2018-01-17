using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2.AStar
{
    public static class IntersectCalculator
    {
        private const double Tolerance = 1E-3;

        /// <summary>
        /// Main method to get all anchor crossing squares
        /// </summary>
        /// <param name="start">Start anchor point</param>
        /// <param name="end">End anchor point</param>
        /// <param name="squareSide">square side</param>
        /// <returns></returns>
        public static IList<Tuple<int, int>> GetLineSquares(Position start, Position end, double squareSide)
        {
            //invert anchor if start.X > end.X 
            var realStart = start.GetXPos() <= end.GetXPos() ? start : end;
            var realEnd = start.GetXPos() <= end.GetXPos() ? end : start;

            //get step coeffs depending on start and end points position
            var xCoeff = realEnd.GetXPos() > realStart.GetXPos() ? 1 : -1;
            var yCoeff = realEnd.GetYPos() > realStart.GetYPos() ? 1 : -1;

            //get left and top coords of containing start point square
            var xIndex = GetSquareIndex(realStart.GetXPos(), squareSide);
            var yIndex = GetSquareIndex(realStart.GetYPos(), squareSide);
            //var startX = xIndex * squareSide;
            //var startY = yIndex * squareSide;

            //get left and top coords of containing end point square
            var endXIndex = GetSquareIndex(realEnd.GetXPos(), squareSide);
            var endYIndex = GetSquareIndex(realEnd.GetYPos(), squareSide);
            //var endX = endXIndex * squareSide;
            //var endY = GetSquareIndex(realEnd.GetYPos(), squareSide) * squareSide;

            //add containing start point square 
            var result = new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(xIndex, yIndex)
            };
            //var x = startX;
            //var y = startY;

            while (xIndex != endXIndex) //while anchor is not finished
            {
                var newXIndex = xIndex + xCoeff;
                var newX = newXIndex * squareSide;

                //var newX = x + squareSide * xCoeff; //x coord of next x border
                var yReal = Math.Abs(end.GetXPos() - start.GetXPos()) < Tolerance
                    ? start.GetYPos()
                    : start.GetYPos() + (end.GetYPos() - start.GetYPos()) * (newX - start.GetXPos()) / (end.GetXPos() - start.GetXPos()); //y coord of next x border 

                var newYIndex = GetSquareIndex(yReal, squareSide);
                //var newY = newYIndex * squareSide; //top coord of containing yReal square 
                while (yIndex != newYIndex) //look through current anchor [(x, y), (newX, newY)] 
                {
                    yIndex += yCoeff; //y coord of next y border
                    result.Add(new Tuple<int, int>(xIndex, yIndex)); //add all squares while y != newY  
                }
                result.Add(new Tuple<int, int>(newXIndex,newYIndex)); //add final square for current anchor 
                xIndex = newXIndex;
            }


            //Log.LogMessage("y = " + y.ToString());
            {
                //final step to add squares with x: endX - squareSide < x <= endX 
                while (yIndex != endYIndex)
                {
                    yIndex += yCoeff;
                    result.Add(new Tuple<int, int>(xIndex, yIndex));
                }
            }

            return result;
        }

        /// <summary>
        /// Get containg value square index
        /// </summary>
        /// <param name="value">value</param>
        /// <param name="squareSide">sqaure side</param>
        /// <returns></returns>
        private static int GetSquareIndex(double value, double squareSide)
        {
            return (int) Math.Truncate(value / squareSide);
        }
    }
}
