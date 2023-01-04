using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using Pedantic.Chess;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class NegamaxTests
    {
        [TestMethod]
        public void SearchTest()
        {
            Board bd = new(Constants.FEN_START_POS);
            TimeControl time = new()
            {
                Infinite = true
            };
            time.Reset();


            Negamax search = new(bd, time, 8);
            Task<short> task = Task.Run(() => search.Search());
            task.Wait();
        }

        [TestMethod]
        public void SearchTest2()
        {
            Board bd = new(Constants.FEN_START_POS);
            TimeControl time = new()
            {
                Infinite = true
            };
            time.Reset();

            Negamax search = new(bd, time, 8);
            Task<short> task = Task.Run(() => search.Search());
            task.Wait();

            bd.MakeMove(search.PV.Moves[0][0]);
            Console.WriteLine();

            TtEval.Clear();
            time.Reset();
            task = Task.Run(() => search.Search());
            task.Wait();
        }

    }
}