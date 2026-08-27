using System.ComponentModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public interface IFlagTestModel : INotifyPropertyChanged
{
    int ChangingCount { get; }

    bool IsA { get; set; }
    bool IsB { get; set; }
    bool IsC { get; set; }
}