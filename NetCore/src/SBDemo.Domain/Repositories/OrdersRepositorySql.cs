using SBDemo.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBDemo.Domain.Repositories
{
    public class OrdersRepositorySql : IRepository
    {
        public async Task<IEnumerable<OnlineTransaction>> GetAsync()
        {
            return null;
        }

        public async Task<OnlineTransaction> GetAsync(string id)
        {
            return null;
        }

        public async Task SaveTransactionAsync(OnlineTransaction onlineTransaction)
        {

        }
    }
}
