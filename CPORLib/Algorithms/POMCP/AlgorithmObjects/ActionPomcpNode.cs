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
    internal class ActionPomcpNode : PomcpNode
    {

        public Action Action { get; set; }  

        public ActionPomcpNode(Action action)
        {
            Parent = null;
            Childs = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            Action = action;
        }

        public ActionPomcpNode(ObservationPomcpNode ObservationParentNode, Action action)
        {
            Parent = ObservationParentNode;
            Childs = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            Action = action;
        }

        public ObservationPomcpNode AddObservationChilds(List<Predicate> Observations, PartiallySpecifiedState partiallySpecifiedState, BelifeParticles particleFilter)
        {
            if (Childs.ContainsKey(GetObservationsHash(Observations)))
            {
                return (ObservationPomcpNode)Childs[GetObservationsHash(Observations)];
            }
            ObservationPomcpNode observationPomcpNode = new ObservationPomcpNode(this, Observations, partiallySpecifiedState, particleFilter);
            int ObservationsHash = GetObservationsHash(Observations);
            Childs.Add(ObservationsHash, observationPomcpNode);
            return observationPomcpNode;
        }

        public static int GetObservationsHash(List<Predicate> Observations)
        {
            int resultHash = 0;
            foreach (Predicate predicate in Observations)
            {
                resultHash = resultHash ^ predicate.GetHashCode();
            }
            return resultHash;
        }

        public string ToString()
        {
            return $"{Action.Name} | V={Value} | VC={VisitedCount}";
        }
    }
}
