using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class ResourcesTests
  {
    public ResourcesTests()
    {
      TestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData("ID3_TagIT.Resources.xml")]
    [InlineData("ID3_TagIT.Resources-Ger.xml")]
    [InlineData("ID3_TagIT.Resources-Pol.xml")]
    public void EveryLanguageHasTheStringsAddedSince330(string resource)
    {
      DataSet set = ReadResource(resource);
      DataRow strings = set.Tables[0].Rows[0];
      DataRow preferences = set.Tables["frmPreferences"].Rows[0];

      foreach (string key in new[] { "ThemeSystem", "ThemeLight", "ThemeDark", "Col27", "SettingsReadOnly" })
        Assert.False(string.IsNullOrEmpty(strings[key] as string), key + " is missing");

      foreach (string key in new[] { "lblTheme", "chkShellMenuMe", "chkShellMenuAll", "lblShellMenuPortable" })
        Assert.False(string.IsNullOrEmpty(preferences[key] as string), key + " is missing");
    }

    [Theory]
    [InlineData("ID3_TagIT.Resources.xml")]
    [InlineData("ID3_TagIT.Resources-Ger.xml")]
    [InlineData("ID3_TagIT.Resources-Pol.xml")]
    [InlineData("ID3_TagIT.MenuResources.xml")]
    [InlineData("ID3_TagIT.MenuResources-Ger.xml")]
    [InlineData("ID3_TagIT.MenuResources-Pol.xml")]
    [InlineData("ID3_TagIT.Tooltips.xml")]
    [InlineData("ID3_TagIT.Tooltips-Ger.xml")]
    [InlineData("ID3_TagIT.Tooltips-Pol.xml")]
    public void EveryLanguageFileLoads(string resource)
    {
      Assert.NotEmpty(ReadResource(resource).Tables);
    }

    [Theory]
    [InlineData("ID3_TagIT.MenuResources-Pol.xml", "btnV1V2View", "Wy\u015Bwietlana wersja tag\u00F3w")]
    [InlineData("ID3_TagIT.Tooltips-Pol.xml", "chkAlwaysFirst", "Zmiana pierwszej litery ka\u017Cdego pola/nazwy pliku na wielk\u0105, poza wyj\u0105tkami")]
    public void PolishTextKeepsItsLetters(string resource, string key, string expected)
    {
      Assert.Contains(expected, ValuesOf(ReadResource(resource), key));
    }

    private static DataSet ReadResource(string resource)
    {
      DataSet set = new DataSet();

      using (Stream stream = typeof(Resources).Assembly.GetManifestResourceStream(resource))
        set.ReadXml(stream);

      return set;
    }

    private static string[] ValuesOf(DataSet set, string key)
    {
      return set.Tables.Cast<DataTable>()
        .Where(table => table.Columns.Contains(key))
        .SelectMany(table => table.Rows.Cast<DataRow>().Select(row => row[key] as string))
        .ToArray();
    }

    [Fact]
    public void ResourcesToFormTranslatesKnownControlsAndSkipsTheRest()
    {
      Sta.Run(delegate ()
      {
        using (Form form = new Form { Name = "frmPreferences" })
        {
          Label known = new Label { Name = "lblTheme", Text = "before" };
          Label unknown = new Label { Name = "lblNotTranslated", Text = "unchanged" };
          form.Controls.Add(known);
          form.Controls.Add(unknown);
          Form reference = form;

          Declarations.objResources.ResourcesToForm(ref reference);

          Assert.Equal("Color theme:", known.Text);
          Assert.Equal("unchanged", unknown.Text);
        }
      });
    }
  }
}
