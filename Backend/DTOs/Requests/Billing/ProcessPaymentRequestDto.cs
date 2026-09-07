namespace CampusServicesPortal.DTOs.Requests.Billing
{
    public class ProcessPaymentRequestDto
    {
        public string? PaymentChannel { get; set; } = "card";
        public string? OtpCode { get; set; }
        public string? ReferenceNo { get; set; }
        public string? CardholderName { get; set; }
        public string? BankPortal { get; set; }
        public string? FileName { get; set; }
    }
}
