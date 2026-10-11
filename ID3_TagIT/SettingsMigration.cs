using System;
using System.IO;

namespace ID3_TagIT
{
  // Prepares the settings folder at startup: writes any missing defaults, applies the resets
  // older versions needed once, and records the running version in VersionInfo.txt.
  internal static class SettingsMigration
  {
    public static readonly Version CommDescriptorsReset = new Version(3, 1, 3);
    public static readonly Version ColumnsReset = new Version(3, 4, 0);

    // The version that last used the folder: null when there are no settings yet (first run),
    // 0.0.0 when Settings.xml exists but VersionInfo.txt is missing or unreadable.
    public static Version ReadPrevious(string settingsFolder)
    {
      if (!File.Exists(Path.Combine(settingsFolder, "Settings.xml")))
        return null;

      string versionFile = Path.Combine(settingsFolder, "VersionInfo.txt");
      Version version;

      if (File.Exists(versionFile))
      {
        using (StreamReader reader = new StreamReader(versionFile))
        {
          string line = reader.ReadLine();

          if (line != null && Version.TryParse(line.Trim(), out version))
            return version;
        }
      }

      return new Version(0, 0, 0);
    }

    // Returns ReadPrevious's result. Throws IOException or UnauthorizedAccessException when the
    // folder can't be written.
    public static Version Run(string settingsFolder, Version current)
    {
      Version previous = ReadPrevious(settingsFolder);

      Directory.CreateDirectory(settingsFolder);
      DefaultSettings.WriteMissing(settingsFolder);

      if (previous != null && previous < CommDescriptorsReset)
        Reset(settingsFolder, @"Libraries\Library-CommDescriptors.xml");

      if (previous != null && previous < ColumnsReset)
        Reset(settingsFolder, @"Libraries\Library-Columns.xml");

      File.WriteAllText(Path.Combine(settingsFolder, "VersionInfo.txt"), current.ToString(3));
      return previous;
    }

    private static void Reset(string settingsFolder, string file)
    {
      DefaultSettings.Write(file, Path.Combine(settingsFolder, file));
    }
  }
}
