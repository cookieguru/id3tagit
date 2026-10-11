using System.IO;

namespace ID3_TagIT
{
  // The settings and libraries a new user starts with, built into the exe (from the 3.3.0
  // installer's defaults), so every edition can start on a clean machine. Library-Favourites.xml
  // is not among them: despite its name it holds BinaryFormatter data, and a missing file
  // already means no favourites.
  internal static class DefaultSettings
  {
    public static readonly string[] Files = new string[]
    {
      "Settings.xml",
      @"Libraries\Library-Artists.xml",
      @"Libraries\Library-Columns.xml",
      @"Libraries\Library-CommDescriptors.xml",
      @"Libraries\Library-Exceptions.xml",
      @"Libraries\Library-Formats.xml",
      @"Libraries\Library-Genres.xml",
      @"Libraries\Library-Tools.xml"
    };

    // relativePath is one of Files.
    public static Stream Open(string relativePath)
    {
      return typeof(DefaultSettings).Assembly.GetManifestResourceStream("ID3_TagIT.Defaults." + relativePath.Replace('\\', '.'));
    }

    // Writes each default that is missing from settingsFolder; existing files are left alone.
    public static void WriteMissing(string settingsFolder)
    {
      Directory.CreateDirectory(Path.Combine(settingsFolder, "Libraries"));

      foreach (string file in Files)
      {
        string path = Path.Combine(settingsFolder, file);

        if (!File.Exists(path))
          Write(file, path);
      }
    }

    // Replaces the file at path with the default for relativePath.
    public static void Write(string relativePath, string path)
    {
      using (Stream source = Open(relativePath))
      using (FileStream target = new FileStream(path, FileMode.Create))
        source.CopyTo(target);
    }
  }
}
