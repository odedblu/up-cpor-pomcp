using Microsoft.SolverFoundation.Solvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;

namespace CPORLib.Algorithms
{
    internal class HAddRolloutPolicy : IRolloutPolicy
    {
        public Dictionary<HashSet<Predicate>, List<Action>> DomainAvailableActionsCache { get; set; }
        public Dictionary<Tuple<int, int>, HashSet<Predicate>> NextLevelHAddCache { get; set; }

        public Dictionary<State, HashSet<Action>> StateResultCache { get; set; }

        public Dictionary<int, bool> IsApplicableCache { get; set; }

        public List<Action> AllGroundedActions { get; set; }

        public List<Action> ActuationGroundedActions { get; set; }



        public HAddRolloutPolicy(Problem p)
        {
            DomainAvailableActionsCache = new Dictionary<HashSet<Predicate>, List<Action>>(HashSet<Predicate>.CreateSetComparer());
            NextLevelHAddCache = new Dictionary<Tuple<int, int>, HashSet<Predicate>>();
            StateResultCache = new Dictionary<State, HashSet<Action>>();
            IsApplicableCache = new Dictionary<int, bool>();
            Console.WriteLine("Start grounding...");
            AllGroundedActions = p.Domain.GroundAllActions(p, false);
            ActuationGroundedActions = new List<Action>();
            foreach(Action a in AllGroundedActions)
            {
                if (a.Effects != null)
                {
                    Action newAction = new Action(a.Name);
                    newAction.Effects = a.Effects;
                    newAction.Observe = a.Observe;
                    CompoundFormula cf = new CompoundFormula("and");
                    if (a.Preconditions != null)
                    {
                        foreach (GroundedPredicate gp in a.Preconditions.GetAllPredicates())
                        {
                            if (!(p.Domain.AlwaysConstant(gp) && p.Domain.AlwaysKnown(gp)))
                            {
                                cf.AddOperand(gp);
                            }
                        }
                        newAction.Preconditions = cf;
                    }
                    else
                    {
                        newAction.Preconditions = null;
                    }

                    ActuationGroundedActions.Add(newAction);
                }
            }
            Console.WriteLine("Done grounding.");

        }

        public Action ChooseAction(State s)
        {
            HashSet<Action> bestActions = new HashSet<Action>();
            if (!StateResultCache.ContainsKey(s))
            {
                Dictionary<Action, int> ActionsScores = new Dictionary<Action, int>();
                HashSet<Predicate> GoalPredicates = (HashSet<Predicate>)s.Problem.Goal.GetAllPredicates();

                //s.GroundAllActions();
                List<Action> validActions = new List<Action>();
                foreach (Action a in ActuationGroundedActions)
                {
                    if (a.Preconditions == null || a.Preconditions.IsTrue(s.Predicates))
                    {
                        validActions.Add(a);
                    }
                }
                s.AvailableActions = validActions;
                //foreach (Action action in s.AvailableActions)
                foreach (Action action in validActions)
                {
                    ISet<Predicate> StatePredicates = new HashSet<Predicate>(s.Predicates);
                    if (action.Effects != null)
                    {

                        if (action.HasConditionalEffects)
                        {
                            State NextState = s.Apply(action);
                            //StatePredicates = new HashSet<Predicate>(NextState.Predicates);
                            StatePredicates.UnionWith(NextState.Predicates);
                        }
                        else
                        {
                            StatePredicates.UnionWith(action.Effects.GetAllPredicates());
                        }

                    }

                    /*if (action.Observe != null) 
                    {
                        StatePredicates.UnionWith(action.Observe.GetAllPredicates());
                        StatePredicates.UnionWith(action.Observe.Negate().GetAllPredicates());
                    }*/
                    Dictionary<Predicate, int> ActionPredicatesScores = GetGoalPredicatesHaddLevel(GoalPredicates, GetRelavantPredicates(StatePredicates, s.Problem) , s.Problem.Domain);
                    int hAddSum;
                    try
                    {
                        hAddSum = ActionPredicatesScores.Sum(x => x.Value);
                    }
                    catch (OverflowException ex)
                    {
                        hAddSum = int.MaxValue;
                    }

                    ActionsScores.Add(action, hAddSum);
                }
                int BestActionScore = int.MaxValue;
                foreach (KeyValuePair<Action, int> kvp in ActionsScores)
                {
                    if (kvp.Value < BestActionScore)
                    {
                        BestActionScore = kvp.Value;
                    }
                }
                foreach (KeyValuePair<Action, int> kvp in ActionsScores)
                {
                    if (kvp.Value == BestActionScore)
                    {
                        bestActions.Add(kvp.Key);
                    }
                }
                if(s.AvailableActions.Count() == 0)
                {
                    StateResultCache[s] = new HashSet<Action>();
                }
                else
                {
                    StateResultCache[s] = bestActions;
                }
            }
            else
            {
                bestActions = StateResultCache[s];
            }

            Action BestAction = null;

            if (bestActions.Count == 0)
            {
                if(s.AvailableActions.Count() == 0)
                {
                    return null;
                }
                int selectedIndex = RandomGenerator.Next(s.AvailableActions.Count());
                BestAction = s.AvailableActions.ElementAt(selectedIndex);
            }
            else
            {
                int selectedIndex = RandomGenerator.Next(bestActions.Count());
                BestAction = bestActions.ElementAt(selectedIndex);
            }
            return BestAction;
        }

        private Dictionary<Predicate, int> GetGoalPredicatesHaddLevel(ISet<Predicate> GoalPredicates, HashSet<Predicate> StartPredicates, Domain domain)
        {
            Dictionary<Predicate, int> GoalPredicatesHadd = new Dictionary<Predicate, int>();
            List<Action> actions = GetCachedAvailableActions(domain, StartPredicates);
            int currentLevel = 0;
            foreach (Predicate goalPredicate in GoalPredicates)
            {
                if (StartPredicates.Contains(goalPredicate)) GoalPredicatesHadd.Add(goalPredicate, currentLevel);
            }
            HashSet<Predicate> nextLayerPredicats = GetNextLevelHaddPredicates(StartPredicates, actions);
            while (GoalPredicatesHadd.Count() != GoalPredicates.Count() && nextLayerPredicats.Count() != StartPredicates.Count())
            {
                foreach (Predicate goalPredicate in GoalPredicates)
                {
                    if (GoalPredicatesHadd.ContainsKey(goalPredicate)) continue;
                    if (nextLayerPredicats.Contains(goalPredicate)) GoalPredicatesHadd.Add(goalPredicate, currentLevel + 1);
                }
                StartPredicates = nextLayerPredicats;
                actions = GetCachedAvailableActions(domain, StartPredicates);
                nextLayerPredicats = GetNextLevelHaddPredicates(StartPredicates, actions);
                currentLevel++;
            }
            foreach (Predicate goalPredicate in GoalPredicates)
            {
                if (!GoalPredicatesHadd.ContainsKey(goalPredicate)) GoalPredicatesHadd.Add(goalPredicate, int.MaxValue);
            }
            return GoalPredicatesHadd;
        }

        private HashSet<Predicate> GetNextLevelHaddPredicates(HashSet<Predicate> StartPredicates, List<Action> AvailableActions)
        {
            //Tuple<int, int> CacheKey = new Tuple<int, int>(GetEnumarbleHashCode(StartPredicates), GetEnumarbleHashCode(AvailableActions));
            //if(NextLevelHAddCache.ContainsKey(CacheKey)) return NextLevelHAddCache[CacheKey];
            HashSet<Predicate> NextLevelPredicates = new HashSet<Predicate>(StartPredicates);
            foreach (Action action in AvailableActions)
            {

                ISet<Predicate> influences = new HashSet<Predicate>();
                // In case we want observation will also effect the hAdd value.
                /*
                HashSet<Predicate> observedPredicates = action.Observe.GetAllPredicates();
                if (observedPredicates != null) 
                {
                    influences.UnionWith(observedPredicates);
                    foreach (Predicate predicate in observedPredicates) influences.Add(predicate.Negate());
                } 
                */
                ISet<Predicate> effectPredicates = new HashSet<Predicate>();
                if (action.Effects != null)
                {
                    if (action.HasConditionalEffects)
                    {
                        effectPredicates = GetConditionalEffects(StartPredicates, action);
                    }
                    else
                    {
                        effectPredicates = action.Effects.GetAllPredicates();
                    }

                }

                if (effectPredicates != null) influences.UnionWith(effectPredicates);

                foreach (Predicate influencePredicate in influences)
                {
                    if (!NextLevelPredicates.Contains(influencePredicate))
                    {
                        NextLevelPredicates.Add(influencePredicate);
                    }
                }

            }
            //NextLevelHAddCache[CacheKey] = NextLevelPredicates;
            return NextLevelPredicates;
        }

        private bool IsApplicableAction(ISet<Predicate> KnownPredicates, Action action)
        {
            if (action.Preconditions == null) return true;

            // Add cache
            int CacheKey = KnownPredicates.GetHashCode() + action.GetHashCode();
            if (IsApplicableCache.ContainsKey(CacheKey))
            {
                return IsApplicableCache[CacheKey];
            }

            foreach (Predicate preconditionPredicate in action.Preconditions.GetAllPredicates())
            {
                if (!KnownPredicates.Contains(preconditionPredicate))
                {
                    IsApplicableCache[CacheKey] = false;
                    return false;
                }
            }
            IsApplicableCache[CacheKey] = true;
            return true;
        }

        private List<Action> GetCachedAvailableActions(Domain domain, HashSet<Predicate> availablePredicates)
        {
            if (DomainAvailableActionsCache.ContainsKey(availablePredicates))
            {
                return DomainAvailableActionsCache[availablePredicates];
            }
            else
            {
                DateTime s = DateTime.Now;
                List<Action> validActions = new List<Action>();
                foreach (Action a in ActuationGroundedActions)
                {
                    if (a.Preconditions == null || IsApplicableAction(availablePredicates,a)) // Only apply on delete relaxion.
                    {
                        validActions.Add(a);
                    }
                }
                //List<Action> availableActions = domain.GroundAllActions(availablePredicates, false);
                List<Action> availableActions = validActions;
                DomainAvailableActionsCache.Add(availablePredicates, availableActions);
                return availableActions;
            }
        }

        private int GetEnumarbleHashCode<T>(IEnumerable<T> values)
        {
            int hash = 19;
            foreach (var subObject in values)
            {
                hash = hash * 31 + subObject.GetHashCode();
            }
            return hash;
        }

        /*private HashSet<Predicate> GetConditionalEffects(HashSet<Predicate> StatePredicates, Action a)
        {
            HashSet<Predicate> conditionalEffects = new HashSet<Predicate>();
            List<Action> CondtionalSplitedActions = a.SplitConditionalEffects(out CompoundFormula f);
            foreach (Action CondtionalSplitedAction in CondtionalSplitedActions)
            {
                if (CondtionalSplitedAction.Preconditions.RemoveNegations().IsTrue(StatePredicates, false))
                {
                    conditionalEffects.UnionWith(CondtionalSplitedAction.Effects.GetAllPredicates());
                }
            }
            return conditionalEffects;
        }*/

        private HashSet<Predicate> GetConditionalEffects(HashSet<Predicate> StatePredicates, Action a)
        {
            HashSet<Predicate> conditionalEffects = new HashSet<Predicate>(); ;

            if(a.Effects is CompoundFormula cf)
            {
                conditionalEffects = GetConditionalEffects(StatePredicates, cf);
            }
            if(a.Effects is ProbabilisticFormula pf)
            {
                foreach (Formula of in pf.Options)
                {
                    if (of is CompoundFormula ocf)
                    {
                        conditionalEffects.UnionWith(GetConditionalEffects(StatePredicates, ocf));
                    }
                }
            }

            return conditionalEffects;
        }

        private HashSet<Predicate> GetConditionalEffects(ISet<Predicate> StatePredicates, CompoundFormula effects)
        {
            HashSet<Predicate> conditionalEffects = new HashSet<Predicate>();
            foreach (Formula f in effects.Operands)
            {
                if (f is CompoundFormula cf)
                {
                    if (cf.Operator == "when")
                    {
                        if (cf.Operands[0].IsTrue(StatePredicates))
                        {
                            conditionalEffects.UnionWith(cf.Operands[1].GetAllPredicates());
                        }
                    }
                }
            }
            return conditionalEffects;
        }

        private HashSet<Predicate> GetRelavantPredicates(IEnumerable<Predicate> predicates, Problem problem)
        {
            HashSet<Predicate> ChangingPredicates = new HashSet<Predicate>();
            foreach (Predicate p in predicates)
            {
                if (!(problem.Domain.AlwaysConstant(p) && problem.Domain.AlwaysKnown(p)))
                {
                    ChangingPredicates.Add(p);
                }
            }
            return ChangingPredicates;
        }

        (PlanningAction, State) IRolloutPolicy.ChooseAction(State s)
        {
            throw new NotImplementedException();
        }

        public (PlanningAction, State, ISet<State>) ChooseAction(State s, ISet<State> l, bool bPreferRefutation)
        {
            throw new NotImplementedException();
        }
    }
}
