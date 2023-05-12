using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPORLib.Algorithms
{
    internal abstract class PomcpNode
    {
        public PomcpNode Parent;
        public Dictionary<int, PomcpNode> Childs;
        public int VisitedCount;
        public double Value;


        public int ChildrenSize()
        {
            return Childs.Count;
        }

        public bool IsLeaf()
        {
            return ChildrenSize() == 0;
        }
        
    }
}
