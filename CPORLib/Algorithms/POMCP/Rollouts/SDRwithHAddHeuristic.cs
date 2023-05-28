using System;
using System.Collections.Generic;
using System.Text;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;
using static CPORLib.Tools.Options;

namespace CPORLib.Algorithms
{
    internal class SDRwithHAddHeuristic : IRolloutPolicy
    {
        public IRolloutPolicy taggedHAdd { get; set; }  

        public SDRwithHAddHeuristic()
        {
            taggedHAdd = null;
        }

        public void UpdateTaggedDomainAndProblem(PartiallySpecifiedState pss, bool bPreconditionFailure)
        {
            pss.GetTaggedDomainAndProblem(DeadendStrategies.Lazy, bPreconditionFailure, out int cTags, out Domain dTagged, out Problem pTagged);
            taggedHAdd = new GuyHaddHeuristuc(dTagged, pTagged);
        }


        public Action ChooseAction(State s)
        {
            return taggedHAdd.ChooseAction(s);
        }
    }
}
