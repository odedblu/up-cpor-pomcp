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
            Children = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            Action = action;
            IsGoalNode = false;
        }

        public ActionPomcpNode(ObservationPomcpNode ObservationParentNode, Action action)
        {
            Parent = ObservationParentNode;
            Children = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            Action = action;
            IsGoalNode = false;
        }



        public void AddObservationChild(PartiallySpecifiedState partiallySpecifiedState, Formula fObservation, BeliefParticles particleFilter)
        {
            int iHashCode = ActionPomcpNode.GetObservationsHash(fObservation);
            ObservationPomcpNode nObservation = new ObservationPomcpNode(this, partiallySpecifiedState, particleFilter, fObservation);
            if (partiallySpecifiedState == null)
                nObservation.InexactExpansion = true;
            if (partiallySpecifiedState.IsGoalState())
            {
                nObservation.SelectionValue = 100;
                nObservation.VisitedCount++;
                nObservation.SelctionVisitedCount++;
                nObservation.IsGoalNode = true;
            }
            Children.Add(iHashCode, nObservation);
        }

        public ObservationPomcpNode GetObservationChild(Formula fObservation)
        {
            int iHashCode = ActionPomcpNode.GetObservationsHash(fObservation);
            ObservationPomcpNode nObservation = (ObservationPomcpNode)Children[iHashCode];
            return nObservation;
        }
        public ObservationPomcpNode AddObservationChilds(List<Predicate> Observations, PartiallySpecifiedState partiallySpecifiedState, BeliefParticles particleFilter)
        {
            if (Children.ContainsKey(GetObservationsHash(Observations)))
            {
                return (ObservationPomcpNode)Children[GetObservationsHash(Observations)];
            }
            PredicateFormula pf = new PredicateFormula(Observations.First());
            ObservationPomcpNode observationPomcpNode = new ObservationPomcpNode(this, Observations, partiallySpecifiedState, particleFilter, pf);
            int ObservationsHash = GetObservationsHash(Observations);
            Children.Add(ObservationsHash, observationPomcpNode);
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

        public static int GetObservationsHash(Formula fObservation)
        {
            if (fObservation == null)
                return 0;
            if(fObservation is PredicateFormula pf)
            {
                if (pf.Predicate.Negation)
                    return 2;
                else
                    return 1;
            }
            throw new NotImplementedException();
        }

        public string ToString()
        {
            return $"{Action.Name} | V={Value} | VC={VisitedCount} | SV={SelectionValue} | SVC={SelctionVisitedCount}";
        }
    }
}
