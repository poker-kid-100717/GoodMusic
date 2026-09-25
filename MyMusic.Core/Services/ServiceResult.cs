namespace MyMusic.Core.Services;

/// <summary>Outcome of a write that can fail for a business reason.</summary>
public enum ServiceStatus
{
    Success,
    NotFound,
    Conflict,
    Invalid
}

public record ServiceResult<T>(ServiceStatus Status, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Success, value);
    public static ServiceResult<T> NotFound(string? error = null) => new(ServiceStatus.NotFound, default, error);
    public static ServiceResult<T> Conflict(string error) => new(ServiceStatus.Conflict, default, error);
    public static ServiceResult<T> Invalid(string error) => new(ServiceStatus.Invalid, default, error);
}
