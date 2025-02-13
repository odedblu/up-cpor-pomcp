﻿using System;
using System.Collections.Generic;
using System.Linq;
using CPORLib.PlanningModel;
using CPORLib.Tools;



namespace CPORLib.LogicalUtilities
{
    public class PredicateFormula : Formula
    {
        public Predicate Predicate { get; private set; }
        public PredicateFormula(Predicate p)
        {
            Predicate = p;
            Size = 1;
        }
        public override bool IsTrueDeleteRelaxation(ISet<Predicate> lKnown)
        {
            if (Predicate == Utilities.TRUE_PREDICATE)
                return true;
            if (Predicate == Utilities.FALSE_PREDICATE)
                return false;
            if (Predicate.Negation)
                return true;

            if (lKnown != null)
            {
                return lKnown.Contains(Predicate);
                
            }
            return false;
        }

        public override bool IsTrue(ISet<Predicate> lKnown, bool bContainsNegations)
        {
            if (Predicate == Utilities.TRUE_PREDICATE)
                return true;
            if (Predicate == Utilities.FALSE_PREDICATE)
                return false;
            if (Predicate.Name == "=" && Predicate is GroundedPredicate)
            {
                GroundedPredicate gp = (GroundedPredicate)Predicate;
                bool bIsSame = gp.Constants[0].Equals(gp.Constants[1]);
                if (gp.Negation)
                    return !bIsSame;
                return bIsSame;
            }
            if (lKnown != null)
            {
                if (bContainsNegations)
                    return lKnown.Contains(Predicate);
                else
                {
                    Predicate pCheck = Predicate;
                    if (Predicate.Negation)
                        pCheck = Predicate.Negate();

                    bool bContained = lKnown.Contains(pCheck);
                    if (!bContained && Predicate.Negation)
                        return true;

                    if (bContained && !Predicate.Negation)
                        return true;

                    return false;
                }



            }
            return false;
        }
        public override bool IsFalse(ISet<Predicate> lKnown, bool bContainsNegations)
        {
            if (Predicate == Utilities.FALSE_PREDICATE)
                return true;
            if (Predicate == Utilities.TRUE_PREDICATE)
                return false;
            if (Predicate.Name == "=" && Predicate is GroundedPredicate)
            {
                GroundedPredicate gp = (GroundedPredicate)Predicate;
                bool bIsSame = gp.Constants[0].Equals(gp.Constants[1]);
                if (gp.Negation)
                    return bIsSame;
                return !bIsSame;
            }
            if (lKnown == null)
                return false;
            if (lKnown.Contains(Predicate))
                return false;
            Predicate pNegate = Predicate.Negate();

            if (lKnown != null)
            {
                bool bContained = lKnown.Contains(pNegate);
                if (bContained)
                    return true;
                if (pNegate.Negation && !bContainsNegations)
                    return true;
                return false;
            }
            return false;
        }
        public override string ToString()
        {
            return Predicate.ToString();
        }

        public override Formula Negate()
        {
            return new PredicateFormula(Predicate.Negate());
        }

        public override Formula Ground(Dictionary<Parameter, Constant> dBindings)
        {
            if (Predicate is ParametrizedPredicate)
            {
                ParametrizedPredicate ppred = (ParametrizedPredicate)Predicate;
                GroundedPredicate gpred = ppred.Ground(dBindings);
                return new PredicateFormula(gpred);
            }
            /*
            if (Predicate is KnowPredicate)
            {
                KnowPredicate kp = (KnowPredicate)Predicate;
                GroundedPredicate gpred = kp.Ground(dBindings);
                return new PredicateFormula(gpred);
            }
            if (Predicate is KnowGivenPredicate)
            {
                throw new NotImplementedException();
            }
            */
            return this;
        }
        public override Formula PartiallyGround(Dictionary<Parameter, Constant> dBindings)
        {
            if (Predicate is ParametrizedPredicate)
            {
                ParametrizedPredicate ppred = (ParametrizedPredicate)Predicate;
                Predicate pGrounded = ppred.PartiallyGround(dBindings);
                return new PredicateFormula(pGrounded);
            }
            /*
            if (Predicate is KnowPredicate)
            {
                throw new NotImplementedException();
            }
            if (Predicate is KnowGivenPredicate)
            {
                throw new NotImplementedException();
            }
            */
            return this;
        }

        public override void GetAllPredicates(ISet<Predicate> lPredicates)
        {
            if (!lPredicates.Contains(Predicate))
                lPredicates.Add(Predicate);
        }

        public override void GetAllEffectPredicates(ISet<Predicate> lConditionalPredicates, ISet<Predicate> lNonConditionalPredicates)
        {
            GetAllPredicates(lNonConditionalPredicates);
        }


        public override bool ContainsCondition()
        {
            return false;
        }

        public override Formula Clone()
        {
            PredicateFormula f = new PredicateFormula(Predicate);
            return f;
        }

        public override bool ContainedIn(ISet<Predicate> lPredicates, bool bContainsNegations)
        {
            if(!bContainsNegations)
            {
                if (Predicate.Negation)
                    return true;
                else
                {
                    return lPredicates.Contains(Predicate);
                }
            }


            Predicate pNegate = Predicate.Negate();
            foreach (Predicate pOther in lPredicates)
            {
                if (pOther.Equals(Predicate))
                    return true;
            }
            foreach (Predicate pOther in lPredicates)
            {
                if (pOther.Equals(pNegate))
                    return false;
            }
            if (!bContainsNegations)
                return Predicate.Negation;//assumes that predicate list contains only positives - not sure where this applies
            return false;
        }

        public override Formula Replace(Formula fOrg, Formula fNew)
        {
            if (Equals(fOrg))
                return fNew;
            return this;
        }
        public override Formula Replace(Dictionary<Formula, Formula> dTranslations)
        {
            if (dTranslations.ContainsKey(this))
                return dTranslations[this];
            return this;
        }

        public override Formula Simplify()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            PredicateFormula fOther = null;
            if (obj is CompoundFormula)
            {
                Formula fSimplify = ((CompoundFormula)obj).Simplify();
                if (fSimplify is PredicateFormula)
                    fOther = (PredicateFormula)fSimplify;
                else
                    return false;//might not be accurate - could be not
            }
            else if (obj is PredicateFormula)
            {
                fOther = (PredicateFormula)obj;
            }
            else
                return false;
            return Predicate.Equals(fOther.Predicate);
        }

        public override Formula Regress(PlanningAction a, ISet<Predicate> lObserved)
        {
            if (lObserved.Contains(Predicate))
                return new PredicateFormula(Utilities.TRUE_PREDICATE);
            if (lObserved.Contains(Predicate.Negate()))
                return new PredicateFormula(Utilities.FALSE_PREDICATE);
            return Regress(a);
        }

        public override Formula Regress(PlanningAction a)
        {
            if (a.Effects != null && a.Effects.ContainsProbabilisticEffects())
                return RegressProb(a);
            if (a.ContainsNonDeterministicEffect)
                return RegressNonDet(a);
            else
                return RegressDet(a);
        }



        public Formula RegressNonDet(PlanningAction a)
        {
            CompoundFormula cfAndNot = new CompoundFormula("and");
            CompoundFormula cfOr = new CompoundFormula("or");
            int iCondition = 0;
            Predicate pNegate = Predicate.Negate();
            foreach (CompoundFormula cfCondition in a.GetConditions())
            {
                ISet<Predicate> lEffects = cfCondition.Operands[1].GetAllPredicates();
                ISet<Predicate> lOptionalEffects = cfCondition.Operands[1].GetAllOptionalPredicates();
                if (lEffects.Contains(Predicate))
                {
                    int iChoice = cfCondition.GetChoiceIndex(Predicate);
                    cfOr.AddOperand(cfCondition.Operands[0].CreateRegression(Predicate, iChoice));
                    a.SetChoice(iCondition, iChoice);
                }
                else if (lEffects.Contains(pNegate))
                {

                    if (!lOptionalEffects.Contains(pNegate))
                        cfAndNot.AddOperand(cfCondition.Operands[0].Negate());
                    else
                    {
                        int iChoice = cfCondition.GetChoiceIndex(pNegate);
                        int iOtherChoice = cfCondition.GetOtherChoiceIndex(pNegate);
                        cfAndNot.AddOperand(cfCondition.Operands[0].CreateRegression(pNegate, iChoice).Negate());
                        a.SetChoice(iCondition, iOtherChoice);
                    }
                }
                iCondition++;
            }
            cfOr.AddOperand(this);
            cfAndNot.AddOperand(cfOr);
            return cfAndNot.Simplify();
        }


        public Formula RegressProb(PlanningAction a)
        {
            Predicate pNegate = Predicate.Negate();
            ProbabilisticFormula pfEffects = a.Effects as ProbabilisticFormula;
            foreach (Formula optionalEffect in pfEffects.Options)
            {
                ISet<Predicate> lOptionalEffects = optionalEffect.GetAllPredicates();
                if (lOptionalEffects.Contains(Predicate) || lOptionalEffects.Contains(pNegate))
                {
                    return new PredicateFormula(Utilities.TRUE_PREDICATE);
                }
            }
            return new PredicateFormula(Utilities.FALSE_PREDICATE);
        }

        public Formula RegressDet(PlanningAction a)
        {
            Formula f = a.RegressDet(Predicate);
            if (f != null)
                return f;

            CompoundFormula cfAndNot = new CompoundFormula("and");
            CompoundFormula cfOr = new CompoundFormula("or");
            int iCondition = 0;
            Predicate pNegate = Predicate.Negate();
            foreach (CompoundFormula cfCondition in a.GetConditions())
            {
                ISet<Predicate> lEffects = cfCondition.Operands[1].GetAllPredicates();
                if (lEffects.Contains(Predicate))
                {
                    Formula fReg = cfCondition.Operands[0].CreateRegression(Predicate, -1);
                    cfOr.AddOperand(fReg);
                    cfOr.AddOperand(cfCondition.Operands[0]);
                }
                else if (lEffects.Contains(pNegate))
                {
                    Formula fReg = cfCondition.Operands[0].CreateRegression(pNegate, -1).Negate();
                    cfAndNot.AddOperand(fReg);
                    cfOr.AddOperand(cfCondition.Operands[0].Negate());
                }
                iCondition++;
            }
            cfOr.AddOperand(this);
            cfAndNot.AddOperand(cfOr);
            return cfAndNot.Simplify();
        }

        public Formula RegressII(PlanningAction a)
        {
            CompoundFormula cfAndNot = new CompoundFormula("and");
            CompoundFormula cfOr = new CompoundFormula("or");
            /*
            if (a.Effects is PredicateFormula)
            {
                if (a.Effects.Equals(this))
                    return AddPreconditions(a);//assuming that an effect can't be both deterministic and conditional
            }
            else
            {
                CompoundFormula cfEffects = (CompoundFormula)a.Effects;
                if (cfEffects.Operator != "and")
                    throw new NotImplementedException();
                foreach (Formula f in cfEffects.Operands)
                    if (f.Equals(this))
                        return AddPreconditions(a);//assuming that an effect can't be both deterministic and conditional
            }
             * */
            foreach (CompoundFormula cfCondition in a.GetConditions())
            {
                HashSet<Predicate> lEffects = new HashSet<Predicate>();
                cfCondition.Operands[1].GetAllPredicates(lEffects);
                if (lEffects.Contains(Predicate))
                {
                    cfOr.AddOperand(cfCondition.Operands[0]);
                }
                if (lEffects.Contains(Predicate.Negate()))
                    cfAndNot.AddOperand(cfCondition.Operands[0].Negate());
            }
            cfAndNot.AddOperand(this);
            cfOr.AddOperand(cfAndNot);
            return cfOr.Simplify();
        }

        private Formula AddPreconditions(PlanningAction a)
        {
            CompoundFormula cfOr = new CompoundFormula("or");
            CompoundFormula cfAnd = new CompoundFormula("and");
            cfAnd.AddOperand(a.Preconditions);
            cfAnd.AddOperand(Negate());
            cfOr.AddOperand(cfAnd);
            cfOr.AddOperand(this);
            return cfOr.Simplify();
        }

        public override Formula Reduce(ISet<Predicate> lKnown, bool bContainsNegations)
        {
            Predicate pReduced = Predicate;
            if (bContainsNegations)
            {
                if (lKnown.Contains(Predicate))
                    pReduced = Utilities.TRUE_PREDICATE;
                if (lKnown.Contains(Predicate.Negate()))
                    pReduced = Utilities.FALSE_PREDICATE;
            }
            else
            {
                if(Predicate.Negation)
                {
                    if(lKnown.Contains(Predicate.Negate()))
                        pReduced = Utilities.FALSE_PREDICATE;
                    else
                        pReduced = Utilities.TRUE_PREDICATE;
                }
                else
                {
                    if (lKnown.Contains(Predicate))
                        pReduced = Utilities.TRUE_PREDICATE;
                    else
                        pReduced = Utilities.FALSE_PREDICATE;
                }
            }
            return new PredicateFormula(pReduced);
        }

        public override bool ContainsNonDeterministicEffect()
        {
            return false;
        }
        public override int GetMaxNonDeterministicOptions()
        {
            return 0;
        }

        public override void GetAllOptionalPredicates(HashSet<Predicate> lPredicates)
        {
            //predicate is not optional
        }

        public override Formula CreateRegression(Predicate p, int iChoice)
        {
            RegressedPredicate rpNew = new RegressedPredicate((GroundedPredicate)Predicate, p, iChoice);
            return new PredicateFormula(rpNew);
        }

        public override Formula GenerateGiven(string sTag, List<string> lAlwaysKnown)
        {
            if (lAlwaysKnown.Contains(Predicate.Name))
                return this;
            PredicateFormula pfGiven = new PredicateFormula(Predicate.GenerateKnowGiven(sTag));
            return pfGiven;
        }

        public override Formula AddTime(int iTime)
        {
            throw new NotImplementedException();

        }

        public override Formula ReplaceNegativeEffectsInCondition()
        {
            if (Predicate.Negation)
            {
                Predicate p = Predicate.Negate();
                p.Name = "Not-" + p.Name;
                return new PredicateFormula(p);
            }
            return this;
        }
        public override Formula RemoveImpossibleOptions(ISet<Predicate> lObserved)
        {
            if (lObserved.Contains(Predicate.Negate()))
                return null;
            return this;
        }

        public override Formula ApplyKnown(ISet<Predicate> lKnown)
        {
            return this;
            /* Seems like this is what we want, but perhaps not here
            if (lKnown.Contains(Predicate))
                return new PredicateFormula(Utilities.TRUE_PREDICATE);
            else if(lKnown.Contains(Predicate.Negate()))
                return new PredicateFormula(Utilities.FALSE_PREDICATE);
            return this;
             * */
        }

        public override List<Predicate> GetNonDeterministicEffects()
        {
            return new List<Predicate>();
        }

        public override Formula RemoveUniversalQuantifiers(List<Constant> lConstants, List<Predicate> lConstantPredicates, Domain d)
        {
            if (d != null && lConstantPredicates != null && d.AlwaysConstant(Predicate) && d.AlwaysKnown(Predicate) && !(Predicate is ParametrizedPredicate))
            {
                Predicate p = Predicate;
                if (p.Negation)
                    p = p.Negate();
                bool bContains = lConstantPredicates.Contains(p);
                //assuming that list does not contain negations
                if (bContains && !Predicate.Negation || !bContains && Predicate.Negation)
                    return new PredicateFormula(Utilities.TRUE_PREDICATE);
                else
                    return new PredicateFormula(Utilities.FALSE_PREDICATE);
            }
            return this;
        }

        public override Formula GetKnowledgeFormula(List<string> lAlwaysKnown, bool bKnowWhether)
        {
            if (Predicate.Name == Utilities.OPTION_PREDICATE)
                return null;//we never know an option value
            if (lAlwaysKnown.Contains(Predicate.Name))
                return this;
            if (bKnowWhether)
                return new PredicateFormula(Predicate.GenerateKnowWhetherPredicate(Predicate));
            else
                return new PredicateFormula(Predicate.GenerateKnowPredicate(Predicate));
        }

        public override Formula ReduceConditions(ISet<Predicate> lKnown, bool bContainsNegations, ISet<Predicate> lRelevantOptions)
        {
            return new PredicateFormula(Predicate);
        }

        public override Formula RemoveNegations()
        {
            if (Predicate.Negation)
                return null;
            return this;
        }

        public override Formula ToCNF()
        {
            return this;
        }

        public override void GetNonDeterministicOptions(List<CompoundFormula> lOptions)
        {

        }

        public override bool ContainsProbabilisticEffects()
        {
            return false;
        }

        public override void GetProbabilisticOptions(List<Formula> lOptions)
        {
            
        }
    }
}
