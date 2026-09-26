using FieldReport.Core.ViewModels.NewReport;

namespace FieldReport.Tests.ViewModels.NewReport;

[TestFixture]
public class StepSequenceTests
{
    [Test]
    public void StartsOnFirstStep()
    {
        var steps = new StepSequence(3);
        Assert.Multiple(() =>
        {
            Assert.That(steps.Index, Is.Zero);
            Assert.That(steps.IsFirst, Is.True);
            Assert.That(steps.IsLast, Is.False);
            Assert.That(steps.Progress, Is.EqualTo(1d / 3).Within(1e-9));
        });
    }

    [Test]
    public void MoveNext_StopsOnLastStep()
    {
        var steps = new StepSequence(3);
        Assert.That(steps.MoveNext(), Is.True);
        Assert.That(steps.MoveNext(), Is.True);
        Assert.That(steps.MoveNext(), Is.False);
        Assert.That(steps.Index, Is.EqualTo(2));
        Assert.That(steps.IsLast, Is.True);
        Assert.That(steps.Progress, Is.EqualTo(1d));
    }

    [Test]
    public void MovePrevious_OnFirstStep_ReturnsFalse()
    {
        var steps = new StepSequence(3);
        Assert.That(steps.MovePrevious(), Is.False);
        steps.MoveNext();
        Assert.That(steps.MovePrevious(), Is.True);
        Assert.That(steps.IsFirst, Is.True);
    }

    [Test]
    public void SingleStep_IsFirstAndLast()
    {
        var steps = new StepSequence(1);
        Assert.That(steps.IsFirst && steps.IsLast, Is.True);
        Assert.That(steps.MoveNext(), Is.False);
    }
}
