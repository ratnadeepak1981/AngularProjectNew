using System.Collections.Generic;
using CampusServicesPortal.DTOs.Requests.Student;

namespace CampusServicesPortal.DTOs.Responses.Student
{
    public class StudentProfileResponseDto
    {
        public int Id { get; set; }
        public string IndexNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int? FacultyId { get; set; }
        public string FacultyName { get; set; } = null!;
        public string? ContactDetails { get; set; }
        public bool EmailVerified { get; set; }
        public bool PhoneVerified { get; set; }
        public bool IsActive { get; set; }

        public List<StudentPhoneNumberDto> PhoneNumbers { get; set; } = new();
        public List<StudentAddressDto> Addresses { get; set; } = new();
    }
}
