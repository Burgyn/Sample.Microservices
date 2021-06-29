using AutoBogus;
using AutoBogus.Conventions;
using Microsoft.Extensions.Caching.Distributed;
using Sample.Basket.Base;
using Sample.Basket.Domain;
using System;
using System.Linq;

namespace Sample.Basket.Infrastructure
{
    public class DummyDataInitializer
    {
        private readonly IDistributedCache _cache;

        static DummyDataInitializer()
        {
            AutoFaker.Configure(builder =>
            {
                builder.WithConventions();
            });
        }

        public DummyDataInitializer(IDistributedCache cache)
        {
            _cache = cache;
        }

        public void Init()
        {
            if (_cache.GetString("1") is null)
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
                foreach (var basket in baskets)
                {
                    _ = _cache.SetAsync(basket.BuyerId.ToString(), basket, new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    });
                }
            }
        }
    }
}
