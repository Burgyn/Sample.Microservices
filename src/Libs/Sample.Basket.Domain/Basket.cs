using MMLib.ToString.Abstraction;
using System.Collections.Generic;
using System.Linq;

namespace Sample.Basket.Domain
{
    /// <summary>
    /// User
    /// </summary>
    [ToString]
    public partial class Basket
    {
        public int BuyerId { get; set; }

        public decimal TotalPrice => Items.Sum(p => p.TotalPrice);

        public IEnumerable<BasketItem> Items { get; set; }
    }
}
