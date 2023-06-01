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
    internal interface IRolloutPolicy
    {
        (Action, State) ChooseAction(State s);
        (Action, State, List<State>) ChooseAction(State s, List<State> l);
    }
}
