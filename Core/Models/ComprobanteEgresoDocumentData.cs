using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    public class ComprobanteEgresoDocumentData
    {
        public ChequeData? Cheque { get; set; }
        public EgresoData? Egreso { get; set; }
    }

    public class DateInfo
    {
        public string? Day { get; set; }
        public string? Month { get; set; }
        public string? Year { get; set; }
    }

    public class ChequeData
    {
        public string? Number { get; set; }
        public string? Date { get; set; }
        public DateInfo? DateInfo { get; set; }
        public string? PayTo { get; set; }
        public string? AmountText { get; set; }
        public string? Amount { get; set; }
        public string? City { get; set; }
    }

    public class EgresoData
    {
        public string? Number { get; set; }
        public string? Date { get; set; }
        public DateInfo? DateInfo { get; set; }
        public string? BankCode { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverId { get; set; }
        public string? Concept { get; set; }
        public string? Description { get; set; }
        public List<AccountingItem>? Items { get; set; }
        public string? TotalDebit { get; set; }
        public string? TotalCredit { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class AccountingItem
    {
        public string? Account { get; set; }
        public string? CO { get; set; }
        public string? ThirdParty { get; set; }
        public string? Reference { get; set; }
        public string? Debit { get; set; }
        public string? Credit { get; set; }
    }
}
