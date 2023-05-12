using System;
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


namespace CPORLib.Algorithms
{
    internal class PomcpAlgorithm: PlannerBase
    {
        public double DiscountFactor { get; set; }
        public double DepthThreshold { get; set; }
        public int SimulationsThreshold { get; set; }
        public Problem Problem { get; set; }
        public ObservationPomcpNode Root { get; set; }
        public IActionSelectPolicy ActionSelectPolicy { get; set; }
        public IActionSelectPolicy FinalActionSelectPolicy { get; set; }
        public IRolloutPolicy RolloutPolicy { get; set; }  

        public Func<State, Problem, Action, double> RewardFunction { get; set; }


        public PomcpAlgorithm(double discountFactor, double depthThreshold, 
                              int simulationsThreshold, Problem problem, ObservationPomcpNode root,
                              IActionSelectPolicy finalActionSelectPolicy, IActionSelectPolicy actionSelectPolicy, IRolloutPolicy rolloutPolicy, 
                              Func<State, Problem,Action, double> rewardFunction,
                              bool ExactBelifeStateRepresentation=false) : base(problem.Domain, problem)
        {
            DiscountFactor = discountFactor;
            DepthThreshold = depthThreshold;
            SimulationsThreshold = simulationsThreshold;
            Problem = problem;
            Root = root;
            ActionSelectPolicy = actionSelectPolicy;
            FinalActionSelectPolicy = finalActionSelectPolicy;
            RolloutPolicy = rolloutPolicy;
            RewardFunction = rewardFunction;
        }

       

        public void Search(bool verbose=false)
        {
            if (verbose) Console.WriteLine("Simulating...");

            // Initial Root particle filter.
            if(Root.ParticleFilter.Size() == 0)
            {
                for(int i = 0; i < 500; i++)
                {
                    Root.ParticleFilter.AddState(Root.PartiallySpecifiedState.m_bsInitialBelief.ChooseState(true));
                }
            }
            

            for (int SimulationIndex = 0; SimulationIndex < SimulationsThreshold; SimulationIndex++)
            {
                if (verbose && SimulationIndex % 100 == 0)
                {
                    int progress = (int)((double)SimulationIndex / SimulationsThreshold * 100);
                    PrintProgressBar(progress);
                }
                Simulate(Root);
            }
            if (verbose) {
                Console.WriteLine("Done simulations.");
                PrintActionLayer(Root);
            } 
        }

        

        public void Simulate(ObservationPomcpNode Node)
        {
            // Init 
            int CurrentDepth = 0;
            State SampledState;
            // Check if node is the start root.
            if (Node.ParticleFilter.Size() == 0)
            {
                SampledState = Node.PartiallySpecifiedState.m_bsInitialBelief.ChooseState(true);
            }
            else
            {
                // Sample a fully observable state from the pomcp node's particle filter.
                SampledState = Node.ParticleFilter.GetRandomState();
            }
            SampledState.GroundAllActions();
            // Running through the tree, until getting to a leaf pomcp node.
            ObservationPomcpNode Current = Node;
            State CurrentState = SampledState;
            while (!Current.IsLeaf())
            {
                // Select next action to preform inside the tree.
                Action NextAction = ActionSelectPolicy.SelectBestAction(Current, CurrentState);


                // Get the observation's predicates of this action.
                Formula observation = null;
                if (NextAction.Observe != null && NextAction.Observe.IsTrue(CurrentState.Predicates))
                {
                    observation = NextAction.Observe;
                }
                else if(NextAction.Observe != null)
                {
                    observation = NextAction.Observe.Negate();
                }
                List<Predicate> PredicatsObservation = new List<Predicate>();
                if (observation != null)
                {
                    PredicatsObservation = observation.GetAllPredicates().ToList();
                }

                // Get Next pomcp observation node inside the tree.
                Current = GetNextObservationNode(Current,NextAction,PredicatsObservation);
                CurrentDepth += 1;

                // Check we havent met the maximal depth
                if ((Math.Pow(DiscountFactor, (double)CurrentDepth) < DepthThreshold || DiscountFactor == 0) && CurrentDepth != 0)
                {
                    //Console.WriteLine("Got max depth on search");
                    return;
                }


                // Apply action on CurrentState.
                CurrentState = CurrentState.Apply(NextAction);
                CurrentState.GroundAllActions();
            }

            // Expand node.
            ExpandNode(Current);

            // Finished run inside the tree, now do rollout.
            double Reward = MultipleRollouts(Current.ParticleFilter, CurrentDepth, 1);
            double CummulativeReward = Reward;

            // Start back propogation phase.
            while (Current != Node && !Double.IsNaN(Reward))
            {
                // Increase the action pomcp node's visited count.
                Current.Parent.VisitedCount++;
                // Increase the observation pomcp node's visited count.
                Current.VisitedCount++; 
                Current.Parent.Value += (CummulativeReward - Current.Parent.Value) / Current.Parent.VisitedCount;

                // Set the currents to their predicessors.
                Current = Current.Parent.Parent as ObservationPomcpNode; // Set the current to be the previous observation pomcp node.
                CurrentState = CurrentState.Predecessor;
                CurrentDepth -= 1;
                double CurrentReward = RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction);
                CummulativeReward = Math.Pow(DiscountFactor, CurrentDepth) * CurrentReward + CummulativeReward;// RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction) + DiscountFactor * CummulativeReward;
                
                
            }
             Current.VisitedCount++;
        }

        // Get the next observation pomcp node by action and observation.
        private ObservationPomcpNode GetNextObservationNode(ObservationPomcpNode Node, Action a, List<Predicate> ObservedPredicates)
        {
            ActionPomcpNode NextActionNode = Node.Childs[a.GetHashCode()] as ActionPomcpNode;
            ObservationPomcpNode NextObservationNode = NextActionNode.Childs[ActionPomcpNode.GetObservationsHash(ObservedPredicates)] as ObservationPomcpNode;
            return NextObservationNode;
        }

        private void ExpandNode(ObservationPomcpNode Node)
        {
            Node.VisitedCount++;
            PartiallySpecifiedState NodePartialyState = Node.PartiallySpecifiedState;
            NodePartialyState.GroundAllActions();
            /*List<PlanningAction> AvailableActionsSnapshot = new List<PlanningAction>(NodePartialyState.AvailableActions);
            //Apply all observe actions here.
            foreach (Action action in AvailableActionsSnapshot)
            {
                if (action.Effects == null && Node.ParticleFilter.IsApplicable(action))
                {
                    NodePartialyState = NodePartialyState.Apply(action, out Formula o);
                    Node.ParticleFilter = Node.ParticleFilter.Apply(action, o);
                }
            }
            // ReGroud all actions.
            NodePartialyState.GroundAllActions();
            Node.PartiallySpecifiedState = NodePartialyState;*/

            // Add all effect actions to childs
            foreach (Action action in NodePartialyState.AvailableActions)
            {
                //if (action.Effects == null) continue;
                if (NodePartialyState.IsApplicable(action) && Node.ParticleFilter.IsApplicable(action))
                {
                   
                    // Create the action node.
                    ActionPomcpNode actionPomcpNode = new ActionPomcpNode(Node, action);

                    // Add the action node to the Node childs.
                    Node.AddActionPomcpNode(actionPomcpNode);

                    Formula observation;
                    if (action.Observe != null)
                    {
                        // Create all observation nodes for this action node.
                        PartiallySpecifiedState bsNew = new PartiallySpecifiedState(Node.PartiallySpecifiedState, action);
                        PartiallySpecifiedState TrueChild = bsNew.Clone();
                        PartiallySpecifiedState FalseChild = bsNew.Clone();
                        TrueChild.GeneratingObservation = action.Observe;
                        FalseChild.GeneratingObservation = action.Observe.Negate();
                        TrueChild.AddObserved(action.Observe);
                        FalseChild.AddObserved(action.Observe.Negate());

                        // Create the particle filters
                        BelifeParticles PositiveNextParticleFilter = Node.ParticleFilter.Apply(action, action.Observe);
                        BelifeParticles NegetiveNextParticleFilter = Node.ParticleFilter.Apply(action, action.Observe.Negate());


                        // Add the observations nodes to action pomcp node's childs.
                        actionPomcpNode.AddObservationChilds(TrueChild.GeneratingObservation.GetAllPredicates().ToList(), TrueChild, PositiveNextParticleFilter);
                        actionPomcpNode.AddObservationChilds(FalseChild.GeneratingObservation.GetAllPredicates().ToList(), FalseChild, NegetiveNextParticleFilter);
                    }
                    else
                    {
                        List<Predicate> PredicatsObservation = new List<Predicate>();
                        PartiallySpecifiedState NextState = Node.PartiallySpecifiedState.Apply(action, out observation);
                        BelifeParticles NextParticleFilter = Node.ParticleFilter.Apply(action, observation);
                        actionPomcpNode.AddObservationChilds(PredicatsObservation, NextState, NextParticleFilter);
                    }
                    

                   

                }
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
                    totalScore += ForRollout(s, currentDepth);
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

            Action RolloutAction = RolloutPolicy.ChooseAction(state);
            State NextState = state.Apply(RolloutAction);
            double Reward = RewardFunction(NextState, Problem, RolloutAction);
            if (Reward > 0) return Reward;
            return Reward + DiscountFactor * Rollout(NextState, currentDepth + 1);
        }

        public double ForRollout(State state, int currentDepth)
        {
            State CurrentState = state;
            if (CurrentState == null) return -1;

            double StartStateReward = RewardFunction(CurrentState, Problem, CurrentState.GeneratingAction);
            if (StartStateReward > 0) return StartStateReward * Math.Pow(DiscountFactor, currentDepth);

            double Reward = StartStateReward * Math.Pow(DiscountFactor, currentDepth);
            currentDepth += 1;

            while (!((Math.Pow(DiscountFactor, (double)currentDepth) < DepthThreshold || DiscountFactor == 0) && currentDepth != 0))
            {
                
                Action RolloutAction = RolloutPolicy.ChooseAction(CurrentState);
                if (RolloutAction == null) return Double.MinValue;
                State NextState = CurrentState.Apply(RolloutAction);
               
                double CurrentReward = RewardFunction(NextState, Problem, RolloutAction);
                Reward += Math.Pow(DiscountFactor, currentDepth) * CurrentReward;
                if (CurrentReward > 0) { 
                    break; 
                }
                currentDepth += 1;
                State prevState = CurrentState;
                CurrentState = NextState;
            }
            return Reward;
        }



        public List<Action> FindPlan(bool verbose=false)
        {
            List<Action> Plan = new List<Action>();
            // State CurrentState = Problem.GetInitialBelief().ChooseState(true);
            PartiallySpecifiedState CurrentState = Root.PartiallySpecifiedState.Clone();
            Console.WriteLine(CurrentState.UnderlyingEnvironmentState);
            if(verbose) Console.WriteLine(string.Join(",", CurrentState.Observed.Where(predicate => !predicate.Negation)));
            CurrentState.GroundAllActions();
            //while (!Problem.IsGoalState(CurrentState.UnderlyingEnvironmentState))
            while (!CurrentState.IsGoalState())
            {
                /*
                // Apply all observe actions.
                foreach (Action a in CurrentState.AvailableActions)
                {
                    PartiallySpecifiedState observationPSS;

                    if (a.Effects == null)
                    {
                        observationPSS = CurrentState.Apply(a, out Formula o);
                        Root.ParticleFilter = Root.ParticleFilter.Apply(a,o);
                        observationPSS.m_bsInitialBelief = CurrentState.m_bsInitialBelief;
                        if (o != null)
                        {
                            // Revise belife state
                            HashSet<int> hsModified = CurrentState.m_bsInitialBelief.ReviseInitialBelief(o, CurrentState);
                            if (hsModified.Count > 0)
                            {
                                observationPSS.PropogateObservedPredicates();
                            }

                        }
                        CurrentState = observationPSS;
                    }
                }
                CurrentState.GroundAllActions();
                */

                Search(verbose);
                Action bestValidAction = null;
                double bestScore = Double.MinValue;
                ActionPomcpNode bestActionNode = null;
                foreach (PomcpNode pn in Root.Childs.Values)
                {

                    ActionPomcpNode actionNode = pn as ActionPomcpNode;
                    
                    if (actionNode.Value > bestScore)
                    {
                        bestValidAction = actionNode.Action;
                        bestScore = actionNode.Value;
                        bestActionNode = actionNode;
                    }

                }

                

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
                CurrentState.GroundAllActions();

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

                if (verbose)
                {
                    Console.WriteLine($"Selected action: {bestValidAction.Name}");
                    Console.WriteLine($"New Underline state is: {string.Join(",", CurrentState.UnderlyingEnvironmentState.Predicates.Where(predicate => !predicate.Negation))}");
                    Console.WriteLine($"New state is: {string.Join(",", CurrentState.Observed.Where(predicate => !predicate.Negation))}");
                }
            }

            return Plan;
        }

        private void UnrelevantPomcpNodeDestructor(ObservationPomcpNode Node, Action bestValidAction, List<Predicate> PredicatsObservation)
        {
            List<int> toRemoveChilds = new List<int>();
            foreach(KeyValuePair<int, PomcpNode> kvp in Node.Childs)
            {
                /*if(kvp.Key != bestValidAction.GetHashCode())
                {
                    toRemoveChilds.Add(kvp.Key);
                }*/
                toRemoveChilds.Add(kvp.Key); // check theory here delete if not working
            }
            foreach(int key in toRemoveChilds)
            {
                Node.Childs.Remove(key);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        private void PrintActionLayer(PomcpNode RootNode)
        {
            foreach(PomcpNode child in Root.Childs.Values)
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
