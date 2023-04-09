using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public class WeightsRepository : IRepository<ChessWeights>
    {
        public WeightsRepository(IDictionary<Guid, ChessWeights> weights)
        {
            this.weights = weights;
        }
        public IEnumerable<ChessWeights> GetAll()
        {
            return weights.Values;
        }

        public IEnumerable<ChessWeights> Where(Func<ChessWeights, bool> predicate)
        {
            return weights.Values.Where(predicate);
        }

        public ChessWeights? FirstOrDefault(Func<ChessWeights, bool> predicate)
        {
            return weights.Values.FirstOrDefault(predicate);
        }

        public void Update(ChessWeights item)
        {
            weights[item.Id] = item;
        }

        public void Insert(ChessWeights item)
        {
            weights.Add(item.Id, item);
        }

        public void Delete(Guid id)
        {
            weights.Remove(id);
        }

        private IDictionary<Guid, ChessWeights> weights;
    }
}
