using AutoBogus;
using AutoBogus.Conventions;
using Kros.KORM;
using Sample.Basket.Domain;
using System;
using System.Linq;

namespace Sample.Basket.Infrastructure
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
            if (!_database.Query<Domain.Basket>().Any())
            {
                var basketItemFaker = new AutoFaker<BasketItem>()
                    .RuleFor(fake => fake.UnitPrice, fake => fake.Random.Int(5, 100))
                    .RuleFor(fake => fake.Quantity, fake => fake.Random.Int(1, 10));

                var random = new Random(50);
                var baskets = Enumerable.Range(1, 11).Select(r => new Domain.Basket()
                {
                    BuyerId = r,
                    Items = basketItemFaker.Generate(random.Next(3, 10))
                });
                _database.BulkAddAsync(baskets);
            }
        }
    }
}
