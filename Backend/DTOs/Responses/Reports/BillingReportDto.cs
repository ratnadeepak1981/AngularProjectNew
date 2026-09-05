using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class FeeTypeSummaryItemDto
    {
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public int TotalInvoices { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public double CollectionRate { get; set; }
    }

    public class PaymentDetailItemDto
    {
        public int InvoiceId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }

    public class BillingReportDto
    {
        public List<FeeTypeSummaryItemDto> FeeTypeSummaries { get; set; } = new List<FeeTypeSummaryItemDto>();
        public decimal GrandTotalBilled { get; set; }
        public decimal GrandTotalPaid { get; set; }
        public decimal GrandTotalOutstanding { get; set; }
        public int GrandTotalInvoices { get; set; }
        public double OverallCollectionPercentage { get; set; }
    }
}
