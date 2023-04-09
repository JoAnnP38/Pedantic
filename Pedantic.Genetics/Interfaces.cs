using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> Where(Func<T, bool> predicate);
        T? FirstOrDefault(Func<T, bool> predicate);
        void Update(T item);
        void Insert(T item);
        void Delete(Guid id);
    }
}
