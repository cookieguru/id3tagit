using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using ID3_TagIT;

namespace ID3_TagIT.Tests
{
  // The app keeps its settings and lookup tables in statics (Declarations), so tests share one
  // initialized copy. Settings come from a generated Settings.xml in a temporary folder rather
  // than the user's app data.
  internal static class TestApp
  {
    private static readonly object Lock = new object();
    private static bool vbooInitialized;

    public static Settings Settings
    {
      get
      {
        EnsureInitialized();
        return Declarations.objSettings;
      }
    }

    public static void EnsureInitialized()
    {
      lock (Lock)
      {
        if (vbooInitialized)
          return;

        string appData = Path.Combine(Path.GetTempPath(), "ID3-TagIT.Tests-" + Guid.NewGuid().ToString("N")) + "\\";
        Directory.CreateDirectory(appData);
        WriteSettingsXml(Path.Combine(appData, "Settings.xml"));

        Declarations.vstrUserAppData = appData;
        Declarations.vstrCommonAppData = appData;
        Declarations.Declaration();
        Declarations.objSettings.ReadSettings();
        AddMissingSettings(Declarations.objSettings);
        Declarations.objResources.ReadResources();
        vbooInitialized = true;
      }
    }

    // There is no default Settings.xml to start from, so every other setting a Settings property
    // reads gets a column holding its type's default (false, 0 or ""). A property that fails
    // names the missing column; keep adding until every property reads.
    private static void AddMissingSettings(Settings settings)
    {
      DataTable table = settings.SettingsTable;
      Regex missing = new Regex("Column '([^']+)' does not belong");
      bool added = true;

      while (added)
      {
        added = false;

        foreach (PropertyInfo property in typeof(Settings).GetProperties())
        {
          if (!property.CanRead || property.GetIndexParameters().Length > 0)
            continue;

          try
          {
            property.GetValue(settings, null);
          }
          catch (TargetInvocationException exception)
          {
            Match match = missing.Match(exception.InnerException.Message);

            if (!match.Success || table.Columns.Contains(match.Groups[1].Value))
              continue;

            string name = match.Groups[1].Value;
            PropertyInfo own = typeof(Settings).GetProperty(name);
            Type type = (own != null && own.PropertyType.IsPrimitive) ? own.PropertyType : typeof(string);
            table.Columns.Add(name, type).DefaultValue = (type == typeof(string)) ? (object)"" : Activator.CreateInstance(type);
            table.Rows[0][name] = table.Columns[name].DefaultValue;
            added = true;
          }
        }
      }
    }

    // The settings the tested code uses, with the values of a default install.
    private static void WriteSettingsXml(string path)
    {
      DataSet set = new DataSet("Settings");
      DataTable settings = set.Tables.Add("Settings");
      settings.Columns.Add("Path", typeof(string));
      settings.Columns.Add("Language", typeof(byte));
      settings.Columns.Add("IgnoreWrite", typeof(bool));
      settings.Columns.Add("RestoreDate", typeof(bool));
      settings.Columns.Add("WriteUnsync", typeof(bool));
      settings.Columns.Add("NewV2Version", typeof(byte));
      settings.Columns.Add("V23Encoding", typeof(byte));
      settings.Columns.Add("V24Encoding", typeof(byte));
      settings.Columns.Add("V2Language", typeof(int));
      settings.Columns.Add("TracknumberDigitsTAG", typeof(byte));
      settings.Columns.Add("TracknumberDigitsFilename", typeof(byte));
      settings.Columns.Add("SynchronizeTAGs", typeof(bool));
      settings.Columns.Add("ScanSubDirs", typeof(bool));
      settings.Columns.Add("AudioChecksumCalc", typeof(bool));
      settings.Columns.Add("ColorChangedBack", typeof(int));
      settings.Columns.Add("ColorChangedText", typeof(int));
      settings.Columns.Add("ColorDoubleBack", typeof(int));
      settings.Columns.Add("ColorDoubleText", typeof(int));
      settings.Columns.Add("ColorCompareBack", typeof(int));
      settings.Columns.Add("ColorCompareText", typeof(int));
      settings.Rows.Add(Path.GetTempPath(), (byte)0, false, true, false, (byte)3, (byte)0, (byte)0, 104, (byte)2, (byte)2, false, false, true,
        -128, -16777216, -32640, -16777216, -8323200, -16777216);

      DataTable forms = set.Tables.Add("Forms");
      forms.Columns.Add("Name", typeof(string));

      set.WriteXml(path, XmlWriteMode.WriteSchema);
    }
  }
}
