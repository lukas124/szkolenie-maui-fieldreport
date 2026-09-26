using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Tests.TestData;

/// <summary>Budowanie zgłoszeń w testach (czytelne wartości domyślne).</summary>
internal static class TestReports
{
    public static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.FromHours(2));

    public static Report Create(
        string title = "Awaria pompy obiegowej",
        ReportStatus status = ReportStatus.New,
        SyncState syncState = SyncState.Synced,
        ReportPriority priority = ReportPriority.Normal,
        DateTimeOffset? updatedAt = null,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), ReportTitle.Create(title).Value!, "Opis", priority, status, syncState,
            photoPath: null, createdAt: Now, updatedAt: updatedAt ?? Now);
}
