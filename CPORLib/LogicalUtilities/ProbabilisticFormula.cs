﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CPORLib.PlanningModel;
using CPORLib.Tools;

namespace CPORLib.LogicalUtilities
{
    public class ProbabilisticFormula : Formula
    {
        public List<Formula> Options { get; private set; }
        public List<double> Probabilities { get; private set; }

        public List<Predicate> ProbaEffectsAndNegations { get; set; }
        public ProbabilisticFormula()
        {
            Options = new List<Formula>();
            Probabilities = new List<double>();
            ProbaEffectsAndNegations = new List<Predicate>();
        }

        public void AddOption(Formula fOption, double dProb)
        {
            Options.Add(fOption);
            Probabilities.Add(dProb);
        }



        public override string ToString()
        {
            string s = "(probabilistic";
            for (int i = 0; i < Options.Count; i++)
            {
                s += " " + Math.Round(Probabilities[i], 3) + " " + Options[i].ToString();
            }
            s += ")";
            return s;
        }

        public override bool IsTrueDeleteRelaxation(ISet<Predicate> lKnown)
        {
            return false;
        }

        public override bool IsTrue(ISet<Predicate> lKnown, bool bContainsNegations)
        {
            return false;
        }

        public override bool IsFalse(ISet<Predicate> lKnown, bool bContainsNegations)
        {
            return false;
        }

        public override Formula Ground(Dictionary<Parameter, Constant> dBindings)
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].Ground(dBindings), Probabilities[i]);
            return pf;
        }

        public override Formula PartiallyGround(Dictionary<Parameter, Constant> dBindings)
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].PartiallyGround(dBindings), Probabilities[i]);
            return pf;
        }

        public override Formula Negate()
        {
            throw new NotImplementedException();
        }

        public override void GetAllPredicates(ISet<Predicate> lPredicates)
        {
            foreach (Formula f in Options)
                f.GetAllPredicates(lPredicates);
        }

        public override void GetAllEffectPredicates(ISet<Predicate> lConditionalPredicates, ISet<Predicate> lNonConditionalPredicates)
        {
            foreach (Formula f in Options)
                f.GetAllEffectPredicates(lConditionalPredicates, lNonConditionalPredicates);

        }

        public override bool ContainsCondition()
        {
            foreach (Formula f in Options)
                if (f.ContainsCondition())
                    return true;
            return false;
        }

        public override Formula Clone()
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].Clone(), Probabilities[i]);
            pf.ProbaEffectsAndNegations = ProbaEffectsAndNegations;
            return pf;
        }

        public override bool ContainedIn(ISet<Predicate> lPredicates, bool bContainsNegations)
        {
            throw new NotImplementedException();
        }

        public override Formula Replace(Formula fOrg, Formula fNew)
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].Replace(fOrg, fNew), Probabilities[i]);
            return pf;
        }

        public override Formula Replace(Dictionary<Formula, Formula> dTranslations)
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].Replace(dTranslations), Probabilities[i]);
            return pf;
        }

        public override Formula Simplify()
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].Simplify(), Probabilities[i]);
            return pf;
        }

        public override Formula Regress(PlanningAction a, ISet<Predicate> lObserved)
        {
            throw new NotImplementedException();
        }

        public override Formula Regress(PlanningAction a)
        {
            throw new NotImplementedException();
        }

        public override Formula Reduce(ISet<Predicate> lKnown, bool bContainsNegations)
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            bool bAllNull = true;
            for (int i = 0; i < Options.Count; i++)
            {
                Formula fReduced = Options[i].Reduce(lKnown, bContainsNegations);
                if (fReduced != null && !fReduced.IsFalse(null) && !fReduced.IsTrue(null)) //we need to modify the probabilties accordingly - not doing this for now
                {
                    pf.AddOption(fReduced, Probabilities[i]);
                    bAllNull = false;
                }
            }
            if (bAllNull)
                return null;
            return pf;
        }

        public override bool ContainsNonDeterministicEffect()
        {
            return true;
        }

        public override int GetMaxNonDeterministicOptions()
        {
            throw new NotImplementedException();
        }

        public override void GetAllOptionalPredicates(HashSet<Predicate> lPredicates)
        {
            foreach (Formula f in Options)
                f.GetAllOptionalPredicates(lPredicates);
        }

        public override Formula CreateRegression(Predicate Predicate, int iChoice)
        {
            throw new NotImplementedException();
        }

        public override Formula GenerateGiven(string sTag, List<string> lAlwaysKnown)
        {
            throw new NotImplementedException();
        }

        public override Formula AddTime(int iTime)
        {
            throw new NotImplementedException();
        }

        public override Formula ReplaceNegativeEffectsInCondition()
        {
            throw new NotImplementedException();
        }

        public override Formula RemoveImpossibleOptions(ISet<Predicate> lObserved)
        {
            throw new NotImplementedException();
        }

        public override Formula ApplyKnown(ISet<Predicate> lKnown)
        {
            throw new NotImplementedException();
        }

        public override List<Predicate> GetNonDeterministicEffects()
        {
            return GetAllPredicates().ToList();
        }

        public override Formula RemoveUniversalQuantifiers(List<Constant> lConstants, List<Predicate> lConstantPredicates, Domain d)
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Options.Count; i++)
                pf.AddOption(Options[i].RemoveUniversalQuantifiers(lConstants, lConstantPredicates, d), Probabilities[i]);
            return pf;
        }

        public override Formula GetKnowledgeFormula(List<string> lAlwaysKnown, bool bKnowWhether)
        {
            throw new NotImplementedException();
        }

        public override Formula ReduceConditions(ISet<Predicate> lKnown, bool bContainsNegations, ISet<Predicate> lRelevantOptions)
        {
            ProbabilisticFormula fNew = new ProbabilisticFormula();
            for(int i = 0; i < Options.Count;i++)
            {
                Formula fOption = Options[i].ReduceConditions(lKnown, bContainsNegations, lRelevantOptions);
                fNew.AddOption(fOption, Probabilities[i]);
            }
            return fNew;
        }

        public override Formula RemoveNegations()
        {
            ProbabilisticFormula pf = new ProbabilisticFormula();
            for (int i = 0; i < Probabilities.Count; i++)
            {
                Formula fRemoved = Options[i].RemoveNegations();
                if (fRemoved != null)
                {
                    pf.AddOption(fRemoved, Probabilities[i]);
                }
            }
            return pf;
        }

        public override Formula ToCNF()
        {
            throw new NotImplementedException();
        }

        public override void GetNonDeterministicOptions(List<CompoundFormula> lOptions)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsProbabilisticEffects()
        {
            return true;
        }

        public override void GetProbabilisticOptions(List<Formula> lOptions)
        {
            lOptions.Add(this);
        }

        public int Choose(ISet<Predicate> lAssignment)
        {
            double dRand = RandomGenerator.NextDouble();
            double dInitialRand = dRand;
            int iOption = 0, iChosenOption = -1;
            ISet<Predicate> choosenPredicates = null;
            for( iOption = 0; iOption < Options.Count;iOption++)
            {
                dRand -= Probabilities[iOption];

                ISet<Predicate> lPredicates = Options[iOption].GetAllPredicates();//if the internal is a conditional effect then we need something more complicate

                if (dRand < 0.01)
                {
                    iChosenOption = iOption;
                    break;
                }

               
                
            }
            for(iOption = 0; iOption < Options.Count; iOption++)
            {
                ISet<Predicate> lPredicates = Options[iOption].GetAllPredicates();
                if (iOption == iChosenOption)
                {
                    foreach(Predicate pred in lPredicates) lAssignment.Add(pred);
                }

                else
                {
                    foreach (Predicate pred in lPredicates) lAssignment.Add(pred.Negate());
                }
            }
            return iChosenOption;
        }
    }
}
