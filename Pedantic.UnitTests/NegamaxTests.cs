﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            TtTran.Resize(128);
            Board bd = new(Constants.FEN_START_POS);
            TimeControl time = new();
            time.Go(400000, 6000, 40);
            time.Reset();


            PedanticSearch search = new(bd, time, 6);
            Task task = Task.Run(() => search.Search());
            task.Wait();
        }

        [TestMethod]
        public void SearchTest2()
        {
            TtTran.Resize(128);
            Board bd = new(Constants.FEN_START_POS);
            TimeControl time = new()
            {
                Infinite = true
            };
            time.Reset();

            PedanticSearch search = new(bd, time, 4);
            Task task = Task.Run(() => search.Search());
            task.Wait();

            bd.MakeMove(search.Result.Pv[0]);
            Console.WriteLine();

            TtTran.Clear();
            time.Reset();
            search = new(bd, time, 3);
            task = Task.Run(() => search.Search());
            task.Wait();
        }

    }
}