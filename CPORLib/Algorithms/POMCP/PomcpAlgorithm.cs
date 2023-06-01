﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static System.Collections.Specialized.BitVector32;
using System.Xml.Linq;
using CPORLib.LogicalUtilities;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using Microsoft.SolverFoundation.Services;
using CPORLib.FFCS;
using CPORLib.Tools;
using System.Resources;

namespace CPORLib.Algorithms
{
    internal class PomcpAlgorithm: PlannerBase
    {

        public bool InexactExpansion = false;
        public double DiscountFactor { get; set; }
        public double DepthThreshold { get; set; }

        public int MaxInnerDepth { get; set; }
        public int MaxOuterDepth { get; set; }

        public int SimulationsThreshold { get; set; }
        public Problem Problem { get; set; }
        public ObservationPomcpNode Root { get; set; }
        public IActionSelectPolicy ActionSelectPolicy { get; set; }
        public IActionSelectPolicy FinalActionSelectPolicy { get; set; }
        public IRolloutPolicy RolloutPolicy { get; set; }  

        public Func<State, Problem, Action, double> RewardFunction { get; set; }


        public PomcpAlgorithm(double discountFactor, double depthThreshold, 
                              int simulationsThreshold, Problem problem,
                              IActionSelectPolicy finalActionSelectPolicy, IActionSelectPolicy actionSelectPolicy, IRolloutPolicy rolloutPolicy, 
                              Func<State, Problem,Action, double> rewardFunction,
                              bool ExactBelifeStateRepresentation=false) : base(problem.Domain, problem)
        {
            MaxInnerDepth = 3;
            MaxOuterDepth = 50;


            DiscountFactor = discountFactor;
            DepthThreshold = depthThreshold;
            SimulationsThreshold = simulationsThreshold;
            Problem = problem;
            Root = new ObservationPomcpNode(new PartiallySpecifiedState(problem.GetInitialBelief()), null); 
            ActionSelectPolicy = actionSelectPolicy;
            FinalActionSelectPolicy = finalActionSelectPolicy;
            RolloutPolicy = rolloutPolicy;
            RewardFunction = rewardFunction;
            Options.ComputeCompletePlanTree = true;
        }

       

        public void Search(ObservationPomcpNode nCurrent, bool verbose=false)
        {
            if (verbose) Console.WriteLine("Simulating...");


            if (nCurrent.InexactExpansion)
                MakeExact(nCurrent);

            // Initial Root particle filter.
            if(nCurrent.ParticleFilter.Size() < 1000)
            {
                List<Action> lActions = new List<Action>();
                List<Formula> lObservations = new List<Formula>();
                PartiallySpecifiedState pssCurrent = nCurrent.PartiallySpecifiedState;
                while(pssCurrent.GeneratingAction != null)
                {
                    lActions.Add(pssCurrent.GeneratingAction);
                    lObservations.Add(pssCurrent.GeneratingObservation);
                    pssCurrent = pssCurrent.Predecessor;
                }


                for (int i = 0; i < 500; i++)
                {
                    State s = nCurrent.PartiallySpecifiedState.m_bsInitialBelief.ChooseState(true, true);
                    for(int j =  lActions.Count - 1; j >= 0; j--)
                    {
                        Action a = lActions[j];
                        Formula o = lObservations[j];
                        State sTag = s.Apply(a);
                        if (sTag == null)
                            Console.Write("*");
                        if(o != null)
                        {
                            if(!o.IsTrue(sTag.Predicates, false))
                                Console.Write("*");
                        }
                        s = sTag;
                    }
                    nCurrent.ParticleFilter.AddState(s);
                }
            }
            

            for (int SimulationIndex = 0; SimulationIndex < SimulationsThreshold; SimulationIndex++)
            {
                if (verbose && SimulationIndex % 100 == 0)
                {
                    int progress = (int)((double)SimulationIndex / SimulationsThreshold * 100);
                    PrintProgressBar(progress);
                }
                Simulate(nCurrent);
            }
            if (verbose) {
                Console.WriteLine("Done simulations.");
                PrintActionLayer(nCurrent);
            } 
        }

        private void MakeExact(ObservationPomcpNode nCurrent)
        {
            List<PomcpNode> lActions = new List<PomcpNode>(nCurrent.Children.Values);
            foreach (ActionPomcpNode nAction in lActions)
            {
                Action a = nAction.Action;
                nCurrent.PartiallySpecifiedState.ApplyOffline(a, out bool bPreconditionFailure, out PartiallySpecifiedState psTrueState, out PartiallySpecifiedState psFalseState);
                if (a.Observe != null && (psTrueState == null || psFalseState == null)) //useless observation over something that we already know
                {
                    nCurrent.RemoveChild(nAction);
                    continue;
                }
                if (bPreconditionFailure)
                {
                    nCurrent.RemoveChild(nAction);
                }
                else 
                { 
                    if (psTrueState != null)
                    {
                        ObservationPomcpNode opn = nAction.GetObservationChild(a.Observe);
                        opn.PartiallySpecifiedState = psTrueState;
                    }
                    if (psFalseState != null)
                    {
                        ObservationPomcpNode opn = nAction.GetObservationChild(a.Observe.Negate());
                        opn.PartiallySpecifiedState = psFalseState;
                    }

                }
            }
            nCurrent.InexactExpansion = false;
        }

        public int SimulationsCount = 0;
        public void Simulate(ObservationPomcpNode Node)
        {

            // Init 
            int CurrentDepth = 0;
            SimulationsCount++;
            State SampledState = null;
            // Check if node is the start root.
            if (Node.ParticleFilter.Size() == 0)
            {
                Console.WriteLine("BUGBUG");
                //SampledState = Node.PartiallySpecifiedState.m_bsInitialBelief.ChooseState(true);
            }
            else
            {
                // Sample a fully observable state from the pomcp node's particle filter.
                SampledState = Node.ParticleFilter.GetRandomState();
            }
            // Running through the tree, until getting to a leaf pomcp node.
            ObservationPomcpNode Current = Node;
            State CurrentState = SampledState;

            // if (SimulationsCount == 501)
            //    Console.WriteLine("*");

            List<Action> lActions = new List<PlanningAction>();
            List<State> lStates = new List<State>();
            List<Formula> lObservations = new List<Formula>();

            bool bDone = false;
            while (!bDone)
            {
                // Expand node.
                if (Current.IsLeaf())
                {
                    if (InexactExpansion)
                        ExpandNodeInexact(Current);
                    else
                        ExpandNode(Current);
                    bDone = true;
                }
                

                // Select next action to preform inside the tree.
                Action NextAction = ActionSelectPolicy.SelectBestAction(Current, CurrentState);

                // Apply action on CurrentState.
                State NextState = CurrentState.Apply(NextAction);
                if (NextState == null)
                    Console.WriteLine("*");

                // Get the observation's predicates of this action.
                Formula observation = null;
                if (NextAction.Observe != null)
                {
                    if (NextAction.Observe.IsTrue(NextState.Predicates, false))
                        observation = NextAction.Observe;
                    else
                        observation = NextAction.Observe.Negate();
                    //PredicatsObservation = observation.GetAllPredicates().ToList();
                }
                lStates.Add(CurrentState);
                lActions.Add(NextAction);
                lObservations.Add(observation);

                // Get Next pomcp observation node inside the tree.
                ObservationPomcpNode Next = GetNextObservationNode(Current, NextAction, observation);
                Next.ParticleFilter.AddState(NextState);
                if (Next == null)
                    Console.WriteLine("*");




                CurrentState = NextState;
                Current = Next;
                CurrentDepth += 1;
                // Check we havent met the maximal depth
                //if ((Math.Pow(DiscountFactor, (double)CurrentDepth) < DepthThreshold || DiscountFactor == 0) && CurrentDepth != 0)
                if (CurrentDepth >= MaxInnerDepth)
                {
                    //Console.WriteLine("Got max depth on search");
                    bDone = true;
                }

                FilterActions(Current, CurrentState);

            }

            

            // Finished run inside the tree, now do rollout.
            if(RolloutPolicy is SDRwithHAddHeuristic SDRRolloutPolicy)
            {
                SDRRolloutPolicy.UpdateTaggedDomainAndProblem(Current.PartiallySpecifiedState, false);
            }

            double Reward = 0;
            if (RolloutPolicy is SDRwithHAddHeuristic)
            {
                List<State> lSample = new List<State>();
                foreach (State sPossible in Current.ParticleFilter.ViewedStates.Keys)
                {
                    if (!sPossible.Equals(CurrentState))
                        lSample.Add(sPossible);
                }
                Reward = ForRollout(CurrentState, lSample, CurrentDepth);
            }
            else
            {
                Reward = ForRollout(CurrentState, CurrentDepth);
            }
            
            
            //double Reward = MultipleRollouts(Current.ParticleFilter, CurrentDepth, 1);
            Current.RolloutSum += Reward;
            Current.VisitedCount++;
            double CummulativeReward = Reward;


            //BUGBUG;//rewrite here - for observation node - max of children * gamma, for action node - (weighted) average of the children (no discount)

            
            ObservationPomcpNode opn = Current;
            ActionPomcpNode apn = null;
            State s = CurrentState.Predecessor;
            double dR = Reward;
            while(opn.Parent != null)
            {
                if(dR < -100)
                    Console.Write("*");


                apn = (ActionPomcpNode)opn.Parent;
                opn = (ObservationPomcpNode)opn.Parent.Parent;
                

                apn.VisitedCount++;
                opn.VisitedCount++;

                double dImmediateReward = -1; // problem, we may receive here the goal reward several time, because the reward here is state based. What we really want is belief-based reward. Alternatively, we can move to a goal declaration action.
                //double dImmediateReward = RewardFunction(s, Problem, apn.Action);

                dR = dImmediateReward + DiscountFactor * dR;

                double dDeltaValue = (dR - apn.Value) / apn.VisitedCount;


                apn.Value += dDeltaValue;

                
            }

            /*
            // Start back propogation phase.
            //while (Current != Node && !Double.IsNaN(Reward))
            bool bDone = false;
            while (!bDone)
            {
                // Increase the observation pomcp node's visited count.
                Current.VisitedCount++;

                if (Current.Parent == null)
                    bDone = true;
                else
                {
                    // Increase the action pomcp node's visited count.
                    Current.Parent.VisitedCount++;
                    Current.Parent.Value += (CummulativeReward - Current.Parent.Value) / Current.Parent.VisitedCount;

                    // Set the currents to their predicessors.
                    Current = Current.Parent.Parent as ObservationPomcpNode; // Set the current to be the previous observation pomcp node.
                    CurrentState = CurrentState.Predecessor;
                    CurrentDepth -= 1;
                    double CurrentReward = RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction);
                    CummulativeReward = Math.Pow(DiscountFactor, CurrentDepth) * CurrentReward + CummulativeReward;// RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction) + DiscountFactor * CummulativeReward;
                }
            }
            */
            //Current.VisitedCount++;

            //sanity check
            foreach(ActionPomcpNode n in Current.Children.Values)
            {
                if (n.Value > Current.Value)
                    Console.Write("*");
            }
        }

        private void FilterActions(ObservationPomcpNode opn, State s)
        {
            List<PomcpNode> lChildren = new List<PomcpNode>(opn.Children.Values);
            foreach(ActionPomcpNode acn in lChildren)
            {
                Action a = acn.Action;
                if (a.Preconditions != null && !a.Preconditions.IsTrue(s.Predicates, false))
                    opn.RemoveChild(acn);
            }
        }

        // Get the next observation pomcp node by action and observation.
        private ObservationPomcpNode GetNextObservationNode(ObservationPomcpNode Node, Action a, List<Predicate> ObservedPredicates)
        {
            ActionPomcpNode NextActionNode = Node.Children[a.GetHashCode()] as ActionPomcpNode;
            int iHash = ActionPomcpNode.GetObservationsHash(ObservedPredicates);
            ObservationPomcpNode NextObservationNode = NextActionNode.Children[iHash] as ObservationPomcpNode;
            return NextObservationNode;
        }

        // Get the next observation pomcp node by action and observation.
        private ObservationPomcpNode GetNextObservationNode(ObservationPomcpNode Node, Action a, Formula fObserved)
        {
            ActionPomcpNode NextActionNode = Node.Children[a.GetHashCode()] as ActionPomcpNode;
            int iHash = ActionPomcpNode.GetObservationsHash(fObserved);
            ObservationPomcpNode NextObservationNode = NextActionNode.Children[iHash] as ObservationPomcpNode;
            return NextObservationNode;
        }


        public static int Expansions = 0;
        private void ExpandNode(ObservationPomcpNode Node)
        {
            //Node.PartiallySpecifiedState.GroundAllActions();
            //foreach (Action a in Node.PartiallySpecifiedState.AvailableActions)

            //BUGBUG;//do a full expansion during simulate, use only actions that are applicable to a given state, only for the root node check true applicability

            Expansions++;

            foreach (Action a in Node.PartiallySpecifiedState.Problem.GroundedActions)
            {
                Node.PartiallySpecifiedState.ApplyOffline(a, out bool bPreconditionFailure, out PartiallySpecifiedState psTrueState, out PartiallySpecifiedState psFalseState);
                if (a.Observe != null && (psTrueState == null || psFalseState == null)) //useless observation over something that we already know
                    continue;
                ActionPomcpNode nAction = new ActionPomcpNode(a);
                if (!bPreconditionFailure)
                {
                    if (psTrueState != null)
                    {
                        BelifeParticles PositiveNextParticleFilter = Node.ParticleFilter.Apply(a, a.Observe);
                        nAction.AddObservationChild(psTrueState, a.Observe, PositiveNextParticleFilter);

                    }
                    if (psFalseState != null)
                    {
                        BelifeParticles NegativeNextParticleFilter = Node.ParticleFilter.Apply(a, a.Observe.Negate());
                        nAction.AddObservationChild(psFalseState, a.Observe.Negate(), NegativeNextParticleFilter);
                    }
                    Node.AddActionPomcpNode(nAction);

                }
            }
            
        }


        private void ExpandNodeInexact(ObservationPomcpNode Node)
        {
            Expansions++;

            Node.InexactExpansion = true;
            foreach (Action a in Problem.GroundedActions)
            {
                ActionPomcpNode nAction = new ActionPomcpNode(a);
                nAction.InexactExpansion = true;
                if (a.Observe != null)
                {
                    BelifeParticles PositiveNextParticleFilter = Node.ParticleFilter.Apply(a, a.Observe);
                    nAction.AddObservationChild(null, a.Observe, PositiveNextParticleFilter);

                    BelifeParticles NegativeNextParticleFilter = Node.ParticleFilter.Apply(a, a.Observe.Negate());
                    nAction.AddObservationChild(null, a.Observe.Negate(), NegativeNextParticleFilter);
                }
                else
                {
                    BelifeParticles NextParticleFilter = Node.ParticleFilter.Apply(a, null);
                    nAction.AddObservationChild(null, null, NextParticleFilter);
                }
                Node.AddActionPomcpNode(nAction);
            }
            
        }




        public double MultipleRollouts(BelifeParticles possiboleStates, int currentDepth, int numberOfRepets)
        {
            if (possiboleStates.Size() == 0) return Double.NaN;
            double totalScore = 0;
            foreach(State s in possiboleStates.ViewedStates.Keys)
            {
                for(int i = 0; i < numberOfRepets; i++)
                {
                    //Console.WriteLine($"Rollout={Rollout(s, currentDepth)}, ForRollout={ForRollout(s, currentDepth)}");
                    double dScore = ForRollout(s, currentDepth);
                    totalScore += dScore;
                }
            }
            return totalScore / (possiboleStates.ViewedStates.Count() * numberOfRepets);
        }

        public double Rollout(State state, int currentDepth)
        {
            if (state == null) return -1;
            if((Math.Pow(DiscountFactor, (double)currentDepth) < DepthThreshold || DiscountFactor == 0) && currentDepth != 0)
            {
                return 0;
            }

            // In case state is a goal state.
            double CurrentStateReward = RewardFunction(state, Problem, state.GeneratingAction);
            if (CurrentStateReward > 0) return CurrentStateReward;

            (Action RolloutAction, State NextState) = RolloutPolicy.ChooseAction(state);
            if (NextState == null)
                NextState = state.Apply(RolloutAction);
            double Reward = RewardFunction(NextState, Problem, RolloutAction);
            if (Reward > 0) return Reward;
            return Reward + DiscountFactor * Rollout(NextState, currentDepth + 1);
        }

        public double ForRollout(State state, int currentDepth)
        {
            State CurrentState = state;
            if (CurrentState == null) return -1;

            double StartStateReward = RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction);
            if (StartStateReward > 0) 
                return StartStateReward * Math.Pow(DiscountFactor, currentDepth);

            double Reward = StartStateReward * Math.Pow(DiscountFactor, currentDepth);
            currentDepth += 1;

            List<State> lStates = new List<State>();
            List<Action> lActions = new List<PlanningAction>();

            int iOuterDepth = 0;

            //while (!((Math.Pow(DiscountFactor, (double)currentDepth) < DepthThreshold || DiscountFactor == 0) && currentDepth != 0))
            while(iOuterDepth < MaxOuterDepth)
            {
                iOuterDepth++;

                (Action RolloutAction, State NextState) = RolloutPolicy.ChooseAction(CurrentState);

                lStates.Add(CurrentState);
                lActions.Add(RolloutAction);


                if (RolloutAction == null) 
                    return Double.MinValue;
                if(NextState == null)
                    NextState = CurrentState.Apply(RolloutAction);
                if (NextState == null) 
                    return Double.MinValue;
               
                double CurrentReward = RewardFunction(NextState, Problem, RolloutAction);


                Reward += Math.Pow(DiscountFactor, currentDepth) * CurrentReward;
                if (CurrentReward < -100 || Reward < -100)
                    Console.Write("*");

                if (CurrentReward > 0) { 
                    break; 
                }
                currentDepth += 1;
                State prevState = CurrentState;
                CurrentState = NextState;
            }
            return Reward;
        }


        public double ForRollout(State sAssumedReal, List<State> lOthers, int currentDepth)
        {
            State CurrentState = sAssumedReal;
            List<State> lCurrentOthers = new List<State>(lOthers);

            if (CurrentState == null) 
                return -1;

            double StartStateReward = RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction);
            if (StartStateReward > 0)
                return StartStateReward * Math.Pow(DiscountFactor, currentDepth);

            double Reward = StartStateReward * Math.Pow(DiscountFactor, currentDepth);
            currentDepth += 1;

            List<State> lStates = new List<State>();
            List<Action> lActions = new List<PlanningAction>();

            int iOuterDepth = 0;

            //while (!((Math.Pow(DiscountFactor, (double)currentDepth) < DepthThreshold || DiscountFactor == 0) && currentDepth != 0))
            while (iOuterDepth < MaxOuterDepth)
            {
                iOuterDepth++;

                (Action RolloutAction, State NextState, List<State> lNextStates) = RolloutPolicy.ChooseAction(CurrentState, lCurrentOthers);

                lStates.Add(CurrentState);
                lActions.Add(RolloutAction);


                if (RolloutAction == null)
                    return Double.MinValue;
                
                if (NextState == null)
                    return Double.MinValue;

                double CurrentReward = RewardFunction(NextState, Problem, RolloutAction);


                Reward += Math.Pow(DiscountFactor, currentDepth) * CurrentReward;
                if (CurrentReward < -100 || Reward < -100)
                    Console.Write("*");

                if (CurrentReward > 0)
                {
                    break;
                }
                currentDepth += 1;
                State prevState = CurrentState;
                CurrentState = NextState;
                lOthers = lNextStates;
            }
            return Reward;
        }




        public List<Action> FindPlan(bool verbose=false)
        {
            List<Action> Plan = new List<Action>();
            // State CurrentState = Problem.GetInitialBelief().ChooseState(true);
            PartiallySpecifiedState CurrentState = Root.PartiallySpecifiedState.Clone();
            State sUnderlyingState = CurrentState.UnderlyingEnvironmentState;
            Console.WriteLine(sUnderlyingState);
            if(verbose) Console.WriteLine(string.Join(",", CurrentState.Observed.Where(predicate => !predicate.Negation)));
            //while (!Problem.IsGoalState(CurrentState.UnderlyingEnvironmentState))
            ObservationPomcpNode nCurrent = Root;

            ExpandNode(Root);
            while (!CurrentState.IsGoalState())
            {
                Search(nCurrent, verbose);

                //PrintTree(nCurrent, "", 0);

                Action bestValidAction = null;
                double bestScore = Double.MinValue;
                ActionPomcpNode bestActionNode = null;
                foreach (PomcpNode pn in nCurrent.Children.Values)
                {

                    ActionPomcpNode actionNode = pn as ActionPomcpNode;
                    
                    if (actionNode.Value > bestScore)
                    {
                        bestValidAction = actionNode.Action;
                        bestScore = actionNode.Value;
                        bestActionNode = actionNode;
                    }
                }
                //BUGBUG;//problem - sometimes checking is the only child although there should be more children.
                if (bestValidAction.Name == "checking" && Plan.Count > 0 && !Plan.Last().Name.StartsWith("move"))
                {
                    PrintTree(nCurrent, "", 0);
                    //Search(nCurrent, verbose);
                }

                Plan.Add(bestValidAction);

                State sNextUnderlying = sUnderlyingState.Apply(bestValidAction);
                Formula observation = sNextUnderlying.Observe(bestValidAction.Observe);
                ObservationPomcpNode nChild = bestActionNode.GetObservationChild(observation);

                nCurrent = nChild;
                CurrentState = nCurrent.PartiallySpecifiedState;
                sUnderlyingState = sNextUnderlying;
                /*
                Formula UnusedObservation;
                PartiallySpecifiedState NextPartiallyState = CurrentState.Apply(bestValidAction, out UnusedObservation);
                //NextPartiallyState.m_bsInitialBelief = CurrentState.m_bsInitialBelief;

                Formula observation = null;
                if (bestValidAction.Observe != null)
                {
                     observation = NextPartiallyState.UnderlyingEnvironmentState.Observe(bestValidAction.Observe);

                }
                List<Predicate> PredicatsObservation = new List<Predicate>();
                if (observation != null)
                {
                    PredicatsObservation = observation.GetAllPredicates().ToList();
                    // Revise belife state
                    HashSet<int> hsModified = NextPartiallyState.m_bsInitialBelief.ReviseInitialBelief(observation, NextPartiallyState);
                }

                Plan.Add(bestValidAction);
                CurrentState = NextPartiallyState;
                //CurrentState.GroundAllActions();

                ObservationPomcpNode NextObservationPomcpNode = GetNextObservationNode(Root, bestValidAction, PredicatsObservation);

                // Update paritcle filter of next observation node. (beside the first one which is empty).
                if(Root.Parent != null)
                {
                    NextObservationPomcpNode.ParticleFilter = Root.ParticleFilter.Apply(bestValidAction, observation);
                }

                // Remove all action childs that not relevant.
                UnrelevantPomcpNodeDestructor(Root, bestValidAction, PredicatsObservation);

                // Update Root.
                Root = NextObservationPomcpNode;
                Root.PartiallySpecifiedState = CurrentState.Clone();
                UnrelevantPomcpNodeDestructor(Root, bestValidAction, PredicatsObservation); // replace with observed propagtion.
                */
                if (verbose)
                {
                    Console.WriteLine($"Selected action: {bestValidAction.Name}");
                    Console.WriteLine($"New Underline state is: {string.Join(",", sUnderlyingState.Predicates.Where(predicate => !predicate.Negation && predicate.Name.StartsWith("at")))}");
                    Console.WriteLine($"New state is: {string.Join(",", CurrentState.Observed.Where(predicate => !predicate.Negation && predicate.Name.StartsWith("at")))}");
                }
            }

            return Plan;
        }

        private void PrintTree(PomcpNode nCurrent, string sPath, int iDepth)
        {
            if(nCurrent is ObservationPomcpNode opn)           //if(nCurrent.IsLeaf())
            {
                sPath += ", " + opn.Observation;
            }
            if(nCurrent is ActionPomcpNode apn)
            {
                sPath += "," + apn.Action.Name;
                Console.WriteLine(sPath + " - " + nCurrent.Value + ", " + nCurrent.VisitedCount);
           }
            if (iDepth == MaxInnerDepth)
            {
                ObservationPomcpNode nLeaf = (ObservationPomcpNode)nCurrent;

                double v = nLeaf.RolloutSum / nLeaf.VisitedCount;
                
                Console.WriteLine(sPath + " - " + v + ", " + nCurrent.VisitedCount);
            }
            else
            {
                foreach (PomcpNode nChild in nCurrent.Children.Values)
                {
                    if (nCurrent is ObservationPomcpNode)
                        PrintTree(nChild, sPath, iDepth);
                    else
                    {
                        PrintTree(nChild, sPath, iDepth + 1);
                    }
                }
            }
        }

        private void UnrelevantPomcpNodeDestructor(ObservationPomcpNode Node, Action bestValidAction, List<Predicate> PredicatsObservation)
        {
            List<int> toRemoveChilds = new List<int>();
            foreach(KeyValuePair<int, PomcpNode> kvp in Node.Children)
            {
                /*if(kvp.Key != bestValidAction.GetHashCode())
                {
                    toRemoveChilds.Add(kvp.Key);
                }*/
                toRemoveChilds.Add(kvp.Key); // check theory here delete if not working
            }
            foreach(int key in toRemoveChilds)
            {
                Node.Children.Remove(key);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        private void PrintActionLayer(PomcpNode nCurrent)
        {
            foreach(PomcpNode child in nCurrent.Children.Values)
            {
                ActionPomcpNode actionChild = child as ActionPomcpNode;
                Console.WriteLine($"Action: {actionChild.Action.Name}, Value: {actionChild.Value}, Visits: {actionChild.VisitedCount}.");
            }
        }

        private static void PrintProgressBar(int progress)
        {
            Console.Write("\r[");
            for (int j = 0; j < 50; j++)
            {
                if (j < progress / 2)
                {
                    Console.Write("=");
                }
                else
                {
                    Console.Write(" ");
                }
            }
            Console.Write("] {0}%", progress);
        }
    }

   
}
