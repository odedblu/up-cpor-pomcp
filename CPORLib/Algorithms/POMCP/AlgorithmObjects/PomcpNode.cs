using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPORLib.Algorithms
{
    internal abstract class PomcpNode
    {
        public static int NodeCount = 0;

        public int ID = NodeCount++;

        public PomcpNode Parent;
        public Dictionary<int, PomcpNode> Children;
        public int VisitedCount;
        public double Value;
        public double SelectionValue { get; set; }
        public int SelctionVisitedCount { get; set; }
        public bool IsGoalNode { get; set; }

        public bool InexactExpansion { get; set; }


        public int ChildrenSize()
        {
            return Children.Count;
        }

        public bool IsLeaf()
        {
            return ChildrenSize() == 0;
        }
        
    }
}
