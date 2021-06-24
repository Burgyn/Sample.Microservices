using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Sample.Users.Domain;
using System;
using System.Threading.Tasks;
using TableStorage.Abstractions.TableEntityConverters;

namespace Sample.Users.Infrastructure
{
    public class UsersRepository : IUsersRepository
    {
        protected readonly Lazy<CloudTable> _table;
        private readonly string _connectionString;

        public UsersRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            _table = new Lazy<CloudTable>(InitializeTable);
        }

        private CloudTable InitializeTable()
        {
            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable cloudTable = tableClient.GetTableReference("Users");

            return cloudTable;
        }

        public async Task CreateAsync(User user)
        {
            var operation = TableOperation.InsertOrMerge(ToTableEntity(user));

            await _table.Value.ExecuteAsync(operation);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await GetAsync(id);

            var deleteOperation = TableOperation.Delete(ToTableEntity(user));

            await _table.Value.ExecuteAsync(deleteOperation);
        }

        private static DynamicTableEntity ToTableEntity(User user)
            => user.ToTableEntity("1", user.Id.ToString());

        public async Task UpdateAsync(User user)
        {
            var operation = TableOperation.InsertOrReplace(ToTableEntity(user));

            await _table.Value.ExecuteAsync(operation);
        }

        public async Task<User> GetAsync(int id)
        {
            var operation = TableOperation.Retrieve<DynamicTableEntity>("1", id.ToString());
            TableResult result = await _table.Value.ExecuteAsync(operation);

            return EntityConvert.FromTableEntity<User, object, object>(result.Result as DynamicTableEntity, null, null);
        }
    }
}
