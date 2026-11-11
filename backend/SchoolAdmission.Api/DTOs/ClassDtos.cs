namespace SchoolAdmission.Api.DTOs;

public class CreateClassRequest
{
    public string? Name { get; set; }
    public int TotalSeats { get; set; }
}

public class ClassDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int TotalSeats { get; set; }
}

public class ClassStatsDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int TotalSeats { get; set; }
    public int HeldSeats { get; set; }
    public int ConfirmedSeats { get; set; }

    // availableSeats = totalSeats - heldSeats - confirmedSeats
    public int AvailableSeats => Math.Max(0, TotalSeats - HeldSeats - ConfirmedSeats);
}