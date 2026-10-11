using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.Win32;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  public class StartupTests
  {
    private const string ShellKey = @"Software\Classes\Directory\shell\ID3-TagIT";

    // Everything an installed copy would touch: its settings folder and its right-click entry.
    private static string OutsideState()
    {
      string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ID3-TagIT 3");
      string files = Directory.Exists(appData)
        ? string.Join("\n", Directory.GetFiles(appData, "*", SearchOption.AllDirectories).OrderBy(f => f).Select(f => f + "|" + new FileInfo(f).Length + "|" + File.GetLastWriteTimeUtc(f).Ticks))
        : "(no settings folder)";

      using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ShellKey + @"\command"))
        return files + "\n" + ((key == null) ? "(no right-click entry)" : Convert.ToString(key.GetValue(null)));
    }

    [Fact]
    public void FirstLaunchKeepsEverythingInThePortableFolder()
    {
      string before = OutsideState();

      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("tagless.mp3"));

        app.Launch(1);
        app.Close();

        string version = FileVersionInfo.GetVersionInfo(Path.Combine(app.AppFolder, "ID3-TagIT.exe")).FileVersion;
        Assert.True(File.Exists(app.SettingsFile));
        Assert.Equal(string.Join(".", version.Split('.').Take(3)), File.ReadAllText(Path.Combine(app.AppFolder, @"Settings\VersionInfo.txt")).Trim());
        Assert.True(Directory.Exists(Path.Combine(app.AppFolder, @"Settings\Libraries")));
      }

      Assert.Equal(before, OutsideState());
    }

    // frmLists reads its export templates from Templetes\ next to the exe; 3.x only got them from
    // its installer.
    [Theory]
    [InlineData("CSV-Short.itl")]
    [InlineData("HTML-Short.itl")]
    [InlineData("RTF-Short.itl")]
    [InlineData("SFV (CRC32 Checksum file).itl")]
    [InlineData("Readme.txt")]
    public void TheListExportTemplatesShipWithTheApp(string file)
    {
      using (PortableApp app = new PortableApp())
        Assert.True(File.Exists(Path.Combine(app.AppFolder, "Templetes", file)), file + " is missing");
    }

    // 3.4/3.5 betas reset the column list and other files on every start.
    [Fact]
    public void SettingsSurviveARestart()
    {
      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("tagless.mp3"));
        app.Launch(1);
        Assert.Contains(Headers(app), h => h == "Publisher");

        app.Shortcut(FlaUI.Core.WindowsAPI.VirtualKeyShort.F9, FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT);
        app.Close();
        Assert.Equal("True", app.ReadSetting("ScanSubDirs"));

        app.Launch(1);
        Assert.Contains(Headers(app), h => h == "Publisher");
        app.Close();
        Assert.Equal("True", app.ReadSetting("ScanSubDirs"));
      }
    }

    private static string[] Headers(PortableApp app)
    {
      return app.FileList.FindFirstChild(cf => cf.ByControlType(ControlType.Header)).FindAllChildren().Select(h => h.Name).ToArray();
    }
  }
}
