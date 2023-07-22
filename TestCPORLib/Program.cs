using CPORLib;
using CPORLib.FFCS;
using System;
using System.IO;

public class Program
{
    public static void RunTest(string sName, bool bOnline)
    {
        DateTime dtStart = DateTime.Now;
        string sPath = Path.Combine("..", "..", "..", "..", "Tests", sName);
        //string sPath = @"C:\Users\Guy\OneDrive - Ben Gurion University of the Negev\Research\projects\AIPlan4EU\up-cpor\Tests\" + sName;
        string sDomainFile = Path.Combine(sPath, "d.pddl");
        string sProblemFile = Path.Combine(sPath, "p.pddl");
        string sOutputFile = Path.Combine(sPath, "out.txt");
        Run.RunPOMCPPlanner(sDomainFile
            , sProblemFile,
            sOutputFile,
            bOnline, false, sName);

        DateTime dtEnd = DateTime.Now;
        Console.WriteLine("Time: " + (dtEnd - dtStart).TotalSeconds);
    }
    public static void TestAll(bool bOnline)
    {
        FFUtilities.Verbose = false;
        gcmd_line.display_info = 0;
        gcmd_line.debug = 0;




        // Regular problems solve:

        RunTest("blocks7", bOnline); //good
        //RunTest("doors5", bOnline); //good
        //RunTest("localize5", bOnline); //good
        //RunTest("medpks010", bOnline); //good
        //RunTest("wumpus05", bOnline); //good
        //RunTest("unix1", bOnline); //good



        //Intersting problems:

        //RunTest("doors5longshort", bOnline); // stuck on generting new state from belife state
        //RunTest("blocks3Hardb2b", bOnline); //good
        //RunTest("localize3leftbetter", bOnline); // error - found falsified original clause.
        //RunTest("medpks010Uneven", bOnline); //parser problem
        //RunTest("wumpus05Uneven", bOnline); //parser problem
        //RunTest("unix1Uneven", bOnline); //parser problem




    }

    public static void Main(string[] args)
    {
        TestAll(true);
        //return;

        //TestClassicalFFCS();

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
            Run.RunPlanner(sDomainFile
                , sProblemFile,
                sOutputFile,
                bOnline);
        }
    }

    private static void TestClassicalFFCS()
    {
        string sDomainFile = @"C:\Users\shanigu\Downloads\domain-driver1.pddl";
        string sProblemFile = @"C:\Users\shanigu\Downloads\problem-driver1.pddl";
        MemoryStream ms = new MemoryStream();
        StreamWriter sw = new StreamWriter(ms);
        using (StreamReader sr = new StreamReader(sDomainFile))
        {
            string sDomain = sr.ReadToEnd();
            sw.Write(sDomain);
            sr.Close();
        }
        using (StreamReader sr = new StreamReader(sProblemFile))
        {
            string sProblem = sr.ReadToEnd();
            sw.Write(sProblem);
            sr.Close();
        }
        sw.Flush();
        ms.Position = 0;
        FF ff = new FF(ms);
        List<string> lPlan = ff.Plan();
    }
}