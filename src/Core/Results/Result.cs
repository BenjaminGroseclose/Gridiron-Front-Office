namespace GridironFrontOffice.Core.Results;

/// <summary>
/// An expected business failure: over the cap, roster full, a pick already made. Commands return these through
/// <see cref="Result{T}"/>; exceptions are for bugs and I/O (DATA_ACCESS_GUIDE.md §3.2).
/// </summary>
/// <param name="Code">A stable identifier, e.g. "OverCap". Tests and the UI branch on it.</param>
/// <param name="Message">A sentence the player can read.</param>
public sealed record DomainError(string Code, string Message)
{
    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>The "no value" value, for commands that succeed without returning anything.</summary>
public readonly record struct Unit
{
    public static Unit Value => default;
}

/// <summary>Either a value or a <see cref="DomainError"/>. Build one with <see cref="Result.Ok{T}"/> or <see cref="Result.Fail{T}"/>.</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, DomainError? error) => (_value, Error) = (value, error);

    public DomainError? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => Error is not null;

    /// <summary>The value of a successful result. Reading it from a failed result is a bug, so it throws.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"The result failed ({Error}), so it has no value.");

    internal static Result<T> Success(T value) => new(value, null);

    internal static Result<T> Failure(DomainError error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));

    public override string ToString() => IsSuccess ? $"Ok({_value})" : $"Fail({Error})";
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => Result<T>.Success(value);

    public static Result<Unit> Ok() => Result<Unit>.Success(Unit.Value);

    public static Result<T> Fail<T>(DomainError error) => Result<T>.Failure(error);
}
