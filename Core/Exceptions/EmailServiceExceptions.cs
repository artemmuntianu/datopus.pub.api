namespace datopus.Core.Exceptions;

public class EmailServiceException : BaseException
{
    public string[]? Errors { get; set; }

    public EmailServiceException(string message)
        : base(message) { }

    public EmailServiceException(string message, string[]? errors)
        : base(message)
    {
        Errors = errors;
    }
}

public class MailTrapErrorResponse
{
    public bool Success { get; set; }

    public string[]? errors { get; set; }
}
