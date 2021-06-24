using Kros.KORM;
using Sample.Basket.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Basket.Infrastructure
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDatabase _database;

        public BasketRepository(IDatabase database)
        {
            _database = database;
        }

        public Task CreateAsync(Domain.Basket basket)
            => _database.AddAsync(basket);

        public Task DeleteAsync(int id)
            => _database.DeleteAsync<Domain.Basket>(id);

        public Task UpdateAsync(Domain.Basket basket)
            => _database.UpsertAsync(basket);

        public Domain.Basket Get(int id)
            => _database.Query<Domain.Basket>().FirstOrDefault(p => p.BuyerId == id);

        public IEnumerable<Domain.Basket> GetAll()
            => _database.Query<Domain.Basket>();
    }
}
