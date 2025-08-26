namespace MemoApp.Core.Services.Packs;

/// <summary>
/// Exception thrown when there is insufficient storage space for a download
/// </summary>
public class InsufficientStorageException : Exception
{
    /// <summary>
    /// The number of additional bytes needed to complete the download
    /// </summary>
    public long RequiredBytes { get; }

    /// <summary>
    /// The available storage space in bytes
    /// </summary>
    public long AvailableBytes { get; }

    public InsufficientStorageException(long requiredBytes, long availableBytes) 
        : base($"Insufficient storage space. Required: {requiredBytes:N0} bytes, Available: {availableBytes:N0} bytes")
    {
        RequiredBytes = requiredBytes;
        AvailableBytes = availableBytes;
    }

    public InsufficientStorageException(long requiredBytes, long availableBytes, Exception innerException) 
        : base($"Insufficient storage space. Required: {requiredBytes:N0} bytes, Available: {availableBytes:N0} bytes", innerException)
    {
        RequiredBytes = requiredBytes;
        AvailableBytes = availableBytes;
    }
}

/// <summary>
/// Exception thrown when pack extraction fails
/// </summary>
public class PackExtractionException : Exception
{
    public PackExtractionException() : base("Failed to extract pack contents")
    {
    }

    public PackExtractionException(string message) : base(message)
    {
    }

    public PackExtractionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when pack validation fails
/// </summary>
public class PackValidationException : Exception
{
    public PackValidationException() : base("Pack validation failed")
    {
    }

    public PackValidationException(string message) : base(message)
    {
    }

    public PackValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}