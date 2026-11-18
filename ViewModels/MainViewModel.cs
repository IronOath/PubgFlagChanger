using Microsoft.Win32;
using PUBGFlagChanger.Models;
using PUBGFlagChanger.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic;

namespace PUBGFlagChanger.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly PackageService _packageService;
    private readonly BackupService _backupService;
    private readonly SettingsService _settingsService;

    public ObservableCollection<TeamPackage> Packages { get; }
        = new();

    private TeamPackage? _selectedPackage;

    public TeamPackage? SelectedPackage
    {
        get => _selectedPackage;
        set
        {
            _selectedPackage = value;
            OnPropertyChanged();
        }
    }

    private string _destinationFolder = "";

    public string DestinationFolder
    {
        get => _destinationFolder;
        set
        {
            _destinationFolder = value;
            OnPropertyChanged();
            SaveSettings();
            RefreshUsedPackage();
        }
    }

    private string _status = "Ready";

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    private string _usedPackageName = "None";

    public string UsedPackageName
    {
        get => _usedPackageName;
        set
        {
            _usedPackageName = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand ImportCommand { get; }
    public RelayCommand BrowseCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand LaunchGameCommand { get; }
    public RelayCommand KillGameCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand RemovePackageCommand { get; }
    public RelayCommand RenamePackageCommand { get; }

    public MainViewModel()
    {
        _packageService = new PackageService();
        _backupService = new BackupService();
        _settingsService = new SettingsService();

        var settings = _settingsService.Load();
        _destinationFolder = settings.DestinationFolder;

        ImportCommand =
            new RelayCommand(ImportPackage);

        BrowseCommand =
            new RelayCommand(BrowseFolder);

        ApplyCommand =
            new RelayCommand(ApplyPackage);

        LaunchGameCommand =
            new RelayCommand(LaunchGame);

        KillGameCommand =
            new RelayCommand(KillGame);

        AddFolderCommand =
            new RelayCommand(AddFolderPackage);

        RemovePackageCommand =
            new RelayCommand(RemoveSelectedPackage);

        RenamePackageCommand =
            new RelayCommand(RenameSelectedPackage);

        OnPropertyChanged(nameof(DestinationFolder));
        LoadPackages();
        RefreshUsedPackage();
    }

    private void LoadPackages()
    {
        string? selectedName = SelectedPackage?.Name;

        Packages.Clear();

        foreach (var pkg in _packageService.GetPackages())
        {
            pkg.PropertyChanged += PackageOnPropertyChanged;
            Packages.Add(pkg);
        }

        foreach (var pkg in Packages)
        {
            pkg.IsUsedPackage =
                !string.IsNullOrWhiteSpace(UsedPackageName)
                && UsedPackageName != "None"
                && string.Equals(pkg.Name, UsedPackageName, StringComparison.OrdinalIgnoreCase);
        }

        SelectedPackage =
            !string.IsNullOrWhiteSpace(selectedName)
            ? Packages.FirstOrDefault(p => p.Name == selectedName)
            : Packages.FirstOrDefault();

        if (SelectedPackage != null)
        {
            SelectedPackage.IsSelected = true;
        }

        if (Packages.Count == 0)
        {
            Status = $"No available packages found in {_packageService.PackageRoot}";
        }
    }

    private void PackageOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TeamPackage.IsSelected))
            return;

        if (sender is not TeamPackage changedPackage || !changedPackage.IsSelected)
            return;

        foreach (var pkg in Packages.Where(p => !ReferenceEquals(p, changedPackage) && p.IsSelected))
        {
            pkg.IsSelected = false;
        }

        SelectedPackage = changedPackage;
    }

    private void ImportPackage()
    {
        var dialog = new OpenFileDialog();

        dialog.Filter =
            "Zip Files (*.zip)|*.zip";

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _packageService.ImportZip(
                    dialog.FileName);

                LoadPackages();

                string importedName =
                    Path.GetFileNameWithoutExtension(dialog.FileName);

                SelectedPackage =
                    Packages.FirstOrDefault(p => p.Name == importedName)
                    ?? Packages.FirstOrDefault();

                Status = "Package imported";
            }
            catch (Exception ex)
            {
                Status = $"Import failed: {ex.Message}";
            }
        }
    }

    private void SaveSettings()
    {
        _settingsService.Save(
            new AppSettings
            {
                DestinationFolder = DestinationFolder
            });
    }

    private void LaunchGame()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c start steam://rungameid/578080",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Status = "Game launched";
    }

    private void KillGame()
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "taskkill",
            Arguments = "/f /im TslGame.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process == null)
        {
            Status = "Failed to start taskkill";
            return;
        }

        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            Status = "Game process terminated";
        }
        else
        {
            Status = "No running TslGame.exe process found";
        }
    }

    private void RefreshUsedPackage()
    {
        string? usedPackage =
            _packageService.GetUsedPackageName(DestinationFolder);

        UsedPackageName =
            string.IsNullOrWhiteSpace(usedPackage)
            ? "None"
            : usedPackage;

        foreach (var pkg in Packages)
        {
            pkg.IsUsedPackage =
                UsedPackageName != "None"
                && string.Equals(pkg.Name, UsedPackageName, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog();

        if (dialog.ShowDialog() == true)
        {
            DestinationFolder = dialog.FolderName;
            RefreshUsedPackage();
        }
    }

    private void ApplyPackage()
    {
        if (SelectedPackage == null)
            return;

        if (!File.Exists(SelectedPackage.CsvPath)
            || !Directory.Exists(SelectedPackage.IconFolder))
        {
            Status = "Selected package is missing TeamInfo.csv or TeamIcon.";
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinationFolder))
        {
            Status = "Select destination folder first";
            return;
        }

        Directory.CreateDirectory(DestinationFolder);

        _backupService
            .CreateBackup(DestinationFolder);

        File.Copy(
            SelectedPackage.CsvPath,
            Path.Combine(
                DestinationFolder,
                "TeamInfo.csv"),
            true);

        string destinationIconFolder =
            Path.Combine(DestinationFolder, "TeamIcon");

        if (Directory.Exists(destinationIconFolder))
        {
            Directory.Delete(destinationIconFolder, true);
        }

        CopyDirectory(
            SelectedPackage.IconFolder,
            destinationIconFolder);

        _packageService.SetUsedPackageName(
            DestinationFolder,
            SelectedPackage.Name);

        RefreshUsedPackage();

        Status = "Package applied";
    }

    private void AddFolderPackage()
    {
        var dialog = new OpenFolderDialog();

        if (dialog.ShowDialog() != true)
            return;

        string sourceFolder = dialog.FolderName;
        string packageName = Path.GetFileName(sourceFolder);

        try
        {
            _packageService.ImportFolder(sourceFolder, packageName);
            LoadPackages();
            RefreshUsedPackage();
            SelectedPackage = Packages.FirstOrDefault(p => p.Name == packageName);
            if (SelectedPackage != null)
            {
                SelectedPackage.IsSelected = true;
            }
            Status = "Folder package added";
        }
        catch (Exception ex)
        {
            Status = $"Add folder failed: {ex.Message}";
        }
    }

    private void RemoveSelectedPackage()
    {
        if (SelectedPackage == null)
            return;

        string packageName = SelectedPackage.Name;

        try
        {
            _packageService.RemovePackage(packageName);

            if (!string.IsNullOrWhiteSpace(DestinationFolder)
                && string.Equals(UsedPackageName, packageName, StringComparison.OrdinalIgnoreCase))
            {
                _packageService.SetUsedPackageName(DestinationFolder, string.Empty);
            }

            LoadPackages();
            RefreshUsedPackage();
            Status = "Package removed";
        }
        catch (Exception ex)
        {
            Status = $"Remove failed: {ex.Message}";
        }
    }

    private void RenameSelectedPackage()
    {
        if (SelectedPackage == null)
            return;

        string oldName = SelectedPackage.Name;

        string newName = Interaction.InputBox(
            "Enter new package folder name:",
            "Rename Package",
            oldName).Trim();

        if (string.IsNullOrWhiteSpace(newName)
            || string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            _packageService.RenamePackage(oldName, newName);

            if (!string.IsNullOrWhiteSpace(DestinationFolder)
                && string.Equals(UsedPackageName, oldName, StringComparison.OrdinalIgnoreCase))
            {
                _packageService.SetUsedPackageName(DestinationFolder, newName);
            }

            LoadPackages();
            RefreshUsedPackage();
            SelectedPackage = Packages.FirstOrDefault(p => p.Name == newName);
            if (SelectedPackage != null)
            {
                SelectedPackage.IsSelected = true;
            }
            Status = "Package renamed";
        }
        catch (Exception ex)
        {
            Status = $"Rename failed: {ex.Message}";
        }
    }

    private void CopyDirectory(
        string source,
        string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(
                file,
                Path.Combine(
                    destination,
                    Path.GetFileName(file)),
                true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(
                directory,
                Path.Combine(
                    destination,
                    Path.GetFileName(directory)));
        }
    }
}