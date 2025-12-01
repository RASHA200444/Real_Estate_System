using otherServices.Models;
using otherServices.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace otherServices.Repositories
{
    public class LandlordRepository : GenericRepository<Landlord>, ILandlordRepository
    {
        private readonly AppDbContext2 _context;

        public LandlordRepository(AppDbContext2 context):base(context)
        {
            _context = context;
        }

        public async Task<Landlord> AcceptUserAsync(long landlordId)
        {
            var Landlord = await GetByIdAsync(landlordId);
            if (Landlord == null)
                throw new KeyNotFoundException("Landlord not found");

            Landlord.PendingStatus = PendingStatus.Active;
            Landlord.OwnershipDocPathEvaluation = AIDecision.Verified;
            await SaveChangesAsync();
            return Landlord;
        }

        public async Task<Landlord> RejectUserAsync(long landlordId)
        {
            var Landlord = await GetByIdAsync(landlordId);
            if (Landlord == null)
                throw new KeyNotFoundException("Landlord not found");

            Landlord.PendingStatus = PendingStatus.Blocked;
            Landlord.OwnershipDocPathEvaluation = AIDecision.Fraudulent;

            await SaveChangesAsync();
            return Landlord;
        }
    }
}
