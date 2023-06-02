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
    internal class GoalPredicateAddRolloutPolicy : IRolloutPolicy
    {
        public Action ChooseAction(State s)
        {
            Dictionary<Action, int> ActionScores = new Dictionary<Action, int>();
            ISet<Predicate> GoalPredicates = s.Problem.Goal.GetAllPredicates();
            s.GroundAllActions();
            int MaxActionGoalPredicatesCount = 0;
            foreach (Action action in s.AvailableActions)
            {
                if (action.Preconditions != null && !action.Preconditions.IsTrue(s.Predicates)) continue;
                int ActionGoalPredicatesCount = 0;
                if (action.Effects != null)
                {
                    ISet<Predicate> ActionEffects = action.Effects.GetAllPredicates();
                    foreach (Predicate GoalPredicate in GoalPredicates)
                    {
                        if (ActionEffects.Contains(GoalPredicate))
                        {
                            ActionGoalPredicatesCount++;
                        }
                    }
                }
                ActionScores.Add(action, ActionGoalPredicatesCount);
                if(ActionGoalPredicatesCount > MaxActionGoalPredicatesCount)
                {
                    MaxActionGoalPredicatesCount = ActionGoalPredicatesCount;
                }
            }

            
            IEnumerable<Action> PossibleActions = ActionScores.Where(pair => pair.Value == MaxActionGoalPredicatesCount).Select(pair => pair.Key);

            int BestActionsCount = PossibleActions.Count();
            int SelectedIndex;
            if (BestActionsCount != 0)
            {
                SelectedIndex = RandomGenerator.Next(0, BestActionsCount);
                return PossibleActions.ElementAt(SelectedIndex);
            }
            SelectedIndex = RandomGenerator.Next(0, ActionScores.Count());
            return ActionScores.ElementAt(SelectedIndex).Key;
        }

        public (PlanningAction, State, ISet<State>) ChooseAction(State s, ISet<State> l)
        {
            throw new NotImplementedException();
        }

        (PlanningAction, State) IRolloutPolicy.ChooseAction(State s)
        {
            throw new NotImplementedException();
        }
    }
}
