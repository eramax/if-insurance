namespace InsuranceManagement.Models;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool isSuccess, T? value, string? errorMessage, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string errorMessage, string? errorCode = null) =>
        new(false, default, errorMessage, errorCode);

    public static Result<T> Conflict(string errorMessage) =>
        new(false, default, errorMessage, "CONFLICT");

    public static Result<T> ValidationError(string errorMessage) =>
        new(false, default, errorMessage, "VALIDATION_ERROR");

    public static Result<T> NotFound(string errorMessage) =>
        new(false, default, errorMessage, "NOT_FOUND");
}

/// <summary>
/// Non-generic result for operations that don't return a value
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool isSuccess, string? errorMessage, string? errorCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string errorMessage, string? errorCode = null) =>
        new(false, errorMessage, errorCode);

    public static Result Conflict(string errorMessage) =>
        new(false, errorMessage, "CONFLICT");

    public static Result ValidationError(string errorMessage) =>
        new(false, errorMessage, "VALIDATION_ERROR");

    public static Result NotFound(string errorMessage) =>
        new(false, errorMessage, "NOT_FOUND");
}
