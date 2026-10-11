using System.Data;
using System.Drawing;
using Xunit;

namespace ID3_TagIT.Tests
{
  // Settings added after 3.3.0 are optional columns: settings files from older versions don't
  // have them, so they must read as defaults until first written.
  public class SettingsTests
  {
    private readonly Settings settings = TestApp.Settings;

    private void RemoveColumn(string name)
    {
      if (this.settings.SettingsTable.Columns.Contains(name))
        this.settings.SettingsTable.Columns.Remove(name);
    }

    [Fact]
    public void MissingThemeMeansFollowWindows()
    {
      this.RemoveColumn("Theme");

      Assert.Equal(ThemeManager.THEME_SYSTEM, this.settings.Theme);

      this.settings.Theme = ThemeManager.THEME_DARK;
      Assert.Equal(ThemeManager.THEME_DARK, this.settings.Theme);
      this.RemoveColumn("Theme");
    }

    [Fact]
    public void MissingToolbarLayoutIsEmpty()
    {
      this.RemoveColumn("ToolbarLayout");

      Assert.Equal("", this.settings.ToolbarLayout);

      this.settings.ToolbarLayout = "mnuBtns,tspTop,0,0";
      Assert.Equal("mnuBtns,tspTop,0,0", this.settings.ToolbarLayout);
      this.RemoveColumn("ToolbarLayout");
    }

    [Fact]
    public void MissingDarkHighlightColorsUseTheDarkDefaults()
    {
      this.RemoveColumn("ColorChangedBackDark");

      Assert.Equal(Color.FromArgb(92, 80, 10).ToArgb(), this.settings.ColorChangedBackDark);

      this.settings.ColorChangedBackDark = Color.Red.ToArgb();
      Assert.Equal(Color.Red.ToArgb(), this.settings.ColorChangedBackDark);
      this.RemoveColumn("ColorChangedBackDark");
    }

    [Fact]
    public void AddNewColumnsShowsPublisherOnce()
    {
      this.RemoveColumn("KnownColumns");
      DataTable columns = new DataTable("Columns");
      columns.Columns.Add("ID", typeof(int));
      columns.Columns.Add("Width", typeof(int));
      columns.Columns.Add("Alignment", typeof(string));
      columns.Rows.Add(1, 200, "Left");
      typeof(Settings).GetField("ColumnsTable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(this.settings, columns);

      this.settings.AddNewColumns();
      Assert.Equal(1, columns.Select("ID = 27").Length);
      Assert.Equal(27, this.settings.KnownColumns);

      // Once offered, hiding it sticks.
      columns.Select("ID = 27")[0].Delete();
      this.settings.AddNewColumns();
      Assert.Empty(columns.Select("ID = 27"));
      this.RemoveColumn("KnownColumns");
    }
  }
}
