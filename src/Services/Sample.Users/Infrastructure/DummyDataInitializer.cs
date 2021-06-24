using AutoBogus;
using AutoBogus.Conventions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Sample.Users.Domain;

namespace Sample.Users.Infrastructure
{
    public class DummyDataInitializer
    {
        private readonly string _connectionString;
        private readonly IUsersRepository _repository;

        static DummyDataInitializer()
        {
            AutoFaker.Configure(builder =>
            {
                builder.WithConventions();
            });
        }

        public DummyDataInitializer(IConfiguration configuration, IUsersRepository repository)
        {
            _connectionString = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            _repository = repository;
        }

        public void Init()
        {
            var table = InitializeTable();
            var operation = TableOperation.Retrieve<DynamicTableEntity>("1", "1");
            TableResult result = table.Execute(operation);

            if (result.Result is null)
            {
                int id = 1;
                var userFaker = new AutoFaker<User>()
                    .RuleFor(fake => fake.Id, fake => id++);
                var users = userFaker.Generate(10);

                foreach (var user in users)
                {
                    _repository.CreateAsync(user);
                }
            }
        }

        private CloudTable InitializeTable()
        {
            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable cloudTable = tableClient.GetTableReference("Users");

            return cloudTable;
        }
    }
}
