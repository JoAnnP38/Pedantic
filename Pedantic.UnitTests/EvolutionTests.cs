using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Genetics;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvolutionTests
    {
        [TestInitialize]
        public void Initialize()
        {
            using var rep = new GeneticsRepository();
            rep.DeleteAll();
        }
    }
}