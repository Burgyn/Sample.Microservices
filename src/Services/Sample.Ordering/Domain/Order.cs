﻿using MMLib.ToString.Abstraction;
using System.Collections.Generic;
using System.Linq;

namespace Sample.Ordering.Domain
{
    /// <summary>
    /// User
    /// </summary>
    [ToString]
    public partial class Order
    {
        public int Id { get; set; }

        public int BuyerId { get; set; }

        public decimal TotalPrice => Items.Sum(p => p.TotalPrice);

        public IEnumerable<OrderItem> Items { get; set; }
    }
}
