using Kros.KORM;
using Kros.KORM.Converter;
using Kros.KORM.Metadata;
using Sample.Basket.Domain;
using System.Collections.Generic;

namespace Sample.Basket.Infrastructure
{
    public class DatabaseConfiguration: DatabaseConfigurationBase
    {
        public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Domain.Basket>()
                .HasPrimaryKey(p => p.BuyerId)
                .Property(p => p.Items).UseConverter<JsonConverter<List<BasketItem>>>();
        }
    }
}
