using FieldReport.Core.Data;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Models;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.Services.Sync;
using FieldReport.Core.ValueObjects;
using FieldReport.Tests.TestData;

namespace FieldReport.Tests.Services.Sync;

[TestFixture]
[Ignore("Ćwiczenie 12: usuń Ignore po uzupełnieniu TODO")]
public class SyncServiceTests
{
    private InMemoryReportRepository _local = null!;
    private Mock<IReportsApiClient> _api = null!;
    private Mock<IConnectivityService> _connectivity = null!;
    private Mock<IPhotoStorage> _photos = null!;
    private SyncService _sync = null!;

    private static readonly Guid ServerId = Guid.Parse("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f"); // Id nadane przez serwer

    [SetUp]
    public void SetUp()
    {
        _local = new InMemoryReportRepository();
        _api = new Mock<IReportsApiClient>();
        _api.Setup(a => a.CreateAsync(It.IsAny<ReportDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportDto dto, Guid _, CancellationToken _) => Result<ReportDto>.Success(dto with { Id = ServerId }));
        _api.Setup(a => a.UpdateAsync(It.IsAny<ReportDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _api.Setup(a => a.UploadPhotoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _connectivity = new Mock<IConnectivityService>();
        _connectivity.SetupGet(c => c.IsOnline).Returns(true);
        _photos = new Mock<IPhotoStorage>();
        _sync = new SyncService(_local, _api.Object, /* TODO: przekaż _connectivity.Object */ _photos.Object);
    }

    private static Report NewReport(string title = "Awaria pompy", string? photo = null) =>
        Report.CreateNew(Guid.NewGuid(), ReportTitle.Create(title).Value!, "Opis", ReportPriority.High, TestReports.Now, photo);

    [Test]
    public async Task Offline_ReturnsNetwork_AndReportStaysOnPhone()
    {
        var pending = NewReport();
        await _local.SaveAsync(pending);
        _connectivity.SetupGet(c => c.IsOnline).Returns(false);

        var result = await _sync.SyncAsync();

        Assert.That(result.Error, Is.EqualTo(Errors.Network));
        _api.VerifyNoOtherCalls();                              // offline: zero wywołań API
        Assert.That(await _local.GetAsync(pending.Id), Is.Not.Null); // nadal w telefonie
    }

    [Test]
    public async Task NewReport_IsSentWithPost_AndItsIdempotencyKey_ThenRemovedFromPhone()
    {
        var pending = NewReport();
        var localId = pending.Id;
        await _local.SaveAsync(pending);

        var result = await _sync.SyncAsync();

        Assert.That(result.IsSuccess, Is.True);
        _api.Verify(a => a.CreateAsync(It.Is<ReportDto>(d => d.Title == "Awaria pompy"), pending.IdempotencyKey!.Value,
            It.IsAny<CancellationToken>()), Times.Once);
        _api.Verify(a => a.UpdateAsync(It.IsAny<ReportDto>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(await _local.GetAllAsync(), Is.Empty);
        Assert.That(await _local.GetAsync(localId), Is.Null);
    }

    [Test]
    public async Task PostReturnedOtherStatus_StatusIsSentWithPut() // odpowiedź zginęła, technik zmienił status, POST = powtórka
    {
        var pending = NewReport();
        pending.ChangeStatus(ReportStatus.InProgress, TestReports.Now.AddMinutes(5));
        await _local.SaveAsync(pending);
        _api.Setup(a => a.CreateAsync(It.IsAny<ReportDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportDto dto, Guid _, CancellationToken _) =>
                Result<ReportDto>.Success(dto with { Id = ServerId, Status = ReportStatus.New })); // zapisany stan z serwera

        var result = await _sync.SyncAsync();

        Assert.That(result.IsSuccess, Is.True);
        _api.Verify(a => a.UpdateAsync(It.Is<ReportDto>(d => d.Id == ServerId && d.Status == ReportStatus.InProgress),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(await _local.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task StatusChangeOfServerReport_IsSentWithPut_WithoutPost()
    {
        var fromServer = TestReports.Create(status: ReportStatus.New, syncState: SyncState.Synced);
        fromServer.ChangeStatus(ReportStatus.InProgress, TestReports.Now.AddMinutes(5));
        await _local.SaveAsync(fromServer);

        await _sync.SyncAsync();

        _api.Verify(a => a.UpdateAsync(It.Is<ReportDto>(d => d.Id == fromServer.Id && d.Status == ReportStatus.InProgress),
            It.IsAny<CancellationToken>()), Times.Once);
        _api.Verify(a => a.CreateAsync(It.IsAny<ReportDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(await _local.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task Photo_IsUploadedAfterPost_ToIdFromServer_ThenFileIsDeleted()
    {
        var withPhoto = NewReport(photo: "/data/photos/a.jpg");
        await _local.SaveAsync(withPhoto);

        await _sync.SyncAsync();

        _api.Verify(a => a.UploadPhotoAsync(ServerId, "/data/photos/a.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _photos.Verify(p => p.DeleteAsync("/data/photos/a.jpg", It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(await _local.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task PhotoUploadFails_ReportStaysOnPhone_WithServerId_AndNextSyncUsesPutNotSecondPost()
    {
        var withPhoto = NewReport(photo: "/data/photos/a.jpg");
        await _local.SaveAsync(withPhoto);
        _api.SetupSequence(a => a.UploadPhotoAsync(ServerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Errors.Network))
            .ReturnsAsync(Result.Success());

        var first = await _sync.SyncAsync();
        var stored = await _local.GetAsync(ServerId);
        var second = await _sync.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first.Error, Is.EqualTo(Errors.Network));
            Assert.That(stored!.IsOnServer, Is.True); // dane już są na serwerze — nie wolno ich usunąć w telefonie
            Assert.That(stored.CanDelete, Is.False);
            Assert.That(second.IsSuccess, Is.True);
        });
        _api.Verify(a => a.CreateAsync(It.IsAny<ReportDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _api.Verify(a => a.UpdateAsync(It.Is<ReportDto>(d => d.Id == ServerId), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(await _local.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task SendFails_StopsAndKeepsReportOnPhone()
    {
        var pending = NewReport();
        await _local.SaveAsync(pending);
        _api.Setup(a => a.CreateAsync(It.IsAny<ReportDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportDto>.Failure(Errors.Server));

        var result = await _sync.SyncAsync();

        Assert.That(result.Error, Is.EqualTo(Errors.Server));
        Assert.That(await _local.GetAsync(pending.Id), Is.Not.Null);
    }

    [Test]
    public async Task RetriedSync_AfterLostResponse_SendsSameIdempotencyKeyAgain() // serwer nie zrobi duplikatu
    {
        var pending = NewReport();
        await _local.SaveAsync(pending);
        var keys = new List<Guid>();
        _api.Setup(a => a.CreateAsync(It.IsAny<ReportDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((ReportDto _, Guid key, CancellationToken _) => keys.Add(key))
            .ReturnsAsync(Result<ReportDto>.Failure(Errors.Network)); // odpowiedź zgubiona

        await _sync.SyncAsync();
        await _sync.SyncAsync();

        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Is.All.EqualTo(pending.IdempotencyKey));
    }
}

[TestFixture]
public class SyncSchedulerTests
{
    [Test]
    public async Task RequestSync_RunsSyncInBackground()
    {
        var started = new TaskCompletionSource();
        var sync = new Mock<ISyncService>();
        sync.Setup(s => s.SyncAsync(It.IsAny<CancellationToken>()))
            .Callback(() => started.TrySetResult()).ReturnsAsync(Result.Success());

        new SyncScheduler(() => sync.Object, Mock.Of<IErrorHandler>()).RequestSync();

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        sync.Verify(s => s.SyncAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Exception_GoesToErrorHandler()
    {
        var sync = new Mock<ISyncService>();
        sync.Setup(s => s.SyncAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        var handled = new TaskCompletionSource();
        var errors = new Mock<IErrorHandler>();
        errors.Setup(e => e.Handle(It.IsAny<Exception>())).Callback(() => handled.TrySetResult());

        new SyncScheduler(() => sync.Object, errors.Object).RequestSync();

        await handled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        errors.Verify(e => e.Handle(It.IsAny<InvalidOperationException>()), Times.Once);
    }
}
