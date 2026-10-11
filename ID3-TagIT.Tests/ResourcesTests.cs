using System.Data;
using System.IO;
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
      DataSet set = new DataSet();

      using (Stream stream = typeof(Resources).Assembly.GetManifestResourceStream(resource))
        set.ReadXml(stream);

      DataRow strings = set.Tables[0].Rows[0];
      DataRow preferences = set.Tables["frmPreferences"].Rows[0];

      foreach (string key in new[] { "ThemeSystem", "ThemeLight", "ThemeDark", "Col27", "SettingsReadOnly" })
        Assert.False(string.IsNullOrEmpty(strings[key] as string), key + " is missing");

      foreach (string key in new[] { "lblTheme", "chkShellMenuMe", "chkShellMenuAll", "lblShellMenuPortable" })
        Assert.False(string.IsNullOrEmpty(preferences[key] as string), key + " is missing");
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
