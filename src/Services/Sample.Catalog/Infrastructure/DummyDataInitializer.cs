using AutoBogus;
using AutoBogus.Conventions;
using Kros.KORM;
using Sample.Catalog.Domain;
using System.Linq;

namespace Sample.Catalog.Infrastructure
{
    public class DummyDataInitializer
    {
        private readonly IDatabase _database;

        static DummyDataInitializer()
        {
            AutoFaker.Configure(builder =>
            {
                builder.WithConventions();
            });
        }

        public DummyDataInitializer(IDatabase database)
        {
            _database = database;
        }

        public void Init()
        {
            if (!_database.Query<Product>().Any())
            {
                int id = 1;
                var productFaker = new AutoFaker<Product>()
                    .RuleFor(fake => fake.Description, fake => fake.Commerce.ProductDescription())
                    .RuleFor(fake => fake.Name, fake => fake.Commerce.ProductName())
                    .RuleFor(fake => fake.Price, fake => fake.Random.Int(5, 100))
                    .RuleFor(fake => fake.Id, fake => id++);

                var products = productFaker.Generate(50);
                _database.BulkAddAsync(products);
            }
        }
    }
}
