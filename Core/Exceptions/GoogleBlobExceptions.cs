namespace datopus.Application.Exceptions;

public class BlobNotFoundException : Exception
{
    public BlobNotFoundException(string message)
        : base(message) { }

    public BlobNotFoundException(string message, Exception inner)
        : base(message, inner) { }
}

public class BlobAccessException : Exception
{
    public BlobAccessException(string message, Exception inner)
        : base(message, inner) { }
}

public class BlobDataFormatException : Exception
{
    public BlobDataFormatException(string message)
        : base(message) { }

    public BlobDataFormatException(string message, Exception inner)
        : base(message, inner) { }
}
