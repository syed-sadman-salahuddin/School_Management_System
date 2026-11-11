using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SchoolAdmission.Tests;

public class AdmissionTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AdmissionTests(ApiFactory factory) => _client = factory.CreateClient();

    private record ClassCreated(string Id, string Name, int TotalSeats);
    private record ClassStats(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
    private record HoldCreated(string HoldId, string ClassId, string StudentId, string Status, DateTime ExpiresAt);
    private record HoldConfirmed(string HoldId, string Status);

    // ---- helpers ----
    private async Task<ClassCreated> CreateClassAsync(int seats)
    {
        var name = "Test Class " + Guid.NewGuid().ToString("N")[..8]; // unique every run
        var res = await _client.PostAsJsonAsync("/classes", new { name, totalSeats = seats });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<ClassCreated>())!;
    }

    private Task<HttpResponseMessage> ApplyAsync(string classId, string studentId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        req.Headers.Add("X-Student-Id", studentId);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> ConfirmAsync(string holdId, string studentId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdId}/confirm");
        req.Headers.Add("X-Student-Id", studentId);
        return _client.SendAsync(req);
    }

    private async Task<ClassStats> GetStatsAsync(string classId)
        => (await _client.GetFromJsonAsync<ClassStats>($"/classes/{classId}"))!;

    private static string StudentId(int i) => $"2015{i:D10}"; 


    [Fact]
    public async Task Student_can_apply_and_a_seat_is_held()
    {
        var cls = await CreateClassAsync(5);

        var res = await ApplyAsync(cls.Id, StudentId(1));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var hold = (await res.Content.ReadFromJsonAsync<HoldCreated>())!;
        Assert.Equal("HELD", hold.Status);
        Assert.Equal(cls.Id, hold.ClassId);
        Assert.Equal(StudentId(1), hold.StudentId);
        Assert.True(hold.ExpiresAt > DateTime.UtcNow);

        var stats = await GetStatsAsync(cls.Id);
        Assert.Equal(1, stats.HeldSeats);
        Assert.Equal(0, stats.ConfirmedSeats);
        Assert.Equal(4, stats.AvailableSeats);
    }


    [Fact]
    public async Task Confirming_multiple_times_counts_only_one_confirmed_seat()
    {
        var cls = await CreateClassAsync(5);
        var sid = StudentId(2);

        var hold = (await (await ApplyAsync(cls.Id, sid)).Content.ReadFromJsonAsync<HoldCreated>())!;

        for (var i = 0; i < 3; i++)
        {
            var res = await ConfirmAsync(hold.HoldId, sid);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = (await res.Content.ReadFromJsonAsync<HoldConfirmed>())!;
            Assert.Equal("CONFIRMED", body.Status);
        }

        var stats = await GetStatsAsync(cls.Id);
        Assert.Equal(1, stats.ConfirmedSeats);
        Assert.Equal(0, stats.HeldSeats);
        Assert.Equal(4, stats.AvailableSeats);

        var other = await ConfirmAsync(hold.HoldId, StudentId(3));
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Concurrent_requests_never_exceed_capacity()
    {
        const int seats = 10;
        const int requests = 100;
        var cls = await CreateClassAsync(seats);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(1, requests).Select(async i =>
        {
            await gate.Task;
            var res = await ApplyAsync(cls.Id, StudentId(1000 + i)); 
            return res.StatusCode;
        }).ToList();

        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        var created = results.Count(s => s == HttpStatusCode.Created);
        var conflicts = results.Count(s => s == HttpStatusCode.Conflict);

        Assert.Equal(seats, created);                    
        Assert.Equal(requests - seats, conflicts);       
        Assert.Equal(requests, created + conflicts);     

        var stats = await GetStatsAsync(cls.Id);
        Assert.True(stats.HeldSeats + stats.ConfirmedSeats <= stats.TotalSeats);
        Assert.Equal(seats, stats.HeldSeats);
        Assert.Equal(0, stats.AvailableSeats);
    }
}