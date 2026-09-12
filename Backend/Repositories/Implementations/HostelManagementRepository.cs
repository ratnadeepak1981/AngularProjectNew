using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CampusServicesPortal.Repositories
{
    public class HostelManagementRepository : IHostelManagementRepository
    {
        private readonly AppDbContext _context;

        public HostelManagementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Hostel?> GetHostelByIdAsync(int id)
        {
            return await _context.Hostels.FindAsync(id);
        }

        public async Task<Room?> GetRoomByIdAsync(int id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        public async Task<bool> HostelHasActiveOccupantsAsync(int hostelId)
        {
            // FIX: Look up the room ID array first to bypass deep entity relationship cascade errors
            var roomIds = await _context.Rooms
                .Where(r => r.HostelId == hostelId)
                .Select(r => r.Id)
                .ToListAsync();

            return await _context.HostelApplications
                .AnyAsync(a => a.AssignedRoomId.HasValue && roomIds.Contains(a.AssignedRoomId.Value) && (a.Status == "RoomAssigned" || a.Status == "Room Assigned"));
        }


        public async Task<int> GetRoomCurrentOccupancyAsync(int roomId)
        {
            // FIX: Match against AssignedRoomId instead of RoomId; handle both RoomAssigned and Room Assigned
            return await _context.HostelApplications
                .CountAsync(a => a.AssignedRoomId == roomId && (a.Status == "RoomAssigned" || a.Status == "Room Assigned"));
        }


        public async Task AddHostelAsync(Hostel hostel)
        {
            await _context.Hostels.AddAsync(hostel);
        }

        public async Task AddRoomAsync(Room room)
        {
            await _context.Rooms.AddAsync(room);
        }

        public async Task UpdateHostelAsync(Hostel hostel)
        {
            _context.Hostels.Update(hostel);
            await Task.CompletedTask;
        }

        public async Task UpdateRoomAsync(Room room)
        {
            _context.Rooms.Update(room);
            await Task.CompletedTask;
        }

        public async Task<bool> HostelNameExistsAsync(string name, int? excludeHostelId = null)
        {
            string cleanName = name.Trim().ToLower();
            var query = _context.Hostels
                .Where(h => h.Name.ToLower() == cleanName);

            if (excludeHostelId.HasValue)
            {
                query = query.Where(h => h.Id != excludeHostelId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> RoomNumberExistsAsync(int hostelId, string roomNumber, int? excludeRoomId = null)
        {
            string cleanNum = roomNumber.Trim().ToLower();
            var query = _context.Rooms
                .Where(r => r.HostelId == hostelId && r.RoomNumber.ToLower() == cleanNum);

            if (excludeRoomId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRoomId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
