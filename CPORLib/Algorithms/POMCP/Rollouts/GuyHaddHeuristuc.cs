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
    internal class GuyHaddHeuristuc: IRolloutPolicy
    {
        public ISet<Predicate> AlwaysConstant;

        public List<Action> AllGroundedActions;
        public List<Action> GroundedActuationActions;

        public Dictionary<GroundedPredicate, HashSet<int>> ActionPreconditions;

        public List<int> ConditionalActions;
        public Dictionary<State, HashSet<Action>> StateResultCache { get; set; }

        public Domain Domain;
        public Problem Problem;

        private bool m_bInitialized;

        public GuyHaddHeuristuc(Domain d, Problem p)
        {
            Domain = d;
            Problem = p;
            m_bInitialized = false;
        }

        public void Init()
        {
            if (m_bInitialized)
                return;

            AllGroundedActions = Problem.GroundedActions;
            GroundedActuationActions = new List<Action>();
            ActionPreconditions = new Dictionary<GroundedPredicate, HashSet<int>>();
            StateResultCache = new Dictionary<State, HashSet<Action>>();


            int cActions = 0;

            ConditionalActions = new List<int>();
            ActionPreconditions[Utilities.TRUE_PREDICATE] = new HashSet<int>();


            AlwaysConstant = new GenericArraySet<Predicate>();
            foreach (GroundedPredicate gp in Problem.Known)
            {
                if (!gp.Negation && Domain.AlwaysConstant(gp))
                    AlwaysConstant.Add(gp);
            }


            foreach (Action a in AllGroundedActions)
            {
                if (a.Observe == null)
                {
                    Action aTag = new Action(a.Name);

                    if (a.Preconditions != null)
                    {
                        CompoundFormula cfPreconditions = new CompoundFormula("and");
                        foreach (GroundedPredicate gp in a.Preconditions.GetAllPredicates())
                        {
                            if (!(Domain.AlwaysConstant(gp) && Domain.AlwaysKnown(gp)))
                            {
                                cfPreconditions.AddOperand(gp);
                                if (!ActionPreconditions.ContainsKey(gp))
                                    ActionPreconditions[gp] = new HashSet<int>();
                                ActionPreconditions[gp].Add(cActions);
                            }
                        }
                        aTag.Preconditions = cfPreconditions;
                    }
                    else
                    {
                        ActionPreconditions[Utilities.TRUE_PREDICATE].Add(cActions);
                    }
                    aTag.Effects = a.Effects;
                    aTag.Observe = a.Observe;
                    GroundedActuationActions.Add(aTag);
                    if (aTag.Effects != null && a.Effects.ContainsCondition())
                        ConditionalActions.Add(cActions);
                    cActions++;
                }
            }
            m_bInitialized = true;
        }

        public Action ChooseAction(State s)
        {
            Init();

            HashSet<Action> bestActions = new HashSet<Action>();
            if (!StateResultCache.ContainsKey(s))
            {
                Dictionary<Action, double> ActionsScores = new Dictionary<Action, double>();

                List<Action> validActions = new List<Action>();
                foreach (Action a in GroundedActuationActions)
                {
                    if (a.Preconditions == null || a.Preconditions.IsTrue(s.Predicates))
                    {
                        validActions.Add(a);
                    }
                }
                s.AvailableActions = validActions;
                foreach (Action action in validActions)
                {
                    State NextState = s.Apply(action);
                    if (NextState != null)
                    {
                        double dH = ComputeHAdd(NextState);
                        ActionsScores.Add(action, dH);
                    }
                }

                double BestActionScore = Double.MaxValue;
                foreach (KeyValuePair<Action, double> kvp in ActionsScores)
                {
                    if (kvp.Value < BestActionScore)
                    {
                        BestActionScore = kvp.Value;
                    }
                }
                foreach (KeyValuePair<Action, double> kvp in ActionsScores)
                {
                    if (kvp.Value == BestActionScore)
                    {
                        bestActions.Add(kvp.Key);
                    }
                }
                StateResultCache[s] = bestActions;
            }
            else
            {
                bestActions = StateResultCache[s];
            }

            Action BestAction = null;

            if (bestActions.Count == 0)
            {
                if (s.AvailableActions.Count() == 0)
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

        public double ComputeHAdd(State s)
        {
            Init();

            ISet<Predicate> hsAllChanging = new GenericArraySet<Predicate>();
            foreach (GroundedPredicate gp in s.ChangingPredicates)
                hsAllChanging.Add(gp);
            ISet<Predicate> hsAll = new UnifiedSet<Predicate>(AlwaysConstant, hsAllChanging);

            List<ISet<Predicate>> lLevels = new List<ISet<Predicate>>();
            lLevels.Add(new GenericArraySet<Predicate>(hsAllChanging));
            int cLevels = 0;
            bool bDone = false;
            Dictionary<GroundedPredicate, int> dGoalCosts = new Dictionary<GroundedPredicate, int>();

            ISet<Predicate> hsGoal = new GenericArraySet<Predicate>();
            bDone = true;
            foreach (GroundedPredicate gp in Problem.Goal.GetAllPredicates())
            {
                hsGoal.Add(gp);
                if (hsAll.Contains(gp))
                    dGoalCosts[gp] = 0;
                else
                    bDone = false;
            }


            while (!bDone)
            {
                HashSet<int> hsActions = new HashSet<int>();
                foreach(int iAction in ActionPreconditions[Utilities.TRUE_PREDICATE])
                    hsActions.Add(iAction);
                foreach (GroundedPredicate gp in hsAll)
                {
                    if (ActionPreconditions.ContainsKey(gp))
                    {
                        foreach (int iAction in ActionPreconditions[gp])
                        {
                            hsActions.Add(iAction);
                        }
                    }
                }
                ISet<Predicate> hsNextLevel = new GenericArraySet<Predicate>();
                hsActions.UnionWith(ConditionalActions);
                foreach (int iAction in hsActions)
                {
                    Action a = GroundedActuationActions[iAction];

                    bool bContainsAll = true;
                    if (a.Preconditions != null)
                    {
                        ISet<Predicate> hsPreconditions = a.Preconditions.GetAllPredicates();
                        foreach (GroundedPredicate gp in hsPreconditions)
                            if (!hsAll.Contains(gp))
                            {
                                bContainsAll = false;
                                break;
                            }
                    }
                    if (bContainsAll)
                    {
                       
                        Formula cf = a.GetApplicableEffects(hsAll, false);
                        foreach (GroundedPredicate gpEffect in cf.GetAllPredicates())
                        {
                            if (!hsAll.Contains(gpEffect))
                            {
                                hsNextLevel.Add(gpEffect);
                                if (hsGoal.Contains(gpEffect) && !dGoalCosts.ContainsKey(gpEffect))
                                    dGoalCosts[gpEffect] = cLevels + 1;
                            }
                        }
                    }

                }
                foreach (GroundedPredicate gp in hsNextLevel)
                {
                    hsAllChanging.Add(gp);
                }
                lLevels.Add(hsNextLevel);
                cLevels++;

                if (hsNextLevel.Count == 0)
                    bDone = true;
                if (dGoalCosts.Count == hsGoal.Count)
                    bDone = true;
            }

            if (hsGoal.Count != dGoalCosts.Count)
                return double.MaxValue;

            int iSum = 0;
            foreach (int iValue in dGoalCosts.Values)
            {
                iSum += iValue;
            }

            return iSum;
        }

    }
}
