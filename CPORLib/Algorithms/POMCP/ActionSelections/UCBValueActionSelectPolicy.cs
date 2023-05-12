using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;

namespace CPORLib.Algorithms
{
    internal class UCBValueActionSelectPolicy : IActionSelectPolicy
    {
        public double C { get; set; }


        public UCBValueActionSelectPolicy(double C)
        {
            this.C = C;
        }

        public Action SelectBestAction(PomcpNode SelectionNode, State CurrentState)
        {
            Dictionary<int, PomcpNode> Childrens = SelectionNode.Childs;
            if (Childrens.Count == 0)
            {
                throw new ArgumentException("Childrens is empty, could not select best action.");
            }
            double MaxUCBValue = Double.MinValue;
            Action BestAction = null;

            foreach (KeyValuePair<int, PomcpNode> kvp in Childrens)
            {
                PomcpNode Children = kvp.Value;
                if (!(Children is ActionPomcpNode))
                {
                    throw new ArgumentException("Try to select best action on observation pomcp node.");
                }
                double ChildrenUCBScore;
                if (Children.VisitedCount == 0)
                {
                    ChildrenUCBScore = double.MaxValue;
                }
                else
                {
                    ChildrenUCBScore = Children.Value + C * Math.Sqrt(Math.Log(SelectionNode.VisitedCount) / (double)Children.VisitedCount);
                }
                if (MaxUCBValue < ChildrenUCBScore && CurrentState.AvailableActions.Contains(((ActionPomcpNode)Children).Action))
                {
                    MaxUCBValue = ChildrenUCBScore;
                    BestAction = ((ActionPomcpNode)Children).Action;
                }
            }
            return BestAction;

        }
    }
}
