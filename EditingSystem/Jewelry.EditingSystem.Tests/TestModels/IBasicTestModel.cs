using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public interface IBasicTestModel : INotifyPropertyChanged
{
    int ChangingCount { get; }

    int IntValue { get; set; }
    string StringValue { get; set; }

    ObservableCollection<int> IntCollection { get; set; }

    ObservableCollection<CollectionItem> Collection { get; set; }
}