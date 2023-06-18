using CPORLib.LogicalUtilities;
using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace CPORLib.Tools
{
    public class Indexed<T>
    {
        private int m_iIndex;
        public int Index
        {
            get
            {
                if (m_iIndex == -1)
                {
                    SetIndex();
                }
                return m_iIndex;
            }
        }

        public static Dictionary<Indexed<T>, int> Indexes = new Dictionary<Indexed<T>, int>();
        public static int CountIndexes = 0;

        public static void Reset()
        {
            foreach(var t in Indexes.Keys)
                t.m_iIndex = -1;
            Indexes = new Dictionary<Indexed<T>, int>();
            CountIndexes = 0;
        }

        public Indexed()
        {
            m_iIndex = -1;
        }

        private void SetIndex()
        {
            /*
            if (ToString().Contains("got-"))
            {
                var v = this.GetType();
                Console.Write("*");
            }
            */
            if (!Indexes.TryGetValue(this, out int index))
            {
                
                index = CountIndexes;
                Indexes[this] = index;
                CountIndexes++;
            }
            m_iIndex = index;

        }
    }
}
