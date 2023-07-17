using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public interface ITestModel : INotifyPropertyChanged
{
    int ChangingCount { get; }

    int IntValue { get; set; }
    string StringValue { get; set; }

    bool IsA { get; set; }
    bool IsB { get; set; }
    bool IsC { get; set; }

    ObservableCollection<int> IntCollection { get; set; }

    ObservableCollection<CollectionItem> Collection { get; set; }
}