using CampusServicesPortal.DTOs.Requests.Hostel.Managment;
using CampusServicesPortal.DTOs.Responses.Hostel.Management;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class HostelManagementService : IHostelManagementService
    {
        private readonly IHostelManagementRepository _repository;

        public HostelManagementService(IHostelManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult<HostelResponseDto>> CreateHostelAsync(CreateHostelRequestDto request)
        {
            // Removed location assignment
            var hostel = new Hostel { Name = request.Name, IsActive = true };
            await _repository.AddHostelAsync(hostel);
            await _repository.SaveChangesAsync();

            var res = new HostelResponseDto { Id = hostel.Id, Name = hostel.Name, IsActive = hostel.IsActive };
            return ServiceResult<HostelResponseDto>.Success(res, 201);
        }

        public async Task<ServiceResult<HostelResponseDto>> UpdateHostelAsync(int id, UpdateHostelRequestDto request)
        {
            var hostel = await _repository.GetHostelByIdAsync(id);
            if (hostel == null) return ServiceResult<HostelResponseDto>.Failure("Hostel record not found.", 404);

            hostel.Name = request.Name;
            hostel.IsActive = request.IsActive;

            await _repository.UpdateHostelAsync(hostel);
            await _repository.SaveChangesAsync();

            var res = new HostelResponseDto { Id = hostel.Id, Name = hostel.Name, IsActive = hostel.IsActive };
            return ServiceResult<HostelResponseDto>.Success(res, 200);
        }


        public async Task<ServiceResult<object>> DeleteHostelAsync(int id)
        {
            var hostel = await _repository.GetHostelByIdAsync(id);
            if (hostel == null) return ServiceResult<object>.Failure("Hostel record not found.", 404);

            // Business Rule: A hostel cannot be deleted while it has rooms with active occupants; deactivate instead [PDF: 0.1.7]
            if (await _repository.HostelHasActiveOccupantsAsync(id))
                return ServiceResult<object>.Failure("Hostel has active occupants. Deactivate the building state instead.", 400);

            hostel.IsActive = false;
            await _repository.UpdateHostelAsync(hostel);
            await _repository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Hostel building deactivated successfully." }, 200);
        }

        public async Task<ServiceResult<RoomResponseDto>> CreateRoomAsync(int hostelId, CreateRoomRequestDto request)
        {
            var hostel = await _repository.GetHostelByIdAsync(hostelId);
            if (hostel == null) return ServiceResult<RoomResponseDto>.Failure("Target hostel building not found.", 404);

            string cleanRoomNumber = request.RoomNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanRoomNumber))
                return ServiceResult<RoomResponseDto>.Failure("Room number or identifier is required.", 400);

            if (request.MaxCapacity <= 0)
                return ServiceResult<RoomResponseDto>.Failure("Max student capacity must be greater than zero.", 400);

            if (await _repository.RoomNumberExistsAsync(hostelId, cleanRoomNumber))
                return ServiceResult<RoomResponseDto>.Failure($"Room number '{cleanRoomNumber}' already exists in this hostel building.", 409);

            var room = new Room { HostelId = hostelId, RoomNumber = cleanRoomNumber, MaxCapacity = request.MaxCapacity, IsActive = true };
            await _repository.AddRoomAsync(room);
            await _repository.SaveChangesAsync();

            var res = new RoomResponseDto { Id = room.Id, HostelId = room.HostelId, RoomNumber = room.RoomNumber, MaxCapacity = room.MaxCapacity, IsActive = room.IsActive };
            return ServiceResult<RoomResponseDto>.Success(res, 201);
        }

        public async Task<ServiceResult<RoomResponseDto>> UpdateRoomAsync(int id, UpdateRoomRequestDto request)
        {
            var room = await _repository.GetRoomByIdAsync(id);
            if (room == null) return ServiceResult<RoomResponseDto>.Failure("Room record not found.", 404);

            string cleanRoomNumber = request.RoomNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanRoomNumber))
                return ServiceResult<RoomResponseDto>.Failure("Room number or identifier is required.", 400);

            if (request.MaxCapacity <= 0)
                return ServiceResult<RoomResponseDto>.Failure("Max student capacity must be greater than zero.", 400);

            int currentOccupancy = await _repository.GetRoomCurrentOccupancyAsync(id);
            // Business Rule: A room's capacity cannot be reduced below its current number of assigned occupants [PDF: 0.1.7]
            if (request.MaxCapacity < currentOccupancy)
                return ServiceResult<RoomResponseDto>.Failure($"Capacity reduction rejected. Room currently holds {currentOccupancy} active occupants.", 400);

            if (await _repository.RoomNumberExistsAsync(room.HostelId, cleanRoomNumber, id))
                return ServiceResult<RoomResponseDto>.Failure($"Room number '{cleanRoomNumber}' already exists in this hostel building.", 409);

            room.RoomNumber = cleanRoomNumber;
            room.MaxCapacity = request.MaxCapacity;
            room.IsActive = request.IsActive;

            await _repository.UpdateRoomAsync(room);
            await _repository.SaveChangesAsync();

            var res = new RoomResponseDto { Id = room.Id, HostelId = room.HostelId, RoomNumber = room.RoomNumber, MaxCapacity = room.MaxCapacity, IsActive = room.IsActive };
            return ServiceResult<RoomResponseDto>.Success(res, 200);
        }

        public async Task<ServiceResult<object>> DeleteRoomAsync(int id)
        {
            var room = await _repository.GetRoomByIdAsync(id);
            if (room == null) return ServiceResult<object>.Failure("Room record not found.", 404);

            int currentOccupancy = await _repository.GetRoomCurrentOccupancyAsync(id);
            if (currentOccupancy > 0)
                return ServiceResult<object>.Failure("Deletion blocked. Room currently houses active occupants.", 400);

            room.IsActive = false;
            await _repository.UpdateRoomAsync(room);
            await _repository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Room unit deactivated successfully." }, 200);
        }

        public async Task<ServiceResult<RoomOccupancyResponseDto>> GetRoomOccupancyAsync(int id)
        {
            var room = await _repository.GetRoomByIdAsync(id);
            if (room == null) return ServiceResult<RoomOccupancyResponseDto>.Failure("Room record not found.", 404);

            int currentOccupancy = await _repository.GetRoomCurrentOccupancyAsync(id);
            var res = new RoomOccupancyResponseDto { RoomId = room.Id, RoomNumber = room.RoomNumber, CurrentOccupancy = currentOccupancy, MaxCapacity = room.MaxCapacity };
            return ServiceResult<RoomOccupancyResponseDto>.Success(res, 200);
        }
    }
}
