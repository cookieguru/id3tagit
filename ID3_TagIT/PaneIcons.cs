using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace ID3_TagIT
{
  // The 16 pixel icons from the 3.3.0 tag editor's left pane, embedded as PNG resources.
  internal static class PaneIcons
  {
    internal static readonly string[] Names =
    {
      "main-entries", "detailed-info", "original-info", "involved-people", "web-info", "pictures",
      "lyrics", "rating", "user-defined", "unsupported", "previous", "next"
    };

    private const string RESOURCE_PREFIX = "ID3_TagIT.Images.";

    internal static Image Get(string name)
    {
      if (Array.IndexOf(Names, name) < 0)
        throw new ArgumentException("Unknown pane icon: " + name, "name");

      using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_PREFIX + name + ".png"))
      using (Image stored = Image.FromStream(stream))
        return new Bitmap(stored);
    }
  }
}
