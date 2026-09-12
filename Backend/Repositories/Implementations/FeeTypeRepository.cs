using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class FeeTypeRepository : IFeeTypeRepository
    {
        private readonly AppDbContext _context;

        public FeeTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FeeType>> GetActiveFeeTypesAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FeeTypes]') AND name = 'IsActive') BEGIN ALTER TABLE [dbo].[FeeTypes] ADD [IsActive] BIT NOT NULL CONSTRAINT DF_FeeTypes_IsActive DEFAULT 1; END"
                );
            }
            catch { }

            var list = await _context.FeeTypes.ToListAsync();
            if (list.Count < 5)
            {
                var defaults = new List<string>
                {
                    "Tuition Fee",
                    "Lab Fine / Equipment Fee",
                    "Hostel Accommodation Fee",
                    "Library Fine & Late Return",
                    "Student Identity Card Renewal Fee"
                };

                bool added = false;
                foreach (var name in defaults)
                {
                    if (!list.Any(f => f.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        await _context.FeeTypes.AddAsync(new FeeType { Name = name });
                        added = true;
                    }
                }

                if (added)
                {
                    await _context.SaveChangesAsync();
                    list = await _context.FeeTypes.ToListAsync();
                }
            }

            return list;
        }

        public async Task<FeeType?> GetFeeTypeByIdAsync(int id)
        {
            return await _context.FeeTypes.FindAsync(id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeFeeTypeId = null)
        {
            string cleanName = name.Trim().ToLower();
            var query = _context.FeeTypes.Where(f => f.Name.ToLower() == cleanName);
            if (excludeFeeTypeId.HasValue) query = query.Where(f => f.Id != excludeFeeTypeId.Value);
            return await query.AnyAsync();
        }

        public async Task AddFeeTypeAsync(FeeType feeType)
        {
            await _context.FeeTypes.AddAsync(feeType);
        }

        public void UpdateFeeType(FeeType feeType)
        {
            _context.FeeTypes.Update(feeType);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
