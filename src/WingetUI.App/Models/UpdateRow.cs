using CommunityToolkit.Mvvm.ComponentModel;
using WingetUI.Core.Packages;

namespace WingetUI.App.Models;

public partial class UpdateRow(Package package) : ObservableObject
{
    public Package Package { get; } = package;
    [ObservableProperty] private bool isChecked;
    [ObservableProperty] private ActivityEntry? operation;
}
