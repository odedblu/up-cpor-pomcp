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
    public interface IRolloutPolicy
    {
        public string Name();
        public (Action, State) ChooseAction(State s);
        public (Action, State, ISet<State>) ChooseAction(State s, ISet<State> l, bool bPreferRefutation);
    }
}
