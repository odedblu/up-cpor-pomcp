﻿
using System;
using System.Diagnostics;

namespace CPORLib.Tools
{
    class RandomGenerator
    {
        private static Random m_rnd = new Random(2);
        public static void Init(int iSeed)
        {
            Debug.WriteLine("Init random seed to: " + iSeed);
            m_rnd = new Random(iSeed);
        }
        public static void Init()
        {
            Debug.WriteLine("Init random seed randomly");
            m_rnd = new Random();
        }
        public static int Next(int iMax)
        {
            return m_rnd.Next(iMax);
        }
        public static double NextDouble()
        {
            return m_rnd.NextDouble();
        }

        internal static int Next(int min, int max)
        {
            return m_rnd.Next(min, max);
        }
    }
}
