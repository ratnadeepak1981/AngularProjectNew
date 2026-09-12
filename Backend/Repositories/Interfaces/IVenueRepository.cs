using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IVenueRepository
    {
        Task<IEnumerable<Venue>> GetAllVenuesAsync();
        Task<Venue?> GetVenueByIdAsync(int venueId);
        Task<bool> ExistsByNameAsync(string name, int? excludeVenueId = null);
        Task AddVenueAsync(Venue venue);
        void UpdateVenue(Venue venue);
        Task<bool> VenueHasUpcomingEventsAsync(int venueId);
        Task<bool> IsVenueDoubleBookedAsync(int venueId, DateTime start, DateTime end);
        Task SaveChangesAsync();
    }
}
