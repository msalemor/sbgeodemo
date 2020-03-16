using System.Collections.Generic;
using System.Threading.Tasks;
using SBDemo.Domain.Models;

namespace SBDemo.Domain.Repositories
{
    public interface IRepository
    {
        Task<IEnumerable<OnlineTransaction>> GetAsync();
        Task<OnlineTransaction> GetAsync(string id);
        Task SaveTransactionAsync(OnlineTransaction onlineTransaction);
    }
}