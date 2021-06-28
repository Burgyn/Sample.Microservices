using Kros.KORM;
using Kros.KORM.Converter;
using Kros.KORM.Metadata;
using Sample.Ordering.Domain;
using System.Collections.Generic;

namespace Sample.Ordering.Infrastructure
{
    public class DatabaseConfiguration: DatabaseConfigurationBase
    {
        public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .HasTableName("Orders")
                .HasPrimaryKey(p => p.Id).AutoIncrement(AutoIncrementMethodType.Identity)
                .Property(p => p.Items).UseConverter<JsonConverter<List<OrderItem>>>();
        }
    }
}
