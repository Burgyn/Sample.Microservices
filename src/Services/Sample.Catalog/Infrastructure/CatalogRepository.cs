using Kros.KORM;
using Sample.Catalog.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Catalog.Infrastructure
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly IDatabase _database;

        public CatalogRepository(IDatabase database)
        {
            _database = database;
        }

        public async Task CreateAsync(Product product)
            => await _database.AddAsync(product);

        public async Task DeleteAsync(int id)
            => await _database.DeleteAsync<Product>(id);

        public Task UpdateAsync(Product product)
            => _database.EditAsync(product);

        public Product Get(int id)
            => _database.Query<Product>().FirstOrDefault();

        public IEnumerable<Product> GetAll()
            => _database.Query<Product>();
    }
}
