namespace FieldReport.Core.Models;

/// <summary>Wynik operacji bez wartości. Przewidywalny błąd to <see cref="Result"/>, a nie wyjątek.</summary>
public sealed class Result
{
    private static readonly Result SuccessResult = new(null);

    private Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result Success() => SuccessResult;
    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
}

/// <summary>Wynik operacji zwracającej wartość.</summary>
public sealed class Result<T>
{
    private Result(T? value, Error? error) { Value = value; Error = error; }

    public T? Value { get; }                      // wartość (gdy sukces)
    public Error? Error { get; }                  // błąd (gdy porażka)
    public bool IsSuccess => Error is null;       // sukces = brak błędu

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));
}
