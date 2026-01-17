using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using System.Linq.Expressions;

namespace otherServices.Repositories
{
    public interface ILandlordRepository : IGenericRepository<Landlord>
    {
        Task<Landlord> AcceptUserAsync(long landlordId);
        Task<Landlord> RejectUserAsync(long landlordId);
        Task<IEnumerable<Landlord>> FindAsync(Expression<Func<Landlord, bool>> predicate);

    }
}
