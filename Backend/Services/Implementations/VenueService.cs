using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class VenueService : IVenueService
    {
        private readonly IVenueRepository _venueRepository;

        public VenueService(IVenueRepository venueRepository)
        {
            _venueRepository = venueRepository;
        }

        public async Task<ServiceResult<IEnumerable<Venue>>> GetVenuesAsync()
        {
            var venues = await _venueRepository.GetAllVenuesAsync();
            return ServiceResult<IEnumerable<Venue>>.Success(venues, 200);
        }

        public async Task<ServiceResult<Venue>> CreateVenueAsync(string name, string venueType, int capacity)
        {
            string cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
                return ServiceResult<Venue>.Failure("Venue name is required.", 400);

            if (capacity <= 0)
                return ServiceResult<Venue>.Failure("Venue capacity must be greater than zero.", 400);

            if (await _venueRepository.ExistsByNameAsync(cleanName))
                return ServiceResult<Venue>.Failure($"An event venue named '{cleanName}' already exists.", 409);

            var venue = new Venue
            {
                Name = cleanName,
                Type = string.IsNullOrWhiteSpace(venueType) ? "Event Hall" : venueType.Trim(),
                Capacity = capacity,
                IsActive = true
            };

            await _venueRepository.AddVenueAsync(venue);
            await _venueRepository.SaveChangesAsync();

            return ServiceResult<Venue>.Success(venue, 201);
        }

        public async Task<ServiceResult<Venue>> UpdateVenueAsync(int id, string name, string venueType, int capacity, bool isActive)
        {
            var venue = await _venueRepository.GetVenueByIdAsync(id);
            if (venue == null)
                return ServiceResult<Venue>.Failure("Target venue record not found.", 404);

            string cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
                return ServiceResult<Venue>.Failure("Venue name is required.", 400);

            if (await _venueRepository.ExistsByNameAsync(cleanName, id))
                return ServiceResult<Venue>.Failure($"An event venue named '{cleanName}' already exists.", 409);

            venue.Name = cleanName;
            if (!string.IsNullOrWhiteSpace(venueType))
                venue.Type = venueType.Trim();
            
            if (capacity > 0)
                venue.Capacity = capacity;

            venue.IsActive = isActive;

            _venueRepository.UpdateVenue(venue);
            await _venueRepository.SaveChangesAsync();

            return ServiceResult<Venue>.Success(venue, 200);
        }

        public async Task<ServiceResult<object>> DeleteVenueAsync(int id)
        {
            var venue = await _venueRepository.GetVenueByIdAsync(id);
            if (venue == null)
                return ServiceResult<object>.Failure("Target venue record not found.", 404);

            bool hasEvents = await _venueRepository.VenueHasUpcomingEventsAsync(id);
            if (hasEvents)
                return ServiceResult<object>.Failure("Cannot deactivate venue while upcoming events are scheduled at this location.", 400);

            venue.IsActive = false;
            _venueRepository.UpdateVenue(venue);
            await _venueRepository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Venue deactivated successfully." }, 200);
        }

        public async Task<ServiceResult<object>> CheckAvailabilityAsync(int id, DateTime from, DateTime to)
        {
            if (from >= to)
                return ServiceResult<object>.Failure("Start time must occur before end time.", 400);

            bool isDoubleBooked = await _venueRepository.IsVenueDoubleBookedAsync(id, from, to);
            return ServiceResult<object>.Success(new { VenueId = id, IsAvailable = !isDoubleBooked }, 200);
        }
    }
}
