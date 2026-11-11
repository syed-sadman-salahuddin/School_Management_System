using System.Text.RegularExpressions;
using SchoolAdmission.Api.Exceptions;

namespace SchoolAdmission.Api.Services;

public static class StudentIdValidator
{
    private static readonly Regex Pattern = new(@"^[A-Za-z0-9\-]{5,32}$", RegexOptions.Compiled);

    public static string Require(string? header)
    {
        var id = header?.Trim();

        if (string.IsNullOrEmpty(id))
            throw new BadRequestException("MISSING_STUDENT_ID", "The X-Student-Id header is required.");

        if (!Pattern.IsMatch(id))
            throw new BadRequestException("INVALID_STUDENT_ID",
                "Student ID must be 5-32 characters: letters, digits or hyphens only.");

        return id;
    }
}