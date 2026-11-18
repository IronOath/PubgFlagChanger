using PUBGFlagChanger.Models;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace PUBGFlagChanger.Services;

public class PackageService
{
    public string PackageRoot { get; }

    private const string CsvFileName = "TeamInfo.csv";
    private const string IconFolderName = "TeamIcon";
    private const string UsedPackagesConfigFileName = "used-packages.json";

    public PackageService()
    {
        PackageRoot =
            Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Packages");

        Directory.CreateDirectory(PackageRoot);
    }

    public void ImportZip(string zipFile)
    {
        string packageName =
            Path.GetFileNameWithoutExtension(zipFile);

        string tempFolder =
            Path.Combine(
                Path.GetTempPath(),
                "PUBGFlagChanger",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempFolder);

        try
        {
            ZipFile.ExtractToDirectory(
                zipFile,
                tempFolder);

            string packageFolder =
                FindPackageFolder(tempFolder);

            string destination =
                Path.Combine(PackageRoot, packageName);

            if (Directory.Exists(destination))
                Directory.Delete(destination, true);

            CopyDirectory(packageFolder, destination);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }

    public List<TeamPackage> GetPackages()
    {
        var result = new List<TeamPackage>();

        foreach (var dir in Directory.GetDirectories(PackageRoot))
        {
            string csv =
                Path.Combine(dir, CsvFileName);

            string iconFolder =
                Path.Combine(dir, IconFolderName);

            var pkg = new TeamPackage
            {
                Name = Path.GetFileName(dir),
                FolderPath = dir,
                TeamCount = File.Exists(csv)
                    ? CountTeams(csv)
                    : 0,
                IconCount = Directory.Exists(iconFolder)
                    ? Directory.GetFiles(
                    iconFolder,
                    "*",
                    SearchOption.AllDirectories).Length
                    : 0
            };

            result.Add(pkg);
        }

        return result;
    }

    public void ImportFolder(string sourceFolder, string packageName)
    {
        string destination =
            Path.Combine(PackageRoot, packageName);

        if (Directory.Exists(destination))
        {
            throw new InvalidOperationException(
                $"Package '{packageName}' already exists.");
        }

        CopyDirectory(sourceFolder, destination);
    }

    public void RemovePackage(string packageName)
    {
        string packageFolder =
            Path.Combine(PackageRoot, packageName);

        if (Directory.Exists(packageFolder))
        {
            Directory.Delete(packageFolder, true);
        }
    }

    public void RenamePackage(string oldName, string newName)
    {
        string source =
            Path.Combine(PackageRoot, oldName);

        string destination =
            Path.Combine(PackageRoot, newName);

        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(
                $"Package '{oldName}' not found.");
        }

        if (Directory.Exists(destination))
        {
            throw new InvalidOperationException(
                $"Package '{newName}' already exists.");
        }

        Directory.Move(source, destination);
    }

    public string? GetUsedPackageName(string destinationFolder)
    {
        if (string.IsNullOrWhiteSpace(destinationFolder))
            return null;

        var map = LoadUsedPackagesMap();

        if (map.TryGetValue(destinationFolder, out string? packageName)
            && !string.IsNullOrWhiteSpace(packageName))
        {
            return packageName;
        }

        return null;
    }

    public void SetUsedPackageName(string destinationFolder, string packageName)
    {
        if (string.IsNullOrWhiteSpace(destinationFolder))
            return;

        var map = LoadUsedPackagesMap();

        if (string.IsNullOrWhiteSpace(packageName))
        {
            map.Remove(destinationFolder);
        }
        else
        {
            map[destinationFolder] = packageName;
        }

        SaveUsedPackagesMap(map);
    }

    private Dictionary<string, string> LoadUsedPackagesMap()
    {
        string configPath =
            Path.Combine(PackageRoot, UsedPackagesConfigFileName);

        if (!File.Exists(configPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string json = File.ReadAllText(configPath);

        var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        return map ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private void SaveUsedPackagesMap(Dictionary<string, string> map)
    {
        string configPath =
            Path.Combine(PackageRoot, UsedPackagesConfigFileName);

        string json = JsonSerializer.Serialize(
            map,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(configPath, json);
    }

    private int CountTeams(string csv)
    {
        return File.ReadLines(csv)
                   .Skip(1)
                   .Count();
    }

    private string FindPackageFolder(string extractedRoot)
    {
        var candidateFolders =
            Directory.GetDirectories(
                extractedRoot,
                "*",
                SearchOption.AllDirectories)
            .Prepend(extractedRoot);

        foreach (var folder in candidateFolders)
        {
            string csv =
                Path.Combine(folder, CsvFileName);

            string iconFolder =
                Path.Combine(folder, IconFolderName);

            if (File.Exists(csv) && Directory.Exists(iconFolder))
            {
                return folder;
            }
        }

        throw new InvalidDataException(
            "Zip package must contain TeamInfo.csv and TeamIcon folder.");
    }

    private void CopyDirectory(string source, string destination)
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