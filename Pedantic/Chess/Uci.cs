using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public static class Uci
    {
        public static void Log(string message)
        {
            Console.WriteLine($@"info string {message}");
        }
    }
}
