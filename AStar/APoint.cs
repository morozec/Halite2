using System;
using System.Collections.Generic;

namespace Halite2.AStar
{

    public abstract class APoint : IComparable<APoint>
    {
        public double G { get; set; }
        public double H { get; set; }

        public double F
        {
            get
            {
                return G + H;
            }
        }

        public APoint CameFromAPoint { get; set; }

        public abstract IEnumerable<APoint> GetNeighbors(IEnumerable<APoint> points);

        public abstract double GetHeuristicCost(APoint goal);

        public abstract double GetCost(APoint goal);

        public int CompareTo(APoint other)
        {
            return -F.CompareTo(other.F);
        }

    }
}
