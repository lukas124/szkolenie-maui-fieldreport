namespace FieldReport.Core.Models;

/// <summary>Przewidywalny błąd. Tekst dla użytkownika powstaje z kodu (zasób <c>Error_{Code}</c>).</summary>
public sealed record Error(string Code);
