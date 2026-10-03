using FieldReport.Core.Services;

namespace FieldReport.App.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
