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
    internal class RandomRolloutPolicy : IRolloutPolicy
    {
        public Action ChooseAction(State s)
        {
            Random random = new Random();
            if(s.AvailableActions.Count() == 0)  s.GroundAllActions();
            List<Action> PossibleActions = s.AvailableActions;
            int SelectedIndex = random.Next(PossibleActions.Count);
            return PossibleActions[SelectedIndex];
        }
    }
}
