﻿using CPORLib.LogicalUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CPORLib.Tools
{
 
    public class GenericArraySet<T> :  ISet<T> where T: Indexed<T>
    {

        public static int Max = 10000;
        



        public bool[] All;
        public List<T> Items;
        public int Sum;

        public int Count
        {
            get 
            { 
                return Items.Count;
            }
        }

        public bool IsReadOnly => throw new NotImplementedException();

        public GenericArraySet()
        {
            All = new bool[Max];
            Items = new List<T>();
            Sum = 0;
        }

        public GenericArraySet(IEnumerable<T> hs) : this()
        {
            foreach (T t in hs)
                Add(t);
        }

       void ICollection<T>.Add(T t)
        {
            throw new NotImplementedException();
        }

        

        public bool Add(T t)
        {
            int index = t.Index;
            if (All[index] == false)
            {
                All[index] = true;
                Items.Add(t);
                Sum += index;
                return true;
            }
            return false;
        }

        private bool RemoveImpl(T t)
        {
            int index = t.Index;
            if (All[index] == true)
            {
                All[index] = false;
                Items.Remove(t);
                Sum -= index;
                return true;
            }
            return false;
        }

        bool ICollection<T>.Remove(T t)
        {
            return RemoveImpl(t);
        }

        public bool Remove(T t)
        {
            return RemoveImpl(t);
        }
        public bool IsSubsetOf(GenericArraySet<T> other)
        {
            if (Items.Count > other.Items.Count)
                return false;
            if (Sum > other.Sum)
                return false;
            foreach (T t in Items)
            {
                int index = t.Index;
                if (other.All[index] == false)
                    return false;
            }
            return true;
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other is GenericArraySet<T> set)
                return IsSubsetOf(set);
            throw new NotImplementedException();
        }

        bool ICollection<T>.Contains(T t)
        {
            int index = t.Index;

            return All[index];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public void UnionWith(IEnumerable<T> hs)
        {
            foreach (T t in hs)
                Add(t);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }


        public bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }


        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            string s = "[";
            foreach(T item in Items)
            {
                s += item.ToString() + ",";
            }
            s+= "]";
            return s;
        }
    }

}



