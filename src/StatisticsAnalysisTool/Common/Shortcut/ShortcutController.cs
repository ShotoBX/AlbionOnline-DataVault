using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace StatisticsAnalysisTool.Common.Shortcut;

public static class ShortcutController
{
    public static void CreateShortcut()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var link = (IShellLink)new ShellLink();
        link.SetDescription("AlbionOnline - DataVault");
        link.SetPath(AppDataPaths.ExecutableFile);

        link.SetWorkingDirectory(AppDataPaths.InstallationDirectory);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var file = (IPersistFile)link;
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        file.Save(Path.Combine(desktopPath, "AlbionOnline - DataVault.lnk"), false);
    }
}