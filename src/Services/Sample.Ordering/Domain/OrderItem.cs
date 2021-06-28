using MMLib.ToString.Abstraction;

namespace Sample.Ordering.Domain
{
    [ToString]
    public partial class OrderItem
    {
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
