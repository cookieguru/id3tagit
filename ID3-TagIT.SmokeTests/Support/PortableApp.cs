using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // A private portable copy of the app in a temp folder, with a music folder beside it, driven
  // through UI Automation. The build to copy comes from ID3TAGIT_SMOKE_APP (CI points it at the
  // extracted portable zip), or the repo's bin\Release.
  //
  // Dialogs are opened with their keyboard shortcuts: invoking a menu item through UI Automation
  // blocks until a modal dialog it opens is closed, and the whole automation tree with it.
  internal sealed class PortableApp : IDisposable
  {
    private static readonly TimeSpan WindowTimeout = TimeSpan.FromSeconds(30);
    private readonly UIA3Automation objAutomation = new UIA3Automation();
    private Application objApp;

    public PortableApp()
    {
      this.Root = Path.Combine(Path.GetTempPath(), "ID3-TagIT.Smoke-" + Guid.NewGuid().ToString("N"));
      this.AppFolder = Path.Combine(this.Root, "app");
      this.MusicFolder = Path.Combine(this.Root, "music");
      Directory.CreateDirectory(this.MusicFolder);
      CopyFolder(BuildFolder(), this.AppFolder);
      File.WriteAllText(Path.Combine(this.AppFolder, "Portable.txt"), "Smoke test copy");
    }

    public string Root { get; private set; }
    public string AppFolder { get; private set; }
    public string MusicFolder { get; private set; }
    public Window Main { get; private set; }

    public string SettingsFile
    {
      get { return Path.Combine(this.AppFolder, @"Settings\Settings.xml"); }
    }

    public string Music(string name)
    {
      return Path.Combine(this.MusicFolder, name);
    }

    private static string BuildFolder()
    {
      string folder = Environment.GetEnvironmentVariable("ID3TAGIT_SMOKE_APP");

      if (string.IsNullOrEmpty(folder))
      {
        // bin\<config>\ of this project -> repo root -> bin\Release
        string repo = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(PortableApp).Assembly.Location), @"..\..\.."));
        folder = Path.Combine(repo, @"bin\Release");
      }

      Assert.True(File.Exists(Path.Combine(folder, "ID3-TagIT.exe")), "No ID3-TagIT.exe in " + folder);
      return folder;
    }

    private static void CopyFolder(string source, string target)
    {
      Directory.CreateDirectory(target);

      foreach (string file in Directory.GetFiles(source))
        File.Copy(file, Path.Combine(target, Path.GetFileName(file)));

      foreach (string folder in Directory.GetDirectories(source))
        CopyFolder(folder, Path.Combine(target, Path.GetFileName(folder)));
    }

    // Starts the app on the music folder and waits until its file list shows `files` entries.
    public void Launch(int files)
    {
      ProcessStartInfo info = new ProcessStartInfo(Path.Combine(this.AppFolder, "ID3-TagIT.exe"), "\"/P=" + this.MusicFolder + "\\\"");
      info.WorkingDirectory = this.AppFolder;
      this.objApp = Application.Launch(info);
      // Windows come and go during startup (the splash screen), so ignore errors while waiting.
      this.Main = Retry.WhileNull(() => this.objApp.GetAllTopLevelWindows(this.objAutomation).FirstOrDefault(w => w.AutomationId == "frmMain" && !w.IsOffscreen), WindowTimeout, TimeSpan.FromMilliseconds(250), true, true).Result;
      Retry.WhileFalse(() => this.Files().Length == files, WindowTimeout, TimeSpan.FromMilliseconds(250), true, true);
      this.AssertNoMessageBox();
    }

    // Closes the app the way a user does and waits for it to exit (it saves settings on exit).
    public void Close()
    {
      if (this.objApp == null)
        return;

      this.Main.Close();
      Retry.WhileFalse(() => this.objApp.HasExited, WindowTimeout, TimeSpan.FromMilliseconds(250), true);
      this.objApp.Dispose();
      this.objApp = null;
      this.Main = null;
    }

    // Launches once and closes, so the portable Settings folder exists for EditSetting.
    public void CreateSettings()
    {
      this.Launch(Directory.GetFiles(this.MusicFolder, "*.mp3").Length);
      this.Close();
    }

    // Changes Settings.xml while the app isn't running; adds the setting if it is missing.
    public void EditSetting(string name, object value)
    {
      DataSet set = new DataSet();
      set.ReadXml(this.SettingsFile);
      DataTable settings = set.Tables[0];

      if (!settings.Columns.Contains(name))
        settings.Columns.Add(name, value.GetType());

      settings.Rows[0][name] = value;
      set.WriteXml(this.SettingsFile, XmlWriteMode.WriteSchema);
    }

    public string ReadSetting(string name)
    {
      DataSet set = new DataSet();
      set.ReadXml(this.SettingsFile);
      return set.Tables[0].Columns.Contains(name) ? Convert.ToString(set.Tables[0].Rows[0][name]) : null;
    }

    public AutomationElement FileList
    {
      get { return this.Main.FindFirstDescendant(cf => cf.ByAutomationId("MP3View")); }
    }

    public AutomationElement[] Files()
    {
      AutomationElement list = this.FileList;
      return (list == null) ? new AutomationElement[0] : list.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));
    }

    public void SelectFile(string name)
    {
      AutomationElement item = this.Files().First(f => f.Name == name);
      item.Patterns.SelectionItem.Pattern.Select();
    }

    // Presses a shortcut in the main window, e.g. Shortcut(VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL).
    public void Shortcut(VirtualKeyShort key, params VirtualKeyShort[] modifiers)
    {
      this.Main.Focus();
      Keyboard.TypeSimultaneously(modifiers.Concat(new[] { key }).ToArray());
    }

    public Window WaitForDialog(string automationId)
    {
      return Retry.WhileNull(() => this.Main.ModalWindows.FirstOrDefault(w => w.AutomationId == automationId), WindowTimeout, TimeSpan.FromMilliseconds(250), true, false, "No " + automationId).Result;
    }

    public void WaitForNoDialog()
    {
      Retry.WhileFalse(() => this.Main.ModalWindows.Length == 0, WindowTimeout, TimeSpan.FromMilliseconds(250), true);
    }

    public Window OpenDialog(string automationId, VirtualKeyShort key, params VirtualKeyShort[] modifiers)
    {
      this.Shortcut(key, modifiers);
      return this.WaitForDialog(automationId);
    }

    // Presses OK (or another closing button) and waits for the dialog to go away.
    public void CloseDialog(Window dialog, string button = "btnOK")
    {
      dialog.FindFirstDescendant(cf => cf.ByAutomationId(button)).AsButton().Invoke();
      this.WaitForNoDialog();
    }

    // Ctrl+S; the progress dialog closes itself when all files are written.
    public void SaveAll()
    {
      this.Shortcut(VirtualKeyShort.KEY_S, VirtualKeyShort.CONTROL);
      Thread.Sleep(500);
      this.WaitForNoDialog();
      this.AssertNoMessageBox();
    }

    // A standard Windows message box (class #32770) means an error the user would see.
    public void AssertNoMessageBox()
    {
      Window box = this.objApp.GetAllTopLevelWindows(this.objAutomation).Concat(this.Main.ModalWindows).FirstOrDefault(w => w.ClassName == "#32770");

      if (box != null)
        Assert.True(false, "Message box: " + box.Title + ": " + string.Join(" ", box.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)).Select(t => t.Name)));
    }

    public void Dispose()
    {
      try
      {
        if (this.objApp != null && !this.objApp.HasExited)
        {
          this.objApp.Kill();
          Retry.WhileFalse(() => this.objApp.HasExited, TimeSpan.FromSeconds(10));
        }
      }
      catch (InvalidOperationException)
      {
      }

      this.objAutomation.Dispose();

      // Never throw from here: it would hide the test's own failure.
      try
      {
        foreach (string file in Directory.GetFiles(this.Root, "*", SearchOption.AllDirectories))
          File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(this.Root, true);
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }
  }
}
