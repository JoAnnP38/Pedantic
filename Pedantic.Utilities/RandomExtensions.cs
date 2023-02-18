using System;


namespace Pedantic.Utilities
{
    public static class RandomExtensions
    {
        public static double NextGaussian(this Random random, double mu = 0.0, double sigma = 1.0)
        {
            double r1 = 1.0 - random.NextDouble();
            double r2 = 1.0 - random.NextDouble();

            double gaussianNormal = Math.Sqrt(-2.0 * Math.Log(r1)) * Math.Sin(2.0 * Math.PI * r2);

            return mu + gaussianNormal * sigma;
        }

        public static bool NextBoolean(this Random random)
        {
            return random.Next(2) > 0;
        }

        public static void Shuffle<T>(this Random random, T[] array)
        {
            for (int n = 0; n < array.Length; n++)
            {
                int m = random.Next(0, n + 1);
                (array[n], array[m]) = (array[m], array[n]);
            }
        }

        public static void Shuffle<T>(this Random random, IList<T> list)
        {
            for (int n = 0; n < list.Count; n++)
            {
                int m = random.Next(0, n + 1);
                (list[n], list[m]) = (list[m], list[n]);
            }
        }
    }
}
