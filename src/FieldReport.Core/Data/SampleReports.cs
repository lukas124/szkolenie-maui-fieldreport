using FieldReport.Core.Models;
using FieldReport.Core.ValueObjects;

namespace FieldReport.Core.Data;

/// <summary>Przykładowe zgłoszenia serwisowe (dane szkoleniowe, dopóki nie ma SQLite i API).</summary>
public static class SampleReports
{
    public static IReadOnlyList<Report> Create(DateTimeOffset now) =>
    [
        Sample("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f", "Awaria pompy obiegowej",
            "Pompa obiegowa w kotłowni budynku B nie uruchamia się, słychać buczenie silnika. Ciśnienie w instalacji spada.",
            ReportPriority.High, ReportStatus.New, SyncState.Pending, now.AddMinutes(-35), now.AddMinutes(-35)),
        Sample("8a41d7c2-5b3e-4f09-a6d1-7e2c9b0f3a14", "Wyciek z instalacji chłodniczej",
            "Pod agregatem na dachu hali magazynowej widoczna plama oleju. Możliwy wyciek czynnika.",
            ReportPriority.High, ReportStatus.InProgress, SyncState.Synced, now.AddHours(-5), now.AddHours(-2)),
        Sample("c7e90b15-2d64-4a8f-9f3b-51d0e8a2c6b7", "Uszkodzony czujnik temperatury",
            "Czujnik w strefie mroźni pokazuje -2°C przy nastawie -18°C. Do weryfikacji i wymiany.",
            ReportPriority.Normal, ReportStatus.InProgress, SyncState.Pending, now.AddDays(-1).AddHours(-3), now.AddHours(-4)),
        Sample("1b5f3e8d-9c27-4d60-b8a4-3e7f2a9c0d51", "Przegląd okresowy klimatyzacji",
            "Coroczny przegląd trzech jednostek split w biurze na 2. piętrze: filtry, odgrzybianie, szczelność.",
            ReportPriority.Normal, ReportStatus.New, SyncState.Synced, now.AddDays(-1), now.AddDays(-1)),
        Sample("e2d4c6a8-0b1f-4e3d-8c5a-7f9e1d3b5a70", "Hałas łożyska wentylatora dachowego",
            "Metaliczny dźwięk przy pracy wentylatora wyciągowego W-3, nasila się przy wyższych obrotach.",
            ReportPriority.Low, ReportStatus.New, SyncState.Pending, now.AddDays(-2), now.AddDays(-2)),
        Sample("5d9a2f71-6e4b-4c83-a0d2-9b8e7c6f5a43", "Wymiana filtra w centrali wentylacyjnej",
            "Wymieniono filtry kieszeniowe w centrali CW-1. Zapisano odczyt spadku ciśnienia.",
            ReportPriority.Low, ReportStatus.Done, SyncState.Synced, now.AddDays(-4), now.AddDays(-3)),
    ];

    private static Report Sample(string id, string title, string description, ReportPriority priority,
        ReportStatus status, SyncState syncState, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(Guid.Parse(id), ReportTitle.Create(title).Value!, description, priority, status, syncState,
            photoPath: null, createdAt, updatedAt);
}
