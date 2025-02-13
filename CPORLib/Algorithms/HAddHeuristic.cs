﻿using CPORLib.LogicalUtilities;
using CPORLib.PlanningModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Action = CPORLib.PlanningModel.PlanningAction;


namespace CPORLib.Algorithms
{
    public class HAddHeuristic
    {
        public List<Action> AllGroundedActions;
        public List<Action> GroundedActuationActions;

        public Dictionary<GroundedPredicate, HashSet<int>> ActionPreconditions;

        public Domain Domain;
        public Problem Problem;

        public HAddHeuristic(Domain d, Problem p)
        {
            Domain = d;
            Problem = p;
            AllGroundedActions = Domain.GroundAllActions(Problem, false);
            GroundedActuationActions = new List<PlanningAction>();
            ActionPreconditions = new Dictionary<GroundedPredicate, HashSet<int>>();
            int cActions = 0;
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
                    aTag.Effects = a.Effects;
                    aTag.Observe = a.Observe;
                    GroundedActuationActions.Add(aTag);
                    cActions++;
                }
            }
        }

        public double ComputeHAdd(State s)
        {
            HashSet<Predicate> hsAll = new HashSet<Predicate>();
            foreach (GroundedPredicate gp in s.Predicates)
                hsAll.Add(gp);
            List<HashSet<Predicate>> lLevels = new List<HashSet<Predicate>>();
            lLevels.Add(new HashSet<Predicate>(hsAll));
            int cLevels = 0;
            bool bDone = false;
            Dictionary<GroundedPredicate,int> dGoalCosts= new Dictionary<GroundedPredicate,int>();

            HashSet<GroundedPredicate> hsGoal = new HashSet<GroundedPredicate>();
            bDone = true;
            foreach(GroundedPredicate gp in Problem.Goal.GetAllPredicates())
            {
                hsGoal.Add(gp);
                if (hsAll.Contains(gp))
                    dGoalCosts[gp] = 0;
                else
                    bDone = false;
            }


            while(!bDone)
            {
                HashSet<int> hsActions = new HashSet<int>();
                foreach(GroundedPredicate gp in lLevels.Last())
                {
                    if (ActionPreconditions.ContainsKey(gp))
                    {
                        foreach (int iAction in ActionPreconditions[gp])
                        {
                            hsActions.Add(iAction);
                        }
                    }
                }
                HashSet<Predicate> hsNextLevel = new HashSet<Predicate>();
                foreach(int iAction in hsActions)
                {
                    Action a = GroundedActuationActions[iAction];
                    ISet<Predicate> hsPreconditions = a.Preconditions.GetAllPredicates();

                    bool bContainsAll = true;
                    foreach(GroundedPredicate gp in hsPreconditions)
                        if(!hsAll.Contains(gp))
                        {
                            bContainsAll = false;
                            break;
                        }

                    if (bContainsAll)
                    {
                        ISet<Predicate> lEffects = a.GetApplicableEffects(hsAll, true, true);
                        foreach (GroundedPredicate gpEffect in lEffects)
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
                foreach(GroundedPredicate gp in hsNextLevel)
                {
                    hsAll.Add(gp);
                }
                lLevels.Add(hsNextLevel);
                cLevels++;

                if (hsNextLevel.Count == 0)
                    bDone = true;
                if (dGoalCosts.Count == hsGoal.Count)
                    bDone = true;

            }

            if (hsGoal.Count != dGoalCosts.Count)
                return double.PositiveInfinity;

            int iSum = 0;
            foreach(int iValue in dGoalCosts.Values)
            {
                iSum += iValue;
            }

            return iSum;
        }


    }
}
