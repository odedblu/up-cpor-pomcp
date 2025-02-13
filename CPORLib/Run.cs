﻿using CPORLib.Algorithms;
using CPORLib.LogicalUtilities;
using CPORLib.Parsing;
using CPORLib.PlanningModel;
using CPORLib.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace CPORLib
{
    public class Run
    {

        public static void Main(string[] args)
        {
            //TestAll();
            //return;


            if (args.Length < 3)
            {
                Console.WriteLine("Usage: RunPlanner domain_file problem_file output_file [online/offline]");
            }
            else
            {
                string sDomainFile = args[0];
                string sProblemFile = args[1];
                string sOutputFile = args[2];
                bool bOnline = false;
                if (args.Length > 3)
                    bOnline = args[2] == "online";
                RunPOMCPPlanner(sDomainFile
                    , sProblemFile,
                    sOutputFile,
                    bOnline);
            }
        }


        public static void TestHAdd(Domain d, Problem p)
        {
            int cExecutions = 1000;
            HAddHeuristic h = new HAddHeuristic(d, p);
            BeliefState bs = p.GetInitialBelief();
            Console.WriteLine("Testing " + p.Name);

            Console.WriteLine("Choosing states");

            List<State> states = new List<State>();
            for (int i = 0; i < cExecutions; i++)
            {
                State s = bs.ChooseState(true, true);
                states.Add(s);
                if (i % 100 == 0)
                    Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b" + i + "/" + cExecutions);

            }

            DateTime dtStart = DateTime.Now;
            Console.WriteLine("\n Computing hadd");

            double dSum = 0.0;

            for (int i = 0; i < cExecutions; i++)
            {
                State s = states[i];
                double cost = h.ComputeHAdd(s);
                dSum += cost;
                //if (i % 100 == 0)
                //  Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b" + i + "/" + cExecutions);
            }

            DateTime dtEnd = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("Run " + cExecutions + " in " + (dtEnd - dtStart).TotalMilliseconds + ", avg = " + dSum / cExecutions);

        }
        //Create run POMCP similar to RunPlanner here.
        public static void RunPOMCPPlanner(string sDomainFile, string sProblemFile, string sOutputFile, bool bOnline, bool bValidate = false, string name = "")
        {
            int NumOfRuns = 20;
            Console.WriteLine($"Running {name}:");
            Console.WriteLine("----------------------");
            double averageTimeInSeconds = 0;
            double averageStepsToGoal = 0;
            int numberOfSuccesRuns = 0;
            Parser parser = new Parser();
            string sName = "";

            double EXPLORATION_FACTOR_UCB = 150.0;
            double DISCOUNT_FACTOR = 0.95;
            double DEPTH_THRESHOLD = 0.55;
            int SIMULATIONS = 1000;
            int MaxInnerDepth = 5;
            int MaxOuterDepth = 50;

            string[] aPloicies = new string[] { "Random", "HADD", "SDR" };


            for (int iMaxDepth = 2; iMaxDepth < 5; iMaxDepth++)
            {



                foreach (string sPolicy in aPloicies)
                {

                    for (int i = 0; i < NumOfRuns; i++)
                    {
                        //Debug.WriteLine("Reading domain and problem");
                        Domain domain = parser.ParseDomain(sDomainFile);
                        Problem problem = parser.ParseProblem(sProblemFile, domain);


                        sName = problem.Name;
                        //Debug.WriteLine("Done reading domain and problem");


                        IRolloutPolicy RolloutPolicy = null;
                        if (sPolicy == "Random")
                            RolloutPolicy = new RandomRolloutPolicy();
                        if (sPolicy == "HADD")
                            RolloutPolicy = new GuyHaddHeuristuc(domain, problem);
                        if (sPolicy == "SDR")
                            RolloutPolicy = new SDRwithHAddHeuristic(domain, problem);


                        IActionSelectPolicy ActionSelectPolicy = new UCBValueActionSelectPolicy(EXPLORATION_FACTOR_UCB);
                        IActionSelectPolicy FinalActionSelectPolicy = new MaxValueActionSelectPolicy();



                        //ObservationPomcpNode root = new ObservationPomcpNode(new PartiallySpecifiedState(problem.GetInitialBelief()), null);
                        PomcpAlgorithm pomcpAlgorithm = new PomcpAlgorithm(DISCOUNT_FACTOR, DEPTH_THRESHOLD, SIMULATIONS, problem, FinalActionSelectPolicy, ActionSelectPolicy, RolloutPolicy, RewardFunctions.GeneralReward);
                        pomcpAlgorithm.MaxInnerDepth = iMaxDepth;
                        pomcpAlgorithm.MaxOuterDepth = MaxOuterDepth;


                        RandomGenerator.Init(i * 1111);

                        DateTime dtStart = DateTime.Now;
                        try
                        {
                            List<PlanningAction> plan = pomcpAlgorithm.FindPlan(true);
                            averageStepsToGoal += plan.Count;
                            if (plan.Count < 100)
                            {
                                numberOfSuccesRuns++;
                            }
                            TimeSpan tsTime = (DateTime.Now - dtStart);
                            averageTimeInSeconds += Math.Round(tsTime.TotalSeconds, 4);
                            Console.WriteLine("\n\nGoal reached. Plan:");
                            foreach (PlanningAction action in plan)
                            {
                                Console.WriteLine(action.Name);
                            }

                            using (StreamWriter sw = new StreamWriter(sOutputFile, true))
                            {
                                sw.WriteLine(sName + ", " + pomcpAlgorithm.MaxInnerDepth + "," + SIMULATIONS + "," + RolloutPolicy.Name() + "," + MaxInnerDepth + ", " + i + ", " + plan.Count + ", " + tsTime.TotalSeconds);
                                sw.Close();
                            }
                        }
                        catch
                        {

                        }




                        //averageTimeInSeconds += Math.Round(tsTime.TotalSeconds, 4);
                        /* Console.WriteLine("Time: " + Math.Round(tsTime.TotalSeconds, 4));
                         Console.WriteLine("************************************************\n\n");*/
                    }
                    Console.WriteLine($"average time = {averageTimeInSeconds / (double)numberOfSuccesRuns}.");
                    Console.WriteLine($"average steps = {averageStepsToGoal / numberOfSuccesRuns}.");
                    Console.WriteLine($"Success rate = {((double)numberOfSuccesRuns * 100) / NumOfRuns}%.");

                    using (StreamWriter sw = new StreamWriter(sOutputFile, true))
                    {
                        sw.WriteLine(sName + ", " + sPolicy + ", " + averageTimeInSeconds / (double)numberOfSuccesRuns + ", " + averageStepsToGoal / numberOfSuccesRuns + ", " + ((double)numberOfSuccesRuns * 100) / NumOfRuns);
                        sw.Close();
                    }
                }
            }
        }

        public static void RunPlanner(string sDomainFile, string sProblemFile, string sOutputFile, bool bOnline, bool bValidate = false)
        {

            Debug.WriteLine("Reading domain and problem");
            Parser parser = new Parser();
            Domain domain = parser.ParseDomain(sDomainFile);
            Problem problem = parser.ParseProblem(sProblemFile, domain);
            Debug.WriteLine("Done reading domain and problem");


            //TestHAdd(domain, problem);
            //return;

            Options.TagsCount = 2;
            //Options.SDR_OBS = true;


            if (bOnline)
            {
                //sdr.OnlineReplanning();
                int cIterations = 10, cSuccess = 0;
                int idx = 0;
                for (int i = 0; i < cIterations; i++)
                {
                    SDRPlanner sdr = new SDRPlanner(domain, problem);
                    Simulator sim = new Simulator(domain, problem);
                    Console.WriteLine("Starting " + domain.Name);
                    while (!sim.GoalReached)
                    {

                        string sAction = sdr.GetAction();
                        if (sAction == null)
                            Console.Write("*");
                        string sObservation = sim.Apply(sAction);
                        bool bResult = sdr.SetObservation(sObservation);
                        if (!bResult)
                        {
                            sObservation = "true";
                            if (RandomGenerator.NextDouble() < 0.5)
                                sObservation = "false";
                            bResult = sdr.SetObservation(sObservation);
                        }
                        Console.WriteLine(idx + ") Executed " + sAction + ", received " + sObservation);
                        idx++;

                    }
                    cSuccess++;
                }
            }
            else
            {
                //Options.SDR_OBS = true;
                CPORPlanner cpor = new CPORPlanner(domain, problem);
                cpor.InfoLevel = 1;
                ConditionalPlanTreeNode n = cpor.OfflinePlanning();
                cpor.WritePlan(sOutputFile, n);

                if (bValidate)
                    if (!cpor.ValidatePlanGraph(n))
                        Console.WriteLine("Invalid plan");
            }
        }


    }
}
