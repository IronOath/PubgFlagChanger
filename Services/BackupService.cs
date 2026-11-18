using System.IO;

namespace PUBGFlagChanger.Services;

public class BackupService
{
    public void CreateBackup(string destination)
    {
        if (!Directory.Exists(destination))
            return;

        string backupRoot =
            Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Backups");

        Directory.CreateDirectory(backupRoot);

        string backupFolder =
            Path.Combine(
                backupRoot,
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss"));

        Directory.CreateDirectory(backupFolder);

        string csv =
            Path.Combine(destination,
            "TeamInfo.csv");

        if (File.Exists(csv))
        {
            File.Copy(
                csv,
                Path.Combine(backupFolder,
                "TeamInfo.csv"));
        }

        string icons =
            Path.Combine(destination,
            "TeamIcon");

        if (Directory.Exists(icons))
        {
            CopyDirectory(
                icons,
                Path.Combine(backupFolder,
                "TeamIcon"));
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

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(
                dir,
                Path.Combine(
                    destination,
                    Path.GetFileName(dir)));
        }
    }
}