using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;

namespace CPORLib.Algorithms
{
    public class BeliefParticles
    {
        public Dictionary<State, double> ViewedStates;

        public BeliefParticles()
        {
            this.ViewedStates = new Dictionary<State, double>();
        }


        /// <summary>
        /// Get the size of the belife particle.
        /// </summary>
        /// <returns> Size of the belife particle. </returns>
        public double Size()
        {
            double newParticleSize = 0;
            foreach (KeyValuePair<State, double> stateFrequency in this.ViewedStates)
            {
                newParticleSize += stateFrequency.Value;
            }
            return newParticleSize;
        }


        /// <summary>
        /// Add state to the belife particle.
        /// </summary>
        /// <param name="s"> State to add to belife particle. </param>
        public void AddState(State s)
        {
            if(s == null)
                return;
            s.ClearOptionPredicates();
            if (this.ViewedStates.ContainsKey(s))
            {
                this.ViewedStates[s] += 1.0;
            }
            else
            {
                this.ViewedStates[s] = 1.0;
            }
        }


        /// <summary>
        /// Get random state from the belife particle, if the belife particle is empty, return null.
        /// </summary>
        /// <returns> Chosen state from the belife particle. </returns>
        public State GetRandomState()
        {
            double cummlativeProbability = 0;
            List<Tuple<double, State>> StateProbabilities = new List<Tuple<double, State>>();
            foreach (KeyValuePair<State, double> stateFrequency in this.ViewedStates)
            {
                double StateProbability = stateFrequency.Value / this.Size();
                StateProbabilities.Add(new Tuple<double, State>(cummlativeProbability + StateProbability, stateFrequency.Key));
                cummlativeProbability += StateProbability;
            }
            double RandomRoll = RandomGenerator.NextDouble();
            foreach (var StateProbability in StateProbabilities)
            {
                if (StateProbability.Item1 > RandomRoll)
                    return StateProbability.Item2;
            }
            return null;
        }

        public BeliefParticles Apply(Action a, Formula observationPredicats)
        {
            // Init the return value.
            BeliefParticles NextBelifePatricle = new BeliefParticles();


            // Foreach particle apply the action and update the new belife particle to have the same frequency.
            foreach (KeyValuePair<State, double> stateFrequency in this.ViewedStates)
            {
                // Handle Probabilty actions
                if (a.Effects != null && a.Effects.ContainsProbabilisticEffects())
                {
                    double UnchangeStateProbability = 1; // Calculate whats the probabilty of staying in the same state.
                    if(a.Effects is CompoundFormula && ((CompoundFormula)a.Effects).Operands.Count() == 1)
                    {
                        a.Effects = ((CompoundFormula)a.Effects).Operands[0];
                    }
                    ProbabilisticFormula probabilisticEffects = a.Effects as ProbabilisticFormula;
                    for (int i = 0; i < probabilisticEffects.Probabilities.Count; i++)
                    {
                        double ChosenEffectsProbability = probabilisticEffects.Probabilities[i];
                        UnchangeStateProbability -= ChosenEffectsProbability;
                        double ProbabilityEffectRatioCount = ChosenEffectsProbability * stateFrequency.Value;
                        Action chosenAction = a.RemoveNonDeterminismByOptionIndex(i);
                        UpdateNextParticle(chosenAction, observationPredicats, NextBelifePatricle, stateFrequency, ProbabilityEffectRatioCount);

                    }
                    // Add to the praticle the case of staying in the same state.
                    double UnchangedStateProbabilityEffectRatioCount = UnchangeStateProbability * stateFrequency.Value;
                    if (UnchangedStateProbabilityEffectRatioCount > 0)
                    {
                        Action stayAction = a.RemoveNonDeterminismByOptionIndex(-1);
                        UpdateNextParticle(stayAction, observationPredicats, NextBelifePatricle, stateFrequency, UnchangedStateProbabilityEffectRatioCount);
                    }
                }
                    

                else
                {
                    UpdateNextParticle(a, observationPredicats, NextBelifePatricle, stateFrequency, stateFrequency.Value);
                }
                
            }

            return NextBelifePatricle;
        }

        private void UpdateNextParticle(Action a, Formula observationPredicats, BeliefParticles NextBelifePatricle, KeyValuePair<State, double> stateFrequency, double ProbabilityEffectRatioCount)
        {
            State NewState = stateFrequency.Key.Apply(a);
            if (NewState != null)
            {
                if (a.Observe != null)
                {
                    if (GetObservationAsList(observationPredicats).All(observedPredicate => observationPredicats.IsTrue(NewState.Predicates, false)))
                    {
                        if (NextBelifePatricle.ViewedStates.ContainsKey(NewState))
                        {
                            NextBelifePatricle.ViewedStates[NewState] += ProbabilityEffectRatioCount;
                        }
                        else
                        {
                            NextBelifePatricle.ViewedStates[NewState] = ProbabilityEffectRatioCount;
                        }
                    }
                }
                else
                {
                    if (NextBelifePatricle.ViewedStates.ContainsKey(NewState))
                    {
                        NextBelifePatricle.ViewedStates[NewState] += ProbabilityEffectRatioCount;
                    }
                    else
                    {
                        NextBelifePatricle.ViewedStates[NewState] = ProbabilityEffectRatioCount;
                    }
                }
            }
        }

        private List<Predicate> GetObservationAsList(Formula observationPredicates)
        {
            List<Predicate> PredicatsObservation = new List<Predicate>();
            if (observationPredicates != null)
            {
                PredicatsObservation = observationPredicates.GetAllPredicates().ToList();
            }
            return PredicatsObservation;
        }

        public bool IsApplicable(Action a)
        {
            if(Size() == 0) return false;
            if (a.Preconditions == null)
                return true;
            bool result = true;
            foreach(State s in ViewedStates.Keys)
            {
                if (a.Preconditions.IsFalse(s.Predicates, false))
                    return false;
            }
            
            return result;
        }
    }
}
