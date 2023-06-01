using System;
using System.Collections.Generic;
using System.Text;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;
using static CPORLib.Tools.Options;
using System.Linq;

namespace CPORLib.Algorithms
{
    internal class SDRwithHAddHeuristic : GuyHaddHeuristuc
    {
        //public IRolloutPolicy taggedHAdd { get; set; }  

        public SDRwithHAddHeuristic(Domain d, Problem p) : base(d,p)
        {
            //taggedHAdd = null;
        }

        public void UpdateTaggedDomainAndProblem(PartiallySpecifiedState pss, bool bPreconditionFailure)
        {
            //pss.GetTaggedDomainAndProblem(DeadendStrategies.Lazy, bPreconditionFailure, out int cTags, out Domain dTagged, out Problem pTagged);
            //taggedHAdd = new GuyHaddHeuristuc(dTagged, pTagged);
        }



        public override (Action, State, List<State>) ChooseAction(State sAssumedReal, List<State> lOthers)
        {
            Init();


            double dBestValue = double.NegativeInfinity;
            Action aBest = null;
            State sBestNext = null;
            List<State> lBestNext = null;
            
            if (!StateResultCache.ContainsKey(sAssumedReal))//leaving it here although for now we will not cache
            {
                Dictionary<Action, double> ActionsScores = new Dictionary<Action, double>();

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

                    Predicate pObserve = null; //assuming here that we observe only facts, not forumlas

                    if (action.Observe != null)
                    {
                        pObserve = ((PredicateFormula)action.Observe).Predicate;
                        if (!NextState.Predicates.Contains(pObserve))
                            pObserve = pObserve.Negate();
                    }

                    List<State> lNextOthers = new List<State>();
                    foreach (State s in lOthers)
                    {
                        State sNextOther = s.Apply(action);
                        bool bConsistent = true;
                        if(action.Observe != null)
                        {
                            if(pObserve.Negation == false && !sNextOther.Predicates.Contains(pObserve))
                               bConsistent = false;
                            if (pObserve.Negation == true && sNextOther.Predicates.Contains(pObserve))
                                bConsistent = false;

                        }
                        if(bConsistent)
                            lNextOthers.Add(sNextOther);
                    }
                    
                    double dH = ComputeHAdd(NextState, lNextOthers);
                    ActionsScores.Add(action, dH);

                    if(dBestValue < dH)
                    {
                        dBestValue = dH;
                        aBest = action;
                        sBestNext = NextState;
                        lBestNext = lNextOthers;

                    }
                }

                
                //StateResultCache[s] = bestActions;
            }
            
            return (aBest,sBestNext, lBestNext) ;
        }

        private bool Contains(ISet<Predicate>[] hsAll, Predicate p)
        {
            foreach (ISet<Predicate> s in hsAll)
                if (!s.Contains(p))
                    return false;
            return true;
        }

        public double ComputeHAdd(State sAssumedReal, List<State> lOthers)
        {
            if (lOthers.Count == 0)
                return ComputeHAdd(sAssumedReal);

            Init();
            List<State> lAll = new List<State>();
            lAll.Add(sAssumedReal);
            if (lOthers != null)
                lAll.AddRange(lOthers);

            ISet<Predicate>[] hsAllChanging = new ISet<Predicate>[lAll.Count];
            ISet<Predicate>[] hsAll = new ISet<Predicate>[lAll.Count];
            List<ISet<Predicate>>[] lLevels = new List<ISet<Predicate>>[lAll.Count];
            ISet<Predicate> hsAgreedChanging = new GenericArraySet<Predicate>();

            for (int i = 0; i < lAll.Count; i++)
            {
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
                if (Contains(hsAllChanging, p))
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
                            Formula cf = a.GetApplicableEffects(hsAll[i], false);
                            foreach (GroundedPredicate gpEffect in cf.GetAllPredicates())
                            {
                                if (!hsAllChanging[i].Contains(gpEffect))
                                {
                                    hsNextLevel[i].Add(gpEffect);
                                    hsAllChanging[i].Add(gpEffect);
                                }
                            }
                        }

                        if (a.Observe != null)
                        {
                            Console.Write("*");
                        }
                    }
                }

                foreach (GroundedPredicate p in hsNextLevel[0])
                {
                    if (Contains(hsAllChanging, p))
                    {
                        //add to agreed
                        if (hsGoal.Contains(p) && !dGoalCosts.ContainsKey(p))
                            dGoalCosts[p] = cLevels + 1;
                        hsAgreedChanging.Add(p);
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
