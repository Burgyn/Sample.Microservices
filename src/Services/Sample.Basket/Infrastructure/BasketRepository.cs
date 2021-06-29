using Microsoft.Extensions.Caching.Distributed;
using Sample.Basket.Base;
using Sample.Basket.Domain;
using System;
using System.Threading.Tasks;

namespace Sample.Basket.Infrastructure
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDistributedCache _cache;

        public BasketRepository(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task CreateAsync(Domain.Basket basket)
            => _cache.SetAsync(basket.BuyerId.ToString(), basket, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
            });

        public Task DeleteAsync(int id)
            => _cache.RemoveAsync(id.ToString());

        public Task<Domain.Basket> GetAsync(int id)
            => _cache.GetAsync<Domain.Basket>(id.ToString());
    }
}
