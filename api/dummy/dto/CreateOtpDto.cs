public class CreateRedpayDto
{
    public string Company { get; set; }
    public string Category { get; set; }
    public string Website { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Currency { get; set; }
    public string? Delegate { get; set; }
    public string? Diskon { get; set; }
    public List<CreateMemberOrder>? MemberOrder { get; set; }
}

public class ApprovedRedpayDto
{
    public string? merchant_transaction_id { get; set; }
    public string? status { get; set; }
}

public class PreviewRedpayDto
{
    public double? Amount { get; set; }
    public double? Participant { get; set; }
    public string? Delegate { get; set; }
    public string? Diskon { get; set; }


}

public class CreateMemberOrder
{
    public string? IdUser { get; set; }
    public string? FullName { get; set; }
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class GetRedpayDto
{
    public string? IdOrder { get; set; }

}

public class TransactionData
{
    public string message { get; set; }
    public string payment_url { get; set; }
    public string va { get; set; }
    public string retcode { get; set; }
    public bool success { get; set; }
    public string transaction_id { get; set; }
}

public class TransactionResponse
{
    public TransactionData data { get; set; }
}