using Kros.KORM;
using Kros.KORM.Metadata;
using Sample.Catalog.Domain;

namespace Sample.Catalog.Infrastructure
{
    public class DatabaseConfiguration: DatabaseConfigurationBase
    {
        public override void OnModelCreating(ModelConfigurationBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasTableName("Products")
                .HasPrimaryKey(p => p.Id).AutoIncrement(AutoIncrementMethodType.Identity);
        }
    }
}
