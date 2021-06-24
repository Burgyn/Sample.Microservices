using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sample.Users.Domain
{
    /// <summary>
    /// Interface which describe users repository.
    /// </summary>
    public interface IUsersRepository
    {
        Task<User> GetAsync(int id);

        Task CreateAsync(User user);

        Task UpdateAsync(User user);

        Task DeleteAsync(int id);
    }
}
