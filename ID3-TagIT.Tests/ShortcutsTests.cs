using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  // The keyboard shortcuts dialog is a fixed list; it must match the menus' ShortcutKeys.
  public class ShortcutsTests
  {
    private const BindingFlags Statics = BindingFlags.Static | BindingFlags.NonPublic;

    private static string[,] Table(string name)
    {
      return (string[,])typeof(frmShortcuts).GetField(name, Statics).GetValue(null);
    }

    // The shortcuts in the dialog's own notation: "Ctrl+Shift+D", "Shift+F1" and so on.
    private static HashSet<string> Listed()
    {
      HashSet<string> listed = new HashSet<string>();
      string[,] normal = Table("NormalKeys");
      string[,] function = Table("FunctionKeys");
      string[] modifiers = { "", "Shift+", "Ctrl+", "Alt+" };

      for (int row = 0; row < normal.GetLength(0); row++)
        listed.Add(normal[row, 0]);

      for (int row = 0; row < function.GetLength(0); row++)
        for (int column = 1; column <= 4; column++)
          if (function[row, column] != "" && function[row, column] != "---")
            listed.Add(modifiers[column - 1] + function[row, 0]);

      return listed;
    }

    private static string Describe(Keys keys)
    {
      string text = "";

      if ((keys & Keys.Control) != 0)
        text += "Ctrl+";

      if ((keys & Keys.Shift) != 0)
        text += "Shift+";

      if ((keys & Keys.Alt) != 0)
        text += "Alt+";

      return text + (keys & Keys.KeyCode);
    }

    private static Dictionary<string, string> MenuShortcuts()
    {
      Dictionary<string, string> shortcuts = new Dictionary<string, string>();

      Sta.Run(delegate ()
      {
        using (MainForm main = new MainForm())
          foreach (FieldInfo field in typeof(frmMain).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
          {
            ToolStripMenuItem item = field.GetValue(main.Form) as ToolStripMenuItem;

            if (item != null && item.ShortcutKeys != Keys.None)
              shortcuts[Describe(item.ShortcutKeys)] = field.Name;
          }
      });

      return shortcuts;
    }

    [Fact]
    public void EveryMenuShortcutIsListed()
    {
      HashSet<string> listed = Listed();
      string[] missing = MenuShortcuts().Where(pair => !listed.Contains(pair.Key)).Select(pair => pair.Key + " (" + pair.Value + ")").ToArray();

      Assert.Empty(missing);
    }

    [Fact]
    public void EveryListedShortcutIsOnAMenu()
    {
      Dictionary<string, string> menus = MenuShortcuts();

      Assert.Empty(Listed().Where(key => !menus.ContainsKey(key)).OrderBy(key => key));
    }
  }
}
