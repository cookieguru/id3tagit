using System.IO;
using Microsoft.Win32;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class AppEnvironmentTests
  {
    private const string TestKey = @"Software\ID3-TagIT.Tests";

    [Fact]
    public void WithoutTheMarkerSettingsAreInAppData()
    {
      using (TempFolder folder = new TempFolder())
      {
        string exe = folder.Folder("exe");
        string appData = folder.Folder("appdata");

        AppEnvironment.Initialize(exe, appData, folder.Folder("temp"));

        Assert.False(AppEnvironment.IsPortable);
        Assert.Equal(appData + @"\ID3-TagIT 3\", AppEnvironment.SettingsFolder);
        Assert.StartsWith(Path.Combine(folder.Path, "temp"), AppEnvironment.NewTempFile("jpg"));
      }
    }

    [Fact]
    public void WithTheMarkerEverythingStaysInTheExeFolder()
    {
      using (TempFolder folder = new TempFolder())
      {
        string exe = folder.Folder("exe");
        File.WriteAllText(Path.Combine(exe, AppEnvironment.PortableMarker), "");

        AppEnvironment.Initialize(exe, folder.Folder("appdata"), folder.Folder("temp"));
        string temp = AppEnvironment.NewTempFile("jpg");

        Assert.True(AppEnvironment.IsPortable);
        Assert.Equal(exe + @"\Settings\", AppEnvironment.SettingsFolder);
        Assert.Equal(Path.Combine(exe, "Temp"), Path.GetDirectoryName(temp));
        Assert.EndsWith(".jpg", temp);
        Assert.True(File.Exists(temp));
        Assert.Equal(exe, AppEnvironment.LogFolder);
        AppEnvironment.DeleteOwnTempFiles();
      }
    }

    // Two portable instances share .\Temp\; each must leave the other's files alone.
    [Fact]
    public void DeletesOnlyItsOwnTempFiles()
    {
      using (TempFolder folder = new TempFolder())
      {
        string exe = folder.Folder("exe");
        File.WriteAllText(Path.Combine(exe, AppEnvironment.PortableMarker), "");
        AppEnvironment.Initialize(exe, folder.Folder("appdata"), folder.Folder("temp"));
        string foreign = Path.Combine(AppEnvironment.TempFolder, "other-instance.jpg");
        File.WriteAllText(foreign, "");
        string first = AppEnvironment.NewTempFile("jpg");
        string second = AppEnvironment.NewTempFile("png");

        AppEnvironment.DeleteOwnTempFiles();

        Assert.False(File.Exists(first));
        Assert.False(File.Exists(second));
        Assert.True(File.Exists(foreign));
      }
    }

    [Fact]
    public void InstallScopeMatchesTheUninstallEntry()
    {
      using (TempFolder folder = new TempFolder())
      {
        string exe = folder.Folder("exe");

        try
        {
          using (RegistryKey machine = Registry.CurrentUser.CreateSubKey(TestKey + @"\Machine"))
          using (RegistryKey user = Registry.CurrentUser.CreateSubKey(TestKey + @"\User"))
          {
            Assert.Equal(InstallScope.None, AppEnvironment.DetectInstallScope(machine, user, exe));

            // Inno writes InstallLocation with a trailing backslash; case may differ.
            SetLocation(machine, exe.ToUpperInvariant() + @"\");
            Assert.Equal(InstallScope.AllUsers, AppEnvironment.DetectInstallScope(machine, user, exe));

            SetLocation(machine, folder.Folder("elsewhere") + @"\");
            Assert.Equal(InstallScope.None, AppEnvironment.DetectInstallScope(machine, user, exe));

            SetLocation(user, exe + @"\");
            Assert.Equal(InstallScope.CurrentUser, AppEnvironment.DetectInstallScope(machine, user, exe));
          }
        }
        finally
        {
          Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
        }
      }
    }

    private static void SetLocation(RegistryKey root, string location)
    {
      using (RegistryKey key = root.CreateSubKey(AppEnvironment.UninstallKey))
        key.SetValue("InstallLocation", location);
    }
  }
}
