using System.Collections.Generic;

namespace Halite2.AStar
{
    /// <summary>
    /// Матрица распростарения
    /// </summary>
    public class ExpansionMatrixConteiner
    {
        /// <summary>
        /// Стоимости прохода до точек сети
        /// </summary>
        public IDictionary<APoint, double> ExpansionMatrix { get; set; }
        ///// <summary>
        ///// Оптимальные пути прохода до точек сети
        ///// </summary>
        //public IDictionary<APoint, IList<APoint>> Path { get; set; } 
    }
}
