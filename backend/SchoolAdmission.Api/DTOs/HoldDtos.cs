namespace SchoolAdmission.Api.DTOs;

// API 4 response
public class HoldCreatedDto
{
    public string HoldId { get; set; } = default!;
    public string ClassId { get; set; } = default!;
    public string StudentId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

// API 5 response
public class HoldDetailsDto
{
    public string HoldId { get; set; } = default!;
    public string ClassId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

// API 6 response
public class HoldConfirmedDto
{
    public string HoldId { get; set; } = default!;
    public string Status { get; set; } = default!;
}

// API 8 response items
public class MyHoldDto
{
    public string HoldId { get; set; } = default!;
    public string ClassId { get; set; } = default!;
    public string ClassName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}