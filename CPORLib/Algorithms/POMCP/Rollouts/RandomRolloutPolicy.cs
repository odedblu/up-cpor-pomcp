using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;

namespace CPORLib.Algorithms
{
    internal class RandomRolloutPolicy : IRolloutPolicy
    {
        public (Action,State) ChooseAction(State s)
        {
            if(s.AvailableActions.Count() == 0)  s.GroundAllActions();
            List<Action> PossibleActions = s.AvailableActions;
            int SelectedIndex = RandomGenerator.Next(PossibleActions.Count);
            return (PossibleActions[SelectedIndex], null);
        }

        public (PlanningAction, State, ISet<State>) ChooseAction(State s, ISet<State> l, bool bPreferRefutation)
        {
            throw new NotImplementedException();
        }
    }
}
