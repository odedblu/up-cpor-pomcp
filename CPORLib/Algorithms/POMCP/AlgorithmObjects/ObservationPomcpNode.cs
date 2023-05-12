using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;

namespace CPORLib.Algorithms
{
    internal class ObservationPomcpNode : PomcpNode
    {

        public BelifeParticles ParticleFilter;
        public List<Predicate> ObservedPredicates;
        public PartiallySpecifiedState PartiallySpecifiedState;

        public ObservationPomcpNode(PartiallySpecifiedState partiallySpecifiedState)
        {
            Parent = null;
            Childs = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            ParticleFilter = new BelifeParticles();
            ObservedPredicates = null;
            PartiallySpecifiedState = partiallySpecifiedState;
        }

        public ObservationPomcpNode(ActionPomcpNode ActionParentNode, List<Predicate> Observed, PartiallySpecifiedState partiallySpecifiedState, BelifeParticles particleFilter)
        {
            Parent = ActionParentNode;
            Childs = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            ParticleFilter = particleFilter;
            ObservedPredicates = new List<Predicate>(); 
            foreach (Predicate Predicate in Observed) ObservedPredicates.Add(Predicate);
            PartiallySpecifiedState = partiallySpecifiedState;
        }


        public void AddActionPomcpNode(ActionPomcpNode actionPomcpNode)
        {
            int actionHash = actionPomcpNode.Action.GetHashCode();
            if(!Childs.ContainsKey(actionHash)) Childs.Add(actionHash, actionPomcpNode);
        }

        public string ToString()
        {
            return string.Join(", ", ObservedPredicates);
        }
    }
}
