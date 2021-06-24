using MMLib.ToString.Abstraction;

namespace Sample.Basket.Domain
{
    [ToString]
    public partial class BasketItem
    {
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
