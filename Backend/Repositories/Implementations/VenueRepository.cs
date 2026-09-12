using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class VenueRepository : IVenueRepository
    {
        private readonly AppDbContext _context;

        public VenueRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
            return await _context.Venues
                .ToListAsync();
        }

        public async Task<Venue?> GetVenueByIdAsync(int venueId)
        {
            return await _context.Venues.FindAsync(venueId);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeVenueId = null)
        {
            string cleanName = name.Trim().ToLower();
            var query = _context.Venues.Where(v => v.Name.ToLower() == cleanName);
            if (excludeVenueId.HasValue) query = query.Where(v => v.Id != excludeVenueId.Value);
            return await query.AnyAsync();
        }

        public async Task AddVenueAsync(Venue venue)
        {
            await _context.Venues.AddAsync(venue);
        }

        public void UpdateVenue(Venue venue)
        {
            _context.Venues.Update(venue);
        }

        public async Task<bool> VenueHasUpcomingEventsAsync(int venueId)
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .AnyAsync(e => e.VenueId == venueId && e.StartDateTime >= now);
        }

        public async Task<bool> IsVenueDoubleBookedAsync(int venueId, DateTime start, DateTime end)
        {
            return await _context.Events
                .AnyAsync(e => e.VenueId == venueId && e.StartDateTime < end && e.EndDateTime > start);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
