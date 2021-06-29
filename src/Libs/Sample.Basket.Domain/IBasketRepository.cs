using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample.Basket.Domain
{
    public interface IBasketRepository
    {
        Task<Basket> GetAsync(int id);

        Task CreateAsync(Basket user);

        Task DeleteAsync(int id);
    }
}
