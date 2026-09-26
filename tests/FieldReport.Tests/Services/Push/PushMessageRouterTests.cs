using FieldReport.Core.Navigation;
using FieldReport.Core.Services.Push;

namespace FieldReport.Tests.Services.Push;

[TestFixture]
public class PushMessageRouterTests
{
    private readonly PushMessageRouter _router = new();
    private static readonly Guid ReportId = Guid.Parse("3f2c9a4e-8d1b-4c7a-9e55-0a1b2c3d4e5f");

    [TestCase("report_assigned")]
    [TestCase("report_status_changed")]
    public void ValidPayload_ReturnsReportLink(string type)
    {
        var link = _router.ToDeepLink(new Dictionary<string, string> { ["type"] = type, ["reportId"] = ReportId.ToString() });
        Assert.That(link, Is.EqualTo(new DeepLink(ReportId)));
    }

    [Test]
    public void UnknownType_ReturnsNull() =>
        Assert.That(_router.ToDeepLink(new Dictionary<string, string> { ["type"] = "promo", ["reportId"] = ReportId.ToString() }), Is.Null);

    [TestCase("abc")]
    [TestCase("")]
    [TestCase("3f2c9a4e")]
    public void InvalidReportId_ReturnsNull(string id) =>
        Assert.That(_router.ToDeepLink(new Dictionary<string, string> { ["type"] = "report_assigned", ["reportId"] = id }), Is.Null);

    [Test]
    public void MissingKeys_ReturnNull()
    {
        Assert.That(_router.ToDeepLink(new Dictionary<string, string>()), Is.Null);
        Assert.That(_router.ToDeepLink(new Dictionary<string, string> { ["type"] = "report_assigned" }), Is.Null);
        Assert.That(_router.ToDeepLink(new Dictionary<string, string> { ["reportId"] = ReportId.ToString() }), Is.Null);
    }

    [Test]
    public void ExtraKeysFromPlatform_AreIgnored() // Android dokłada google.message_id itd., iOS — aps
    {
        var data = new Dictionary<string, string>
        {
            ["type"] = "report_assigned", ["reportId"] = ReportId.ToString(), ["google.message_id"] = "0:1", ["aps"] = "{}"
        };
        Assert.That(_router.ToDeepLink(data), Is.EqualTo(new DeepLink(ReportId)));
    }
}
