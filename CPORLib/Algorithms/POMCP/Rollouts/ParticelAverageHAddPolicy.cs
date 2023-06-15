using System;
using System.Collections.Generic;
using System.Text;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;
using Action = CPORLib.PlanningModel.PlanningAction;
using CPORLib.Tools;
using static CPORLib.Tools.Options;

namespace CPORLib.Algorithms
{
    internal class ParticelAverageHAddPolicy : IRolloutPolicy
    {
        public GuyHaddHeuristuc rolloutPolicy { get; set; }
        public BeliefParticles currentParticle { get; set; }





        public ParticelAverageHAddPolicy(Domain d, Problem p)
        {
            rolloutPolicy = new GuyHaddHeuristuc(d, p);
            rolloutPolicy.Init();
        }

        public void UpdateParticle(BeliefParticles bf)
        {
            currentParticle = bf;
        }


        public double GetParticleAvarageHaddValue(BeliefParticles bf)
        {
            int total_states_count = bf.Size();
            double score_sum = 0;
            foreach (KeyValuePair<State,int> particle in bf.ViewedStates)
            {
                double particle_rollout_value = rolloutPolicy.ComputeHAdd(particle.Key);
                score_sum += particle_rollout_value * particle.Value;
            }
            return score_sum / (double)total_states_count;
        }

        public (PlanningAction, State) ChooseAction(State s)
        {
            Action BestAction = null;
            double BestActionScore = Double.MaxValue;

            foreach(Action a in rolloutPolicy.AllGroundedActions)
            {
                if (currentParticle.IsApplicable(a))
                {
                    BeliefParticles actionBelifeParticle = currentParticle.Apply(a, a.Observe);
                    double postActionParticleAvarageHaddValue = GetParticleAvarageHaddValue(actionBelifeParticle);
                    if(postActionParticleAvarageHaddValue < BestActionScore)
                    {
                        BestAction = a;
                        BestActionScore = postActionParticleAvarageHaddValue;
                    }
                }

            }
            if (BestAction != null)
            {
                currentParticle = currentParticle.Apply(BestAction, BestAction.Observe);
            }
            return (BestAction,null);
        }

        public (PlanningAction, State, ISet<State>) ChooseAction(State s, ISet<State> l, bool bPreferRefutation)
        {
            throw new NotImplementedException();
        }
    }
}

