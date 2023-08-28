using System;
using System.Collections.Generic;
using System.Text;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;
using static CPORLib.Tools.Options;
using System.Linq;
using CPORLib.FFCS;

namespace CPORLib.Algorithms
{
    internal class SDRwithHAddHeuristic : GuyHaddHeuristuc
    {
        public override string Name()
        {
            return "SDR";
        }


        protected Dictionary<State, Dictionary<ISet<State>,double>> HeuristicsCache;

        public SDRwithHAddHeuristic(Domain d, Problem p) : base(d,p)
        {
            HeuristicsCache = new Dictionary<State, Dictionary<ISet<State>, double>>();
        }

 
        public override (Action, State, ISet<State>) ChooseAction(State sAssumedReal, ISet<State> lOthers, bool bPreferRefutation)
        {
            Init();


            double dBestValue = double.PositiveInfinity;
            Action aBest = null;
            State sBestNext = null;
            ISet<State> lBestNext = null;
            Dictionary<Action, double> ActionsScores = null;


            if (!StateResultCache.ContainsKey(sAssumedReal))//leaving it here although for now we will not cache
            {
                ActionsScores = new Dictionary<Action, double>();

                List<Action> validActions = new List<Action>();
                foreach (Action a in GroundedActuationActions)
                {
                    bool bValid = true;
                    if(a.Preconditions != null)
                    {
                        if(a.Preconditions.IsTrue(sAssumedReal.Predicates, false))
                        {
                            foreach(State s in lOthers)
                            {
                                if(!a.Preconditions.IsTrue(s.Predicates, false))
                                {
                                    bValid = false;
                                    break;
                                }
                            }
                        }
                        else
                            bValid = false;
                    }
                    if (a.Effects == null && lOthers.Count == 0) //no need to consider sensing action if there is only one state
                        bValid = false;
                    if (bValid)
                    {
                        validActions.Add(a);
                    }
                }
                //s.AvailableActions = validActions;
                foreach (Action action in validActions)
                {
                    State NextState = sAssumedReal.Apply(action);
                    if (NextState == null)
                        Console.Write("*");


                    bool bAssumedRealValue = true;
                    Predicate pObserve = null;
                    if (action.Observe != null)
                    {
                        pObserve = ((PredicateFormula)action.Observe).Predicate;
                        bAssumedRealValue = NextState.Predicates.Contains(pObserve);
                    }

                    bool bMeaningfulAction = !NextState.Equals(sAssumedReal);
                    ISet<State> lNextOthers = new HashSet<State>();
                    foreach (State s in lOthers)
                    {
                        State sNextOther = s.Apply(action);
                        bool bConsistent = true;
                        if(action.Observe != null)
                        {
                            bool bStateValue = sNextOther.Predicates.Contains(pObserve);
                            if (bAssumedRealValue != bStateValue)
                            {
                                bConsistent = false;
                                bMeaningfulAction = true;
                            }
                        }
                        if (bConsistent)
                        {
                            if (!Equals(sNextOther, s))
                                bMeaningfulAction = true;
                            lNextOthers.Add(sNextOther);
                        }
                    }

                    //double d = ComputeHAdd(sAssumedReal, lOthers);

                    if (bMeaningfulAction)
                    {
                        double dH = 0.0;
                        if (bPreferRefutation && action.Observe != null)
                            dH = 0;
                        else
                            dH = ComputeHAdd(NextState, lNextOthers, bPreferRefutation);
                        ActionsScores.Add(action, dH);

                        if (dBestValue > dH || //cost to go heuristic, we hence need the minimal cost
                            //(dBestValue == dH && RandomGenerator.NextDouble() < 0.5)
                            (dBestValue == dH && action.Observe != null)
                            )
                        {
                            dBestValue = dH;
                            aBest = action;
                            sBestNext = NextState;
                            lBestNext = lNextOthers;
                        }
                    }
                }

                
                //StateResultCache[s] = bestActions;
            }
            
            return (aBest,sBestNext, lBestNext) ;
        }

        private bool Contains(ISet<Predicate>[] hsAll, bool[] aValidState, Predicate p)
        {
            for (int i = 0; i < hsAll.Length; i++)
            {
                if (aValidState[i])
                {
                    if (!hsAll[i].Contains(p))
                        return false;
                }
            }
            return true;
        }



        public double ComputeHAdd(State sAssumedReal, ISet<State> lOthers, bool bPreferRefutation)
        {
            if (lOthers.Count == 0)
                return ComputeHAdd(sAssumedReal);

            Init();


            if (!bPreferRefutation)
            {
                if (HeuristicsCache.TryGetValue(sAssumedReal, out Dictionary<ISet<State>, double> cache))
                {
                    if (cache.TryGetValue(lOthers, out double v))
                        return v;
                }
                else
                {
                    HeuristicsCache[sAssumedReal] = new Dictionary<ISet<State>, double>(new SetComparer());
                }
            }


            List<State> lAll = new List<State>();
            lAll.Add(sAssumedReal);

            lAll.AddRange(lOthers);

            bool[] aValidStates = new bool[lAll.Count];
            ISet<Predicate>[] hsAllChanging = new ISet<Predicate>[lAll.Count];
            ISet<Predicate>[] hsAll = new ISet<Predicate>[lAll.Count];
            List<ISet<Predicate>>[] lLevels = new List<ISet<Predicate>>[lAll.Count];

            for (int i = 0; i < lAll.Count; i++)
            {
                aValidStates[i] = true;
                hsAllChanging[i] = new GenericArraySet<Predicate>();
                State s = lAll[i];
                foreach (GroundedPredicate gp in s.ChangingPredicates)
                    hsAllChanging[i].Add(gp);
                hsAll[i] = new UnifiedSet<Predicate>(AlwaysConstant, s.FixedHiddenPredicates, hsAllChanging[i]);
                lLevels[i] = new List<ISet<Predicate>>();
                lLevels[i].Add(new GenericArraySet<Predicate>(hsAllChanging[i]));
            }

            

            int cLevels = 0;
            bool bDone = false;
            Dictionary<GroundedPredicate, int> dGoalCosts = new Dictionary<GroundedPredicate, int>();

            ISet<Predicate> hsGoal = new GenericArraySet<Predicate>();
            bDone = true;
            foreach (GroundedPredicate gp in Problem.Goal.GetAllPredicates())
            {
                hsGoal.Add(gp);

                if (Contains(hsAll, aValidStates, gp))
                    dGoalCosts[gp] = 0;
                else
                    bDone = false;
            }


            bool[] aAlreadyExecutedActions = new bool[AllGroundedActions.Count];

            while (!bDone)
            {
                HashSet<int> hsActions = new HashSet<int>();
                foreach (int iAction in AllActionPreconditions[Utilities.TRUE_PREDICATE])
                    hsActions.Add(iAction);

                UnifiedSet<Predicate> sRelevant = new UnifiedSet<Predicate>(sAssumedReal.FixedHiddenPredicates, hsAllChanging[0]);

                foreach (GroundedPredicate gp in sRelevant) 
                {
                    if (AllActionPreconditions.ContainsKey(gp))
                    {
                        foreach (int iAction in AllActionPreconditions[gp])
                        {
                            hsActions.Add(iAction);
                        }
                    }
                }
                ISet<Predicate>[] hsNextLevel = new GenericArraySet<Predicate>[lAll.Count];
                for (int i = 0; i < lAll.Count; i++)
                    hsNextLevel[i] = new GenericArraySet<Predicate>();
                hsActions.UnionWith(ConditionalActions);

                bool bInvalidatedState = false;

                foreach (int iAction in hsActions)
                {
                    Action a = AllGroundedActions[iAction];

                    //if (a.Name.Contains("p5-4"))
                    //    Console.Write("*");

                    if (aAlreadyExecutedActions[iAction] && !a.HasConditionalEffects)
                        continue;

                    bool bContainsAll = true;
                    if (a.Preconditions != null)
                    {
                        ISet<Predicate> hsPreconditions = a.Preconditions.GetAllPredicates();
                        foreach (GroundedPredicate gp in hsPreconditions)
                        {
                            if (!gp.Negation)
                            {
                                if (!Contains(hsAll, aValidStates, gp))
                                {
                                    bContainsAll = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (bContainsAll)
                    {
                        aAlreadyExecutedActions[iAction] = true;
                        for (int i = 0; i < lAll.Count; i++)
                        {
                            if (aValidStates[i])
                            {
                                ISet<Predicate> lEffects = a.GetApplicableEffects(hsAll[i], false, true);
                                foreach (GroundedPredicate gpEffect in lEffects)
                                {
                                    if (!gpEffect.Negation)
                                    {
                                        if (!hsAllChanging[i].Contains(gpEffect))
                                        {
                                            hsNextLevel[i].Add(gpEffect);
                                        }
                                    }
                                }
                            }
                        }

                        if (a.Observe != null)//maybe this should be done before all the actuation actions, beacuse they modify the state.
                        {
                            Predicate pObserve = ((PredicateFormula)a.Observe).Predicate;
                            bool bAssumedRealValue = hsAll[0].Contains(pObserve);
                            for (int i = 1; i < lAll.Count; i++)
                            {
                                if (aValidStates[i])
                                {
                                    bool bStateValue = hsAll[i].Contains(pObserve);
                                    if (bStateValue != bAssumedRealValue)
                                    {
                                        aValidStates[i] = false;
                                        bInvalidatedState = true;

                                        if (bPreferRefutation)
                                            return lLevels[0].Count() + 1;
                                    }
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < lAll.Count; i++)
                {
                    if (aValidStates[i])
                    {
                        foreach (GroundedPredicate p in hsNextLevel[i])
                        {
                            hsAllChanging[i].Add(p);

                            if (hsGoal.Contains(p) && !dGoalCosts.ContainsKey(p))
                            {
                                if (Contains(hsAllChanging, aValidStates, p))
                                {
                                    dGoalCosts[p] = cLevels + 1;
                                }
                            }
                        }
                    }
                }

                bDone = true;
                for (int i = 0; i < lAll.Count; i++)
                {
                    if (aValidStates[i])
                    {
                        lLevels[i].Add(hsNextLevel[i]);
                        if (hsNextLevel[i].Count > 0)
                            bDone = false;
                    }
                }
                if (bInvalidatedState)
                    bDone = false;

                cLevels++;


                if (dGoalCosts.Count == hsGoal.Count)
                    bDone = true;
            }

            if (hsGoal.Count != dGoalCosts.Count)
            {
                //it is not impossible to get here - it may be that in the real world we can distnguish between states, but in delete relaxation we cannot
                //this may mean that the heuristic here is meaningless
                //in the meantime, assuming no deadends, returning the number of levels - 1
                return lLevels[0].Count - 1;
            }

            int iSum = 0;
            foreach (int iValue in dGoalCosts.Values)
            {
                iSum += iValue;
            }

            foreach (bool bValid in aValidStates)
                if (!bValid)
                    iSum++;

            if (!bPreferRefutation)
                HeuristicsCache[sAssumedReal][lOthers] = iSum;

            return iSum;
        }



        public double ComputeHAddII(State sAssumedReal, ISet<State> lOthers)
        {
            if (lOthers.Count == 0)
                return ComputeHAdd(sAssumedReal);

            Init();


            if(HeuristicsCache.TryGetValue(sAssumedReal, out Dictionary<ISet<State>,double> cache))
            {
                if (cache.TryGetValue(lOthers, out double v))
                    return v;
            }
            else
            {
                HeuristicsCache[sAssumedReal] = new Dictionary<ISet<State>, double>(new SetComparer());
            }


            List<State> lAll = new List<State>();
            lAll.Add(sAssumedReal);

            lAll.AddRange(lOthers);

            bool[] aValidStates = new bool[lAll.Count];
            ISet<Predicate>[] hsAllChanging = new ISet<Predicate>[lAll.Count];
            ISet<Predicate>[] hsAll = new ISet<Predicate>[lAll.Count];
            List<ISet<Predicate>>[] lLevels = new List<ISet<Predicate>>[lAll.Count];
            ISet<Predicate> hsAgreedChanging = new GenericArraySet<Predicate>();

            for (int i = 0; i < lAll.Count; i++)
            {
                aValidStates[i] = true;
                hsAllChanging[i] = new GenericArraySet<Predicate>();
                State s = lAll[i];
                foreach (GroundedPredicate gp in s.ChangingPredicates)
                    hsAllChanging[i].Add(gp);
                hsAll[i] = new UnifiedSet<Predicate>(AlwaysConstant, hsAllChanging[i]);
                lLevels[i] = new List<ISet<Predicate>>();
                lLevels[i].Add(new GenericArraySet<Predicate>(hsAllChanging[i]));
            }

            foreach (Predicate p in hsAllChanging[0])
            {
                if (Contains(hsAllChanging, aValidStates, p))
                    hsAgreedChanging.Add(p);
            }
            ISet<Predicate> hsAgreedAll = new UnifiedSet<Predicate>(AlwaysConstant, hsAgreedChanging);

            int cLevels = 0;
            bool bDone = false;
            Dictionary<GroundedPredicate, int> dGoalCosts = new Dictionary<GroundedPredicate, int>();

            ISet<Predicate> hsGoal = new GenericArraySet<Predicate>();
            bDone = true;
            foreach (GroundedPredicate gp in Problem.Goal.GetAllPredicates())
            {
                hsGoal.Add(gp);

                if (hsAgreedAll.Contains(gp))
                    dGoalCosts[gp] = 0;
                else
                    bDone = false;
            }


            while (!bDone)
            {
                HashSet<int> hsActions = new HashSet<int>();
                foreach (int iAction in AllActionPreconditions[Utilities.TRUE_PREDICATE])
                    hsActions.Add(iAction);
                foreach (GroundedPredicate gp in hsAgreedChanging) //not sure that it is enough to check the changing predicates (+ actions with no preconditions), but I think so.
                {
                    if (AllActionPreconditions.ContainsKey(gp))
                    {
                        foreach (int iAction in AllActionPreconditions[gp])
                        {
                            hsActions.Add(iAction);
                        }
                    }
                }
                ISet<Predicate>[] hsNextLevel = new GenericArraySet<Predicate>[lAll.Count];
                for (int i = 0; i < lAll.Count; i++)
                    hsNextLevel[i] = new GenericArraySet<Predicate>();
                hsActions.UnionWith(ConditionalActions);
                foreach (int iAction in hsActions)
                {
                    Action a = AllGroundedActions[iAction];

                    bool bContainsAll = true;
                    if (a.Preconditions != null)
                    {
                        ISet<Predicate> hsPreconditions = a.Preconditions.GetAllPredicates();
                        foreach (GroundedPredicate gp in hsPreconditions)
                        {
                            if (!gp.Negation)
                            {
                                if (!hsAgreedChanging.Contains(gp))
                                {
                                    bContainsAll = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (bContainsAll)
                    {
                        for (int i = 0; i < lAll.Count; i++)
                        {
                            ISet<Predicate> lEffects  = a.GetApplicableEffects(hsAll[i], false, true);
                            foreach (GroundedPredicate gpEffect in lEffects)
                            {
                                if (!gpEffect.Negation)
                                {
                                    if (!hsAllChanging[i].Contains(gpEffect))
                                    {
                                        hsNextLevel[i].Add(gpEffect);
                                    }
                                }
                            }
                        }

                        if (a.Observe != null)//maybe this should be done before all the actuation actions, beacuse they modify the state.
                        {
                            Predicate pObserve = ((PredicateFormula)a.Observe).Predicate;
                            bool bAssumedRealValue = hsAllChanging[0].Contains(pObserve);
                            for(int i = 1; i<lAll.Count;i++)
                            {
                                bool bStateValue = hsAllChanging[i].Contains(pObserve);
                                if(bStateValue != bAssumedRealValue)
                                {
                                    aValidStates[i] = false;
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < lAll.Count; i++)
                {
                    foreach (GroundedPredicate p in hsNextLevel[i])
                    {
                        hsAllChanging[i].Add(p);

                        if (Contains(hsAllChanging, aValidStates, p))
                        {
                            //add to agreed
                            if (hsGoal.Contains(p) && !dGoalCosts.ContainsKey(p))
                                dGoalCosts[p] = cLevels + 1;
                            hsAgreedChanging.Add(p);
                        }
                    }
                }

                bDone = true;
                for (int i = 0; i < lAll.Count; i++)
                {
                    lLevels[i].Add(hsNextLevel[i]);
                    if (hsNextLevel[i].Count > 0)
                        bDone = false;
                }
                cLevels++;


                if (dGoalCosts.Count == hsGoal.Count)
                    bDone = true;
            }

            if (hsGoal.Count != dGoalCosts.Count)
            {
                //it is not impossible to get here - it may be that in the real world we can distnguish between states, but in delete relaxation we cannot
                //this may mean that the heuristic here is meaningless
                //in the meantime, assuming no deadends, returning the number of levels - 1
                return lLevels[0].Count - 1;
            }

            int iSum = 0;
            foreach (int iValue in dGoalCosts.Values)
            {
                iSum += iValue;
            }

            HeuristicsCache[sAssumedReal][lOthers] = iSum;

            return iSum;
        }


        class SetComparer : IEqualityComparer<ISet<State>>
        {
            public bool Equals(ISet<State> x, ISet<State> y)
            {
                if(x.Count != y.Count) 
                    return false;
                foreach(State s in x)
                {
                    if(!y.Contains(s)) 
                        return false;
                }
                return true;
            }

            public int GetHashCode(ISet<State> obj)
            {
                int iSum = 0;
                foreach(State s in obj)
                    iSum += s.GetHashCode();
                return iSum;
            }
        }
    }
}
