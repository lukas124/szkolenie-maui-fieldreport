using System.Collections.ObjectModel;

namespace FieldReport.Core.Presentation;

public static class ObservableCollectionExtensions
{
    /// <summary>Podmienia zawartość kolekcji (ta sama instancja, więc binding w widoku zostaje).</summary>
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}
