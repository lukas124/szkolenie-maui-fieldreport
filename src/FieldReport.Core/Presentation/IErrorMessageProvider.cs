using FieldReport.Core.Models;

namespace FieldReport.Core.Presentation;

public interface IErrorMessageProvider
{
    string GetMessage(Error error);
}
