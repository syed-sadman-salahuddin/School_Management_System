namespace SchoolAdmission.Api.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    protected AppException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class BadRequestException : AppException
{
    public BadRequestException(string code, string message) : base(400, code, message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string code, string message) : base(403, code, message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string code, string message) : base(404, code, message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(409, code, message) { }
}