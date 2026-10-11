using System;
using System.IO;
using ID3_TagIT;

namespace ID3_TagIT.Tests
{
  // The app keeps its settings and lookup tables in statics (Declarations), so tests share one
  // initialized copy. It starts from the defaults built into the exe, in a temporary folder
  // rather than the user's app data.
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
        DefaultSettings.WriteMissing(appData);

        Declarations.vstrUserAppData = appData;
        Declarations.Declaration();
        Declarations.objSettings.ReadSettings();
        Declarations.objResources.ReadResources();

        // The tests that depend on these expect them on.
        Declarations.objSettings.AudioChecksumCalc = true;
        Declarations.objSettings.RestoreDate = true;
        vbooInitialized = true;
      }
    }
  }
}
