using System.Net.Http.Json;
using FieldReport.Core.Models;
using Polly;

namespace FieldReport.Core.Data.Api;

/// <summary>
/// Jedno miejsce na zamianę odpowiedzi/wyjątków HTTP na Result (SRP). Klienci API tylko budują żądania.
/// Wyjątki Polly z AddStandardResilienceHandler (TimeoutRejectedException, BrokenCircuitException — obie dziedziczą
/// z ExecutionRejectedException) to też brak sieci z punktu widzenia użytkownika.
/// </summary>
public static class HttpResultExtensions
{
    public static Task<Result<T>> SendForResultAsync<T>(this HttpClient http, HttpRequestMessage request, CancellationToken ct) =>
        CatchErrorsAsync(async () =>
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return Result<T>.Failure(HttpErrorMapper.Map(response.StatusCode));
            var body = await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct);
            return body is null ? Result<T>.Failure(Errors.InvalidResponse) : Result<T>.Success(body);
        }, Result<T>.Failure, ct);    // wyjątki → CatchErrorsAsync

    // wariant bez treści odpowiedzi
    public static Task<Result> SendForResultAsync(this HttpClient http, HttpRequestMessage request, CancellationToken ct) =>
        CatchErrorsAsync(async () =>
        {
            using var response = await http.SendAsync(request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(HttpErrorMapper.Map(response.StatusCode));
        }, Result.Failure, ct);

    // Wyjątki sieci i niepoprawna odpowiedź → błąd w Result (failure tworzy Result właściwego typu)
    private static async Task<TResult> CatchErrorsAsync<TResult>(Func<Task<TResult>> send, Func<Error, TResult> failure, CancellationToken ct)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException)                        // brak sieci, DNS
        {
            return failure(Errors.Network);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return failure(Errors.Network); // timeout
        }
        catch (ExecutionRejectedException)
        {
            // Polly: timeout próby/całości albo otwarty obwód
            return failure(Errors.Network);
        }
        catch (System.Text.Json.JsonException)              // zła treść odpowiedzi
        {
            return failure(Errors.InvalidResponse);
        }
    }
}
