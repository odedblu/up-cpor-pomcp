﻿using CPORLib.LogicalUtilities;
using CPORLib.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Action = CPORLib.PlanningModel.PlanningAction;

namespace CPORLib.PlanningModel
{
    public class State
    {
        public ISet<Predicate> Predicates { get { return new UnifiedSet<Predicate>(m_lFixedAndKnown, m_lOptionPredicates, m_lFixedAndHidden, m_lChangingPredicates); } }

        //protected HashSet<Predicate> m_lPredicates;

        protected ISet<Predicate> m_lFixedAndKnown;
        protected ISet<Predicate> m_lFixedAndHidden;
        protected ISet<Predicate> m_lOptionPredicates;
        protected ISet<Predicate> m_lChangingPredicates;

        public ISet<Predicate> ChangingPredicates { get { return m_lChangingPredicates; } }
        public ISet<Predicate> FixedHiddenPredicates { get { return m_lFixedAndHidden; } }
        public ISet<Predicate> OptionPredicates { get { return m_lOptionPredicates; } }

        public List<Action> AvailableActions { get; set; }
        public State Predecessor { set; get; }
        public PlanningAction GeneratingAction { private set; get; }
        public PlanningAction OriginalGeneratingAction { private set; get; }
        public ISet<Predicate> RelevantOptions { private set; get; }
        public List<string> History { private set; get; }
        public bool MaintainNegations { get; private set; }
        public bool MaintainProbabilisticChoices { get; private set; }
        public Problem Problem { get; private set; }
        public int ID { get; private set; }
        public Dictionary<string, double> FunctionValues { get; private set; }
        public int Time { get; private set; }
        public int ChoiceCount { get; private set; }

        private Dictionary<string, State> m_dSuccssessors;

        public static int STATE_COUNT = 0;

        public State(Problem p)
        {
            Problem = p;
            Predecessor = null;
            //m_lPredicates = new HashSet<Predicate>();
            m_lFixedAndKnown = new GenericArraySet<Predicate>();
            m_lFixedAndHidden = new GenericArraySet<Predicate>();
            m_lChangingPredicates = new GenericArraySet<Predicate>();
            m_lOptionPredicates = new GenericArraySet<Predicate>();
            AvailableActions = new List<Action>();
            MaintainNegations = true;
            MaintainProbabilisticChoices = false;
            ID = STATE_COUNT++;
            FunctionValues = new Dictionary<string, double>();
            Time = 0;
            ChoiceCount = 0;
            foreach (string sFunction in Problem.Domain.Functions)
            {
                FunctionValues[sFunction] = 0.0;
            }
            History = new List<string>();

            m_dSuccssessors = new Dictionary<string, State>();
            //History.Add(ToString());
        }
        public State(State sPredecessor)
            : this(sPredecessor.Problem)
        {
            Predecessor = sPredecessor;
            //m_lPredicates = new HashSet<Predicate>(sPredecessor.m_lPredicates);
            m_lChangingPredicates = new GenericArraySet<Predicate>(Predecessor.m_lChangingPredicates);


            m_lOptionPredicates = sPredecessor.m_lOptionPredicates;
            m_lFixedAndKnown = Predecessor.m_lFixedAndKnown;
            m_lFixedAndHidden = Predecessor.m_lFixedAndHidden;

            

            FunctionValues = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> p in sPredecessor.FunctionValues)
                FunctionValues[p.Key] = p.Value;
            Time = sPredecessor.Time + 1;
            MaintainNegations = sPredecessor.MaintainNegations;
            MaintainProbabilisticChoices = sPredecessor.MaintainProbabilisticChoices;
            m_dSuccssessors = new Dictionary<string, State>();
        }

        public bool ConsistentWith(Predicate p)
        {
            foreach (Predicate pState in Predicates)
            {
                if (!p.ConsistentWith(pState))
                    return false;
            }
            return true;
        }

        public bool ConsistentWith(Formula f)
        {
            if (f is CompoundFormula)
            {
                CompoundFormula cf = (CompoundFormula)f;
                bool bConsistent = false;
                foreach (Formula fOperand in cf.Operands)
                {
                    bConsistent = ConsistentWith(fOperand);
                    if (cf.Operator == "and" && !bConsistent)
                        return false;
                    if (cf.Operator == "or" && bConsistent)
                        return true;
                    if (cf.Operator == "not")
                        return !bConsistent;
                }
                if (cf.Operator == "and")
                    return true;
                if (cf.Operator == "or")
                    return false;
            }
            else
            {
                PredicateFormula vf = (PredicateFormula)f;
                return ConsistentWith(vf.Predicate);
            }
            return false;
        }


        public void RemovePredicate(Predicate p)
        {
            //m_lPredicates.Remove(p);
            m_lChangingPredicates.Remove(p);
        }

        public void AddPredicate(Predicate p)
        {
            if (!MaintainNegations && p.Negation)
                return;
            //m_lPredicates.Add(p);

            if (p.Name.Contains(Utilities.OPTION_PREDICATE))
                m_lOptionPredicates.Add(p);
            else
            {
                if (Problem.Domain.AlwaysConstant(p))
                {
                    if (Problem.InitiallyUnknown(p))
                    {
                        m_lFixedAndHidden.Add(p);
                    }
                    else
                    {
                        m_lFixedAndKnown.Add(p);
                    }

                }
                else
                    m_lChangingPredicates.Add(p);
            }
            
        }


        public override bool Equals(object obj)
        {
            if (obj is State)
            {
                State s = (State)obj;
                if (s.m_lChangingPredicates.Count != m_lChangingPredicates.Count)
                    return false;

                if (s.GetHashCode() != GetHashCode())
                    return false;

                foreach (Predicate p in s.m_lChangingPredicates)
                    if (!m_lChangingPredicates.Contains(p))
                        return false;

                foreach (Predicate p in s.m_lFixedAndHidden)
                    if (!m_lFixedAndHidden.Contains(p))
                        return false;

                foreach (Predicate p in s.m_lOptionPredicates)
                    if (!m_lOptionPredicates.Contains(p))
                        return false;

                return true;

                //return m_lPredicates.Equals(s.m_lPredicates);
            }
            return false;
        }
        public virtual void GroundAllActions()
        {
            AvailableActions = Problem.Domain.GroundAllActions(Predicates, MaintainNegations);
        }
        public bool Contains(Formula f)
        {
            return f.ContainedIn(Predicates, false);
        }
        public virtual State Clone()
        {
            //BUGBUG; //very slow? remove negations?
            return new State(this);
        }
        /*
        public State Apply(string sActionName)
        {
            sActionName = sActionName.Replace(' ', '_');//moving from ff format to local format
            if (AvailableActions.Count == 0)
                GroundAllActions(Problem.Domain.Actions);
            foreach (Action a in AvailableActions)
                if (a.Name == sActionName)
                    return Apply(a);
            return null;
        }
         * */
        public State Apply(string sActionName)
        {
            string sRevisedActionName = sActionName.Replace(Utilities.DELIMITER_CHAR, " ");
            string[] aName = Utilities.SplitString(sRevisedActionName, ' ');
            Action a = Problem.Domain.GroundActionByName(aName);
            if (a == null)
                return null;
            return Apply(a);
        }



        public State Apply(Action a)
        {
            /*
            if(a.Original != null)
            {
                a = a.Original;
            }
            */
            //Debug.WriteLine("Executing " + a.Name);
            if (a is ParametrizedAction)
                return null;

            if (m_dSuccssessors.TryGetValue(a.ToString(), out State s)) //need something smarter for probabilistic effects
                return s;


            //if (m_lPredicates.Count != all.Count)
            //    Console.Write("*");

            if (a.Preconditions != null && !a.Preconditions.IsTrue(Predicates, MaintainNegations))
                return null;

            State sNew = Clone();

            
            sNew.OriginalGeneratingAction = a;
            RelevantOptions = new HashSet<Predicate>();
            Action aTag = a.ApplyObserved(Predicates, false, RelevantOptions);
            sNew.GeneratingAction = aTag;
            //a = aTag;

            sNew.History = new List<string>(History);
            sNew.History.Add(ToString());
            sNew.History.Add(a.Name);

            sNew.Time = Time + 1;

            if (a.Effects == null)
                return sNew;

            if (a.Effects != null)
            {
                /*
                if (a.HasConditionalEffects)
                {
                    sNew.AddEffects(a.GetApplicableEffects(m_lPredicates, MaintainNegations));
                }
                else
                {
                    sNew.AddEffects(a.Effects);
                }
                 * */
                HashSet<Predicate> lDeleteList = new HashSet<Predicate>(), lAddList = new HashSet<Predicate>();
                GetApplicableEffects(a.Effects, lAddList, lDeleteList);
                foreach (Predicate p in lDeleteList)
                    sNew.AddEffect(p);
                foreach (Predicate p in lAddList)
                    sNew.AddEffect(p);
                //sNew.AddEffects(a.Effects);
            }
            /*
            if (!MaintainNegations)
            {
                bool b = sNew.RemoveNegativePredicates();
                
            }
            */
            if (sNew.Predicates.Contains(Utilities.FALSE_PREDICATE)) 
                Debug.WriteLine("BUGBUG");

            if(!a.HasConditionalEffects && !a.HasProbabilisticEffects)
            {
                m_dSuccssessors[a.ToString()] = sNew;
            }

            return sNew;
        }
        private void AddEffect(Predicate pEffect)
        {
            if (pEffect == Utilities.FALSE_PREDICATE)
                Debug.WriteLine("BUGBUG");
            if (Problem.Domain.IsFunctionExpression(pEffect.Name))
            {
                GroundedPredicate gpIncreaseDecrease = (GroundedPredicate)pEffect;
                double dPreviousValue = Predecessor.FunctionValues[gpIncreaseDecrease.Constants[0].Name];
                double dDiff = double.Parse(gpIncreaseDecrease.Constants[1].Name);
                double dNewValue = double.NaN;
                if (gpIncreaseDecrease.Name.ToLower() == "increase")
                    dNewValue = dPreviousValue + dDiff;
                else if (gpIncreaseDecrease.Name.ToLower() == "decrease")
                    dNewValue = dPreviousValue + dDiff;
                else
                    throw new NotImplementedException();
                FunctionValues[gpIncreaseDecrease.Constants[0].Name] = dNewValue;
            }
            else //if (!m_lPredicates.Contains(pEffect))
            {
                Predicate pNegateEffect = pEffect.Negate();

                RemovePredicate(pNegateEffect);

                    AddPredicate(pEffect);
            }
        }
        private void AddEffects(Formula fEffects)
        {
            if (fEffects is PredicateFormula)
            {
                AddEffect(((PredicateFormula)fEffects).Predicate);
            }
            else
            {
                CompoundFormula cf = (CompoundFormula)fEffects;
                if (cf.Operator == "oneof" || cf.Operator == "or")//BUGBUG - should treat or differently
                {
                    int iRandomIdx = RandomGenerator.Next(cf.Operands.Count);
                    AddEffects(cf.Operands[iRandomIdx]);
                    GroundedPredicate pChoice = new GroundedPredicate("Choice");
                    pChoice.AddConstant(new Constant("ActionIndex", "a" + (Time - 1)));//time - 1 because this is the action that generated the state, hence its index is i-1
                    pChoice.AddConstant(new Constant("ChoiceIndex", "c" + iRandomIdx));
                    State s = this;
                    while (s != null)
                    {
                        s.AddPredicate(pChoice);
                        s = s.Predecessor;
                    }
                }
                else if (cf.Operator == "and")
                {
                    foreach (Formula f in cf.Operands)
                    {
                        if (f is PredicateFormula)
                        {
                            AddEffect(((PredicateFormula)f).Predicate);
                        }
                        else
                            AddEffects(f);
                    }
                }
                else if (cf.Operator == "when")
                {
                    if (Predecessor.Contains(cf.Operands[0]))
                        AddEffects(cf.Operands[1]);
                }
                else
                    throw new NotImplementedException();
            }
        }

        private void GetApplicableEffects(Formula fEffects, HashSet<Predicate> lAdd, HashSet<Predicate> lDelete)
        {
            if (fEffects is PredicateFormula)
            {
                Predicate p = ((PredicateFormula)fEffects).Predicate;
                if (p.Negation)
                    lDelete.Add(p);
                else
                    lAdd.Add(p);
            }
            else if (fEffects is ProbabilisticFormula)
            {
                HashSet<Predicate> hsEffects = new HashSet<Predicate>();

                ProbabilisticFormula pf = (ProbabilisticFormula)fEffects;
                int iOption = pf.Choose(hsEffects);

                foreach (Predicate p in hsEffects)
                {
                    if (p.Negation)
                        lDelete.Add(p);
                    else
                        lAdd.Add(p);
                }

                if (MaintainProbabilisticChoices)
                {
                    GroundedPredicate pChoice = new GroundedPredicate("Choice");
                    pChoice.AddConstant(new Constant("ActionIndex", "a" + Time));
                    pChoice.AddConstant(new Constant("ChoiceIndex", "c" + ChoiceCount + "." + iOption));
                    ChoiceCount++;
                    State s = this;
                    while (s != null)
                    {
                        s.AddPredicate(pChoice);
                        s = s.Predecessor;
                    }
                }

            }
            else
            {
                CompoundFormula cf = (CompoundFormula)fEffects;
                if (cf.Operator == "oneof" || cf.Operator == "or")//BUGBUG - should treat or differently
                {
                    int iRandomIdx = RandomGenerator.Next(cf.Operands.Count);
                    GetApplicableEffects(cf.Operands[iRandomIdx], lAdd, lDelete);
                    GroundedPredicate pChoice = new GroundedPredicate("Choice");
                    pChoice.AddConstant(new Constant("ActionIndex", "a" + Time));
                    pChoice.AddConstant(new Constant("ChoiceIndex", "c" + ChoiceCount + "." + iRandomIdx));
                    ChoiceCount++;
                    State s = this;
                    while (s != null)
                    {
                        s.AddPredicate(pChoice);
                        s = s.Predecessor;
                    }
                }
                else if (cf.Operator == "and")
                {
                    foreach (Formula f in cf.Operands)
                    {
                        GetApplicableEffects(f, lAdd, lDelete);
                    }
                }
                else if (cf.Operator == "when")
                {
                    if (Contains(cf.Operands[0]))
                        GetApplicableEffects(cf.Operands[1], lAdd, lDelete);
                }
                else if (cf is ParametrizedFormula)
                {
                    ParametrizedFormula pf = (ParametrizedFormula)cf;
                    foreach (Formula fNew in pf.Ground(Problem.Domain.Constants))
                        GetApplicableEffects(fNew, lAdd, lDelete);
                }
                else
                    throw new NotImplementedException();
            }
        }

        public Formula Observe(Formula fObserve)
        {
            if (fObserve == null)
                return null;
            if (fObserve is PredicateFormula)
            {
                Predicate pObserve = ((PredicateFormula)fObserve).Predicate;
                foreach (Predicate pCurrent in Predicates)
                {
                    if (pObserve.Equals(pCurrent))
                    {
                        return new PredicateFormula(pCurrent);
                    }
                }
                return new PredicateFormula(pObserve.Negate());
            }
            throw new NotImplementedException("Not handling compound formulas for observations");
        }

        public bool RemoveNegativePredicates()
        {
            bool bFiltered = false;
            ISet<Predicate> lFiltered = new GenericArraySet<Predicate>();
            foreach (Predicate pObserved in m_lChangingPredicates)
            {
                if (pObserved.Negation == false)
                {
                    lFiltered.Add(pObserved);
                }
                else
                    bFiltered = true;

            }
            m_lChangingPredicates = lFiltered;

            lFiltered = new GenericArraySet<Predicate>();
            foreach (Predicate pObserved in m_lFixedAndKnown)
            {
                if (pObserved.Negation == false)
                {
                    lFiltered.Add(pObserved);
                }
                else
                    bFiltered = true;

            }
            m_lFixedAndKnown = lFiltered;

            lFiltered = new GenericArraySet<Predicate>();
            foreach (Predicate pObserved in m_lFixedAndHidden)
            {
                if (pObserved.Negation == false)
                {
                    lFiltered.Add(pObserved);
                }
                else
                    bFiltered = true;
            }
            m_lFixedAndHidden = lFiltered;

            MaintainNegations = false;
            return bFiltered;
        }

        private string m_sToString = null;
        public override string ToString()
        {
            if (m_sToString != null)
                return m_sToString;
            m_sToString = "";
            foreach (Predicate p in m_lChangingPredicates)
            {
                m_sToString += p + ",";
            }
            foreach (Predicate p in m_lFixedAndHidden)
            {
                m_sToString += p + ",";
            }
            return m_sToString;
        }

        private int m_iHashCode = 0;
        public override int GetHashCode()
        {
            if (m_iHashCode != 0)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (Predicate p in m_lFixedAndHidden)
                    {
                        hash *= p.GetHashCode();
                    }
                    foreach (Predicate p in m_lChangingPredicates)
                    {
                        hash *= p.GetHashCode();
                    }
                    m_iHashCode = hash;
                }
            }
            return m_iHashCode;
        }

        public bool Contains(Predicate p)
        {
            if (p.Negation)
                return !Predicates.Contains(p.Negate());
            return Predicates.Contains(p);
        }

        public void ClearOptionPredicates()
        {
            m_lOptionPredicates = new GenericArraySet<Predicate>();
        }

        internal void CompleteNegations()
        {
            throw new NotImplementedException();
        }
    }
}
