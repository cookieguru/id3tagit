using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class SettingsMigrationTests
  {
    private static readonly Version Current = new Version(3, 5, 0);
    private const string Columns = @"Libraries\Library-Columns.xml";
    private const string CommDescriptors = @"Libraries\Library-CommDescriptors.xml";

    private static string Default(string file)
    {
      using (StreamReader reader = new StreamReader(DefaultSettings.Open(file)))
        return reader.ReadToEnd();
    }

    // A 3.x settings folder: every default file, with the given VersionInfo.txt (null for none).
    private static string OldSettings(TempFolder folder, string version)
    {
      string settings = folder.Folder("settings") + @"\";
      DefaultSettings.WriteMissing(settings);

      if (version != null)
        File.WriteAllText(settings + "VersionInfo.txt", version);

      File.WriteAllText(settings + Columns, "custom columns");
      File.WriteAllText(settings + CommDescriptors, "custom descriptors");
      return settings;
    }

    // Points the app's settings folder somewhere else for the duration of a test.
    private static void WithSettingsFolder(string settings, Action test)
    {
      TestApp.EnsureInitialized();
      string previous = Declarations.vstrUserAppData;
      Declarations.vstrUserAppData = settings;

      try
      {
        test();
      }
      finally
      {
        Declarations.vstrUserAppData = previous;
      }
    }

    [Fact]
    public void FirstRunWritesEveryDefault()
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = Path.Combine(folder.Path, "new") + @"\";

        Assert.Null(SettingsMigration.Run(settings, Current));

        Assert.Equal(8, DefaultSettings.Files.Length);
        foreach (string file in DefaultSettings.Files)
          Assert.Equal(Default(file), File.ReadAllText(settings + file));
        Assert.Equal("3.5.0", File.ReadAllText(settings + "VersionInfo.txt").Trim());
      }
    }

    [Fact]
    public void EveryDefaultIsValidXml()
    {
      foreach (string file in DefaultSettings.Files)
        using (Stream stream = DefaultSettings.Open(file))
          new System.Xml.XmlDocument().Load(stream);
    }

    [Fact]
    public void ExistingFilesAreNotOverwritten()
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = OldSettings(folder, "3.5.0");
        File.Delete(settings + @"Libraries\Library-Genres.xml");
        File.WriteAllText(settings + @"Libraries\Library-Artists.xml", "my artists");

        SettingsMigration.Run(settings, Current);

        Assert.Equal("my artists", File.ReadAllText(settings + @"Libraries\Library-Artists.xml"));
        Assert.Equal(Default(@"Libraries\Library-Genres.xml"), File.ReadAllText(settings + @"Libraries\Library-Genres.xml"));
      }
    }

    [Fact]
    public void UpgradeFrom330ResetsColumnsOnce()
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = OldSettings(folder, "3.3.0");

        Assert.Equal(new Version(3, 3, 0), SettingsMigration.Run(settings, Current));
        Assert.Equal(Default(Columns), File.ReadAllText(settings + Columns));
        Assert.Equal("custom descriptors", File.ReadAllText(settings + CommDescriptors));

        File.WriteAllText(settings + Columns, "changed after upgrading");
        Assert.Equal(Current, SettingsMigration.Run(settings, Current));
        Assert.Equal("changed after upgrading", File.ReadAllText(settings + Columns));
      }
    }

    [Fact]
    public void UpgradeFrom310AlsoResetsCommDescriptors()
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = OldSettings(folder, "3.1.0");

        SettingsMigration.Run(settings, Current);

        Assert.Equal(Default(Columns), File.ReadAllText(settings + Columns));
        Assert.Equal(Default(CommDescriptors), File.ReadAllText(settings + CommDescriptors));
      }
    }

    [Fact]
    public void VersionsCompareNumerically()
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = OldSettings(folder, "3.10.0");

        SettingsMigration.Run(settings, new Version(3, 10, 0));

        Assert.Equal("custom columns", File.ReadAllText(settings + Columns));
      }
    }

    [Theory]
    [InlineData("")]
    [InlineData("3.3.0 beta")]
    [InlineData(null)]
    public void UnreadableVersionCountsAsOld(string version)
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = OldSettings(folder, version);

        Assert.Equal(new Version(0, 0, 0), SettingsMigration.Run(settings, Current));
        Assert.Equal(Default(Columns), File.ReadAllText(settings + Columns));
      }
    }

    [Fact]
    public void EveryDefaultSettingReads()
    {
      using (TempFolder folder = new TempFolder())
      {
        string settings = folder.Folder("settings") + @"\";
        DefaultSettings.WriteMissing(settings);

        WithSettingsFolder(settings, delegate ()
        {
          Settings defaults = new Settings();
          defaults.ReadSettings();

          foreach (PropertyInfo property in typeof(Settings).GetProperties())
            if (property.CanRead && property.GetIndexParameters().Length == 0)
              property.GetValue(defaults, null);
        });
      }
    }

    [Fact]
    public void UnwritableFolderFallsBackToDefaults()
    {
      using (TempFolder folder = new TempFolder())
      {
        // A folder can't be created under a file.
        string blocker = folder.File("blocker");
        File.WriteAllText(blocker, "");
        string settings = Path.Combine(blocker, "Settings") + @"\";

        Assert.Throws<IOException>(delegate () { SettingsMigration.Run(settings, Current); });

        WithSettingsFolder(settings, delegate ()
        {
          Settings defaults = new Settings();
          defaults.ReadOnly = true;
          defaults.ReadSettings();

          Assert.Equal(2, defaults.VersionToShow);
          defaults.WriteSettings();
        });
      }
    }
  }
}
