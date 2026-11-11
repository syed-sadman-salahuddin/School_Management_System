using Microsoft.AspNetCore.Mvc.Testing;

namespace SchoolAdmission.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    static ApiFactory()
    {
        var conn = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=admission_test;Username=postgres;Password=postgres;Maximum Pool Size=30;Timeout=60";

        Environment.SetEnvironmentVariable("ConnectionStrings__Default", conn);
        Environment.SetEnvironmentVariable("HOLD_DURATION_SECONDS", "600");
    }
}