﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPORLib.PlanningModel;
using CPORLib.LogicalUtilities;

namespace CPORLib.Algorithms
{
    internal class ObservationPomcpNode : PomcpNode
    {

        public BelifeParticles ParticleFilter;
        public List<Predicate> ObservedPredicates;
        public PartiallySpecifiedState PartiallySpecifiedState;
        public Formula Observation { get; set; }

        public double RolloutSum { get; internal set; }

        public ObservationPomcpNode(PartiallySpecifiedState partiallySpecifiedState, Formula observation)
        {
            Parent = null;
            Children = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            ParticleFilter = new BelifeParticles();
            ObservedPredicates = null;
            PartiallySpecifiedState = partiallySpecifiedState;
            Observation = observation;
        }

        public ObservationPomcpNode(ActionPomcpNode ActionParentNode, List<Predicate> Observed, 
            PartiallySpecifiedState partiallySpecifiedState, BelifeParticles particleFilter, Formula observation)
        {
            Parent = ActionParentNode;
            Children = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            ParticleFilter = particleFilter;
            ObservedPredicates = new List<Predicate>(); 
            foreach (Predicate Predicate in Observed) 
                ObservedPredicates.Add(Predicate);
            PartiallySpecifiedState = partiallySpecifiedState;

        }

        public ObservationPomcpNode(ActionPomcpNode ActionParentNode, PartiallySpecifiedState partiallySpecifiedState, BelifeParticles particleFilter, Formula observation)
        {
            Parent = ActionParentNode;
            Children = new Dictionary<int, PomcpNode>();
            VisitedCount = 0;
            Value = 0;
            ParticleFilter = particleFilter;
            PartiallySpecifiedState = partiallySpecifiedState;
            Observation = observation;

            //if (ID == 3256)
            //    Console.Write("*");

        }

        public void AddActionPomcpNode(ActionPomcpNode actionPomcpNode)
        {
            int actionHash = actionPomcpNode.Action.GetHashCode();
            if (!Children.ContainsKey(actionHash))
            {
                actionPomcpNode.Parent = this;
                Children.Add(actionHash, actionPomcpNode);
            }
        }

        public override string ToString()
        {
            return string.Join(", ", ObservedPredicates);
        }

        internal void RemoveChild(ActionPomcpNode acn)
        {
            int actionHash = acn.Action.GetHashCode();
            if (Children.ContainsKey(actionHash))
            {
                Children.Remove(actionHash);
            }
        }
    }
}
