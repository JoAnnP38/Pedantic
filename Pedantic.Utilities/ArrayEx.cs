// ***********************************************************************
// Assembly         : Pedantic.Utilities
// Author           : JoAnn D. Peeler
// Created          : 03-13-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="ArrayEx.cs" company="Pedantic.Utilities">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Additional array functionality.
// </summary>
// ***********************************************************************
namespace Pedantic.Utilities
{
    public static class ArrayEx
    {

        public static T[] Clone<T>(T[] array)
        {
            var clone = new T[array.Length];
            Array.Copy(array, clone, array.Length);
            return clone;
        }
    }
}
