namespace SchoolAdmission.Api.Models;

public class SchoolClass
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; }
}