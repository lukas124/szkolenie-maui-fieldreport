using FieldReport.Core.Models;

namespace FieldReport.Tests.Models;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Success_HasValue_AndNoError()
    {
        var result = Result<int>.Success(42);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void Failure_CarriesError()
    {
        var result = Result.Failure(Errors.Network);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo(new Error("network"))); // Error to record: porównanie po wartości
    }
}
