using System.Diagnostics.CodeAnalysis;

namespace WebFlow.Extensions;

public static class ExceptionExtensions
{
    public static Result Try(Action action)
    {
        try
        {
            action();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    public static Result<T?> Try<T>(Func<T> action)
    {
        try
        {
            T result = action();
            return Result<T?>.Ok(result);
        }
        catch (Exception? ex)
        {
            return Result<T>.Fail(ex);
        }
    }
    
    public static async Task<Result> TryAsync(Func<Task> asyncAction)
    {
        try
        {
            await asyncAction();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }
    
    public static async Task<Result<T?>> TryAsync<T>(Func<Task<T>> asyncAction)
    {
        try
        {
            T result = await asyncAction();
            return Result<T?>.Ok(result);
        }
        catch (Exception? ex)
        {
            return Result<T?>.Fail(ex);
        }   
    }
}

public readonly struct Result<T>
{
    private readonly T _value; 
    public readonly object? Error;

    private Result(T value, object? error)
    {
        _value = value;
        Error = error;
    }

    public static Result<T> Ok(T value)
    {
        return new Result<T>(value, null);
    }

    public static Result<T?> Fail(Exception error)
    {
        return new Result<T?>(default, error);
    }

    public static Result<T?> Fail(string error)
    {
        return new Result<T?>(default, error);
    }

    public bool IsSuccess => Error is null;

    public T? Unwrap()
    {
        return Error is Exception 
            ? default 
            : _value;
    }
}

public readonly struct Result
{
    private Result(object? error = null)
    {
        Error = error;
    }

    public static Result Ok()
    {
        return new Result();
    }

    public static Result Fail(Exception error)
    {
        return new Result(error);
    }

    public static Result Fail(string error)
    {
        return new Result(error);
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public object? Error { get; }
}