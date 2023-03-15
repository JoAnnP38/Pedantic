using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Utilities
{
    public static class ArrayEx
    {

        public static T[] Clone<T>(T[] array)
        {
            T[] clone = new T[array.Length];
            Array.Copy(array, clone, array.Length);
            return clone;
        }
    }
}
