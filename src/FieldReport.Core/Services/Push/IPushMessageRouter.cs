using FieldReport.Core.Navigation;

namespace FieldReport.Core.Services.Push;

public interface IPushMessageRouter
{
    DeepLink? ToDeepLink(IReadOnlyDictionary<string, string> data);
}
