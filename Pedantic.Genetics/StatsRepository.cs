using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public class StatsRepository : IRepository<ChessStats>
    {
        public StatsRepository(IDictionary<Guid, ChessStats> stats)
        {
            this.stats = stats;
        }

        public IEnumerable<ChessStats> GetAll()
        {
            return stats.Values;
        }

        public IEnumerable<ChessStats> Where(Func<ChessStats, bool> predicate)
        {
            return stats.Values.Where(predicate);
        }

        public ChessStats? FirstOrDefault(Func<ChessStats, bool> predicate)
        {
            return stats.Values.FirstOrDefault(predicate);
        }

        public void Update(ChessStats item)
        {
            stats[item.Id] = item;
        }

        public void Insert(ChessStats item)
        {
            stats.Add(item.Id, item);
        }

        public void Delete(Guid id)
        {
            stats.Remove(id);
        }

        private readonly IDictionary<Guid, ChessStats> stats;
    }
}
