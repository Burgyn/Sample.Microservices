using System;
using Kros.KORM;
using Kros.KORM.Metadata.Attribute;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sample.Search
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Start search sync at: {DateTime.Now}.");
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            var connection = config["Catalog"];
            try
            {
               using var database = Database.Builder.UseConnection(connection).Build();

               foreach (var item in database.Query<Product>())
               {
                   log.LogInformation($"{item.Id}: {item.Name} - {item.Description} ({item.Price})");
               }
            }
            catch
            {
            }
        }

        [Alias("Products")]
        public class Product
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public decimal Price { get; set; }
        }
    }
}
