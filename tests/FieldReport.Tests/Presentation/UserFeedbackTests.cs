using FieldReport.Core.Models;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;

namespace FieldReport.Tests.Presentation;

[TestFixture]
public class UserFeedbackTests
{
    private Mock<IDialogService> _dialogs = null!;
    private UserFeedback _feedback = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogs = new Mock<IDialogService>();
        var messages = Mock.Of<IErrorMessageProvider>(m => m.GetMessage(Errors.Network) == "Brak sieci");
        var localizer = new Mock<ILocalizer>();
        localizer.Setup(l => l.GetString(It.IsAny<string>())).Returns((string key) => $"<{key}>");
        _feedback = new UserFeedback(_dialogs.Object, messages, localizer.Object);
    }

    [Test]
    public async Task ShowError_ShowsAlertWithMessageFromProvider()
    {
        await _feedback.ShowErrorAsync(Errors.Network);

        _dialogs.Verify(d => d.AlertAsync("<Common_ErrorTitle>", "Brak sieci", "<Common_Ok>"), Times.Once);
    }

    [Test]
    public async Task Confirm_TranslatesKeys_AndReturnsUserAnswer()
    {
        _dialogs.Setup(d => d.ConfirmAsync("<T>", "<M>", "<Common_Yes>", "<Common_No>")).ReturnsAsync(true);

        var answer = await _feedback.ConfirmAsync("T", "M");

        Assert.That(answer, Is.True);
    }
}

[TestFixture]
public class UserFeedbackCustomButtonsTests
{
    [Test]
    public async Task Confirm_WithButtonKeys_UsesTextsFromResources()
    {
        var dialogs = new Mock<FieldReport.Core.Services.IDialogService>();
        var feedback = new FieldReport.Core.Presentation.UserFeedback(dialogs.Object,
            Mock.Of<FieldReport.Core.Presentation.IErrorMessageProvider>(), new FieldReport.Tests.TestData.KeyLocalizer());

        await feedback.ConfirmAsync("T", "M", "Permission_OpenSettings", "Common_Cancel");

        dialogs.Verify(d => d.ConfirmAsync("T", "M", "Permission_OpenSettings", "Common_Cancel"), Times.Once);
    }
}
