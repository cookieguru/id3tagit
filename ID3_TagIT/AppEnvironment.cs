using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ID3_TagIT
{
  internal enum InstallScope { None, CurrentUser, AllUsers }

  // Decides, once at startup, where this copy keeps its settings, temp files and debug log, and
  // how it was installed. A copy with Portable.txt next to the exe keeps everything in its own
  // folder; any other copy uses the user's profile, as 3.x always did.
  internal static class AppEnvironment
  {
    public const string PortableMarker = "Portable.txt";
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ID3-TagIT 3_is1";

    private static readonly List<string> alstTempFiles = new List<string>();
    private static string vstrTempFolder;
    private static bool vbooScopeKnown;
    private static InstallScope vintScope;

    public static bool IsPortable { get; private set; }
    public static string ExeFolder { get; private set; }
    public static string SettingsFolder { get; private set; }
    public static string LogFolder { get; private set; }

    public static string TempFolder
    {
      get
      {
        Directory.CreateDirectory(vstrTempFolder);
        return vstrTempFolder;
      }
    }

    public static InstallScope InstallScope
    {
      get
      {
        if (!vbooScopeKnown)
        {
          vintScope = IsPortable ? InstallScope.None : DetectInstallScope(Registry.LocalMachine, Registry.CurrentUser, ExeFolder);
          vbooScopeKnown = true;
        }

        return vintScope;
      }
    }

    public static void Initialize(string exeFolder, string appData, string systemTemp)
    {
      ExeFolder = exeFolder;
      IsPortable = File.Exists(Path.Combine(exeFolder, PortableMarker));
      vbooScopeKnown = false;

      if (IsPortable)
      {
        SettingsFolder = Path.Combine(exeFolder, "Settings") + @"\";
        vstrTempFolder = Path.Combine(exeFolder, "Temp");
        LogFolder = exeFolder;
      }
      else
      {
        SettingsFolder = Path.Combine(appData, "ID3-TagIT 3") + @"\";
        vstrTempFolder = systemTemp;
        LogFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
      }
    }

    // Creates an empty, uniquely named file in the temp folder and remembers it for
    // DeleteOwnTempFiles.
    public static string NewTempFile(string extension)
    {
      string path = Path.Combine(TempFolder, Path.GetRandomFileName());

      if (!string.IsNullOrEmpty(extension))
        path = Path.ChangeExtension(path, extension);

      using (new FileStream(path, FileMode.CreateNew))
      {
      }

      lock (alstTempFiles)
        alstTempFiles.Add(path);

      return path;
    }

    // Only the files this process created: another portable instance may share the folder.
    public static void DeleteOwnTempFiles()
    {
      lock (alstTempFiles)
      {
        foreach (string path in alstTempFiles)
        {
          try
          {
            File.Delete(path);
          }
          catch (IOException)
          {
            // Still open in another program; leave it.
          }
          catch (UnauthorizedAccessException)
          {
          }
        }

        alstTempFiles.Clear();
      }
    }

    // machine and user are the HKLM and HKCU roots (tests pass throwaway keys). The installer's
    // uninstall entry records the folder it installed to.
    public static InstallScope DetectInstallScope(RegistryKey machine, RegistryKey user, string exeFolder)
    {
      if (InstalledHere(machine, exeFolder))
        return InstallScope.AllUsers;

      if (InstalledHere(user, exeFolder))
        return InstallScope.CurrentUser;

      return InstallScope.None;
    }

    private static bool InstalledHere(RegistryKey root, string exeFolder)
    {
      using (RegistryKey key = root.OpenSubKey(UninstallKey))
      {
        string location = (key == null) ? null : key.GetValue("InstallLocation") as string;

        return !string.IsNullOrEmpty(location) && string.Equals(Normalize(location), Normalize(exeFolder), StringComparison.OrdinalIgnoreCase);
      }
    }

    private static string Normalize(string folder)
    {
      return Path.GetFullPath(folder).TrimEnd('\\');
    }
  }
}
