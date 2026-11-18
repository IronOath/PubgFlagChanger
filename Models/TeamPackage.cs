using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PUBGFlagChanger.Models;

public class TeamPackage : INotifyPropertyChanged
{
    public string Name { get; set; } = "";

    public string FolderPath { get; set; } = "";

    public int TeamCount { get; set; }

    public int IconCount { get; set; }

    private bool _isUsedPackage;

    public bool IsUsedPackage
    {
        get => _isUsedPackage;
        set
        {
            _isUsedPackage = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string CsvPath =>
        Path.Combine(FolderPath, "TeamInfo.csv");

    public string IconFolder =>
        Path.Combine(FolderPath, "TeamIcon");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}