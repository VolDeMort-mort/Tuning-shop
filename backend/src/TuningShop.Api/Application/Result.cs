using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>Результат прикладної операції: значення або предметна відмова, без HTTP-статусу.</summary>
public sealed class Result<T>
{
    private Result(T? value, DomainError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public DomainError? Error { get; }
    public bool IsSuccess => Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(DomainError error) => new(default, error);
}
