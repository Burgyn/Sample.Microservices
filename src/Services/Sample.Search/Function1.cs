using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Flurl.Http;
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
        public async static Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
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
                    log.LogInformation($"{item.Id}: {item.Name} - {item.Description})");
                }

                var a = await GetDocumentsAsync();
                await UploadDocumentsAsync(database.Query<Product>());
            }
            catch
            {
            }
        }

        private static async Task<string> GetDocumentsAsync()
        {
            string url = $"http://azsearch:8080/indexes/catalog/docs?api-version=2020-06-30-Preview&search=*";
            string value = await url
                .WithHeader("Content-Type", "application/json")
                .GetStringAsync();

            return value;
        }

        private static async Task UploadDocumentsAsync(IEnumerable<Product> documents)
        {
            var searchClient = CreateSearchIndexClient();
            var batch = IndexDocumentsBatch.Upload(documents);

            await searchClient.IndexDocumentsAsync(batch);
        }

        private static SearchClient CreateSearchIndexClient()
        {
            string searchServiceEndPoint = $"http://azsearch:8080";

            var indexClient = new SearchIndexClient(new Uri(searchServiceEndPoint), new AzureKeyCredential("key"));

            return indexClient.GetSearchClient("catalog");
        }

        [Alias("Products")]
        public class Product
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }
        }
    }
}
