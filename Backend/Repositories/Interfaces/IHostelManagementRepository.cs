using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IHostelManagementRepository
    {
        Task<Hostel?> GetHostelByIdAsync(int id);
        Task<Room?> GetRoomByIdAsync(int id);
        Task<bool> HostelHasActiveOccupantsAsync(int hostelId);
        Task<int> GetRoomCurrentOccupancyAsync(int roomId);
        Task AddHostelAsync(Hostel hostel);
        Task AddRoomAsync(Room room);
        Task UpdateHostelAsync(Hostel hostel);
        Task UpdateRoomAsync(Room room);
        Task<bool> HostelNameExistsAsync(string name, int? excludeHostelId = null);
        Task<bool> RoomNumberExistsAsync(int hostelId, string roomNumber, int? excludeRoomId = null);
        Task SaveChangesAsync();
    }
}
