using Kros.KORM;
using Sample.Ordering.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Ordering.Infrastructure
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IDatabase _database;

        public OrderRepository(IDatabase database)
        {
            _database = database;
        }

        public Task CreateAsync(Order order)
            => _database.AddAsync(order);

        public Order Get(int id)
            => _database.Query<Order>().FirstOrDefault(p => p.Id == id);

        public IEnumerable<Order> GetAll(int buyerId)
            => _database.Query<Order>().Where(p => p.BuyerId == buyerId);
    }
}
