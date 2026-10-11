using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  // The background work in frmMain: scanning, loading, saving and audio checksums.
  public class MainFormTests
  {
    private static List<string> MakeFiles(TempFolder folder, int count)
    {
      List<string> files = new List<string>();

      for (int i = 0; i < count; i++)
        files.Add(new Mp3Builder().WithAudio(2, i).WithV1("Title " + i, "Artist", "Album", "2000").Write(folder.File(i.ToString("D4") + ".mp3")));

      return files;
    }

    private static List<string> LoadedPaths()
    {
      return Declarations.MP3s.Cast<MP3>().Select(mp3 => mp3.FI.FullName).ToList();
    }

    // Presses Cancel on the progress dialog as soon as it is idle.
    private static void CancelProgressWhenShown()
    {
      EventHandler cancel = null;
      cancel = delegate
      {
        foreach (Form form in Application.OpenForms)
          if (form is frmProgress && form.Visible)
          {
            typeof(frmProgress).GetField("vbooCanceled", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, true);
            Application.Idle -= cancel;
          }
      };
      Application.Idle += cancel;
    }

    [Fact]
    public void ReadFilesKeepsTheGivenOrder()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          List<string> files = MakeFiles(folder, 200);
          files.Reverse();

          main.Call("ReadFiles", new ArrayList(files));

          Assert.Equal(files, LoadedPaths());
          Assert.Equal("Title 199", ((MP3)Declarations.MP3s[0]).V1TAG.Title);
        }
      });
    }

    [Fact]
    public void CancellingReadFilesKeepsWhatWasReadInOrder()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          List<string> files = MakeFiles(folder, 2000);
          CancelProgressWhenShown();

          main.Call("ReadFiles", new ArrayList(files));
          List<string> kept = LoadedPaths();

          Assert.True(kept.Count < files.Count, "nothing was cancelled");
          Assert.Equal(kept.OrderBy(path => path, StringComparer.Ordinal), kept);
          Assert.False(Application.OpenForms.Cast<Form>().Any(form => form is frmProgress), "the progress dialog is still open");
        }
      });
    }

    [Fact]
    public void ScanFilesFindsTheFolderTree()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          string root = folder.Folder("root");
          string sub = folder.Folder(@"root\sub");
          File.WriteAllText(Path.Combine(root, "b.mp3"), "");
          File.WriteAllText(Path.Combine(sub, "a.mp3"), "");
          TestApp.Settings.ScanSubDirs = true;
          ArrayList found = new ArrayList();

          try
          {
            main.Call("ScanFiles", new ArrayList { root }, found);
          }
          finally
          {
            TestApp.Settings.ScanSubDirs = false;
          }

          Assert.Equal(new[] { Path.Combine(sub, "a.mp3"), Path.Combine(root, "b.mp3") }, found.Cast<string>());
        }
      });
    }

    [Fact]
    public void SaveChangesWritesChangedFilesAndListsFailures()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          List<string> files = MakeFiles(folder, 5);
          File.SetAttributes(files[2], FileAttributes.ReadOnly);
          main.Call("ReadFiles", new ArrayList(files));
          main.ShowFiles(Declarations.MP3s);

          foreach (ListViewItem item in main.List.Items)
          {
            MP3 mp3 = (MP3)item.Tag;
            mp3.V1TAG.Title = "Saved " + mp3.FI.Name;
            mp3.Changed = true;
            item.BackColor = Color.Yellow;
          }

          main.Call("SaveChanges");

          ListView errors = main.Get<ListView>("ErrorMsg");
          Assert.Single(errors.Items);
          Assert.Equal(files[2], errors.Items[0].Text);

          for (int i = 0; i < files.Count; i++)
          {
            MP3 reread = new MP3(files[i]);
            reread.ExploreMP3();
            ListViewItem item = main.List.Items[i];

            if (i == 2)
            {
              Assert.Equal("Title 2", reread.V1TAG.Title);
              Assert.True(((MP3)item.Tag).Changed);
              Assert.Equal(Color.Yellow, item.BackColor);
            }
            else
            {
              Assert.Equal("Saved " + Path.GetFileName(files[i]), reread.V1TAG.Title);
              Assert.False(((MP3)item.Tag).Changed);
              Assert.NotEqual(Color.Yellow, item.BackColor);
            }
          }
        }
      });
    }

    [Fact]
    public void AudioChecksumsAreCalculatedInTheBackground()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          main.Call("ReadFiles", new ArrayList(MakeFiles(folder, 20)));
          main.ShowFiles(Declarations.MP3s);

          main.Call("AudioCheckSumCalculation");
          BackgroundWorker worker = main.Get<BackgroundWorker>("objChecksumWorker");
          DateTime timeout = DateTime.Now.AddSeconds(30);

          while (worker.IsBusy && DateTime.Now < timeout)
          {
            Application.DoEvents();
            Thread.Sleep(10);
          }

          Assert.False(worker.IsBusy);
          Assert.All(Declarations.MP3s.Cast<MP3>(), mp3 => Assert.NotEqual(0, mp3.AudioCheckSum));
        }
      });
    }

    [Fact]
    public void StoppingTheChecksumsReleasesTheFiles()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          List<string> files = new List<string>();

          for (int i = 0; i < 20; i++)
            files.Add(new Mp3Builder().WithAudio(3000, i).Write(folder.File(i + ".mp3")));

          main.Call("ReadFiles", new ArrayList(files));
          main.ShowFiles(Declarations.MP3s);
          main.Call("AudioCheckSumCalculation");
          main.Call("StopAudioCheckSum");

          foreach (string file in files)
            using (File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }
        }
      });
    }

    [Fact]
    public void HighlightedRowsAreRecoloredForTheTheme()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (MainForm main = new MainForm())
        {
          main.Call("ReadFiles", new ArrayList(MakeFiles(folder, 2)));
          main.ShowFiles(Declarations.MP3s);
          ((MP3)main.List.Items[0].Tag).Changed = true;
          ((MP3)main.List.Items[1].Tag).Doubled = true;
          Settings settings = TestApp.Settings;

          try
          {
            settings.Theme = ThemeManager.THEME_DARK;
            ThemeManager.Refresh();
            main.Call("RecolorHighlightedRows");
            Assert.Equal(settings.ColorChangedBackDark, main.List.Items[0].BackColor.ToArgb());
            Assert.Equal(settings.ColorDoubleBackDark, main.List.Items[1].BackColor.ToArgb());

            settings.Theme = ThemeManager.THEME_LIGHT;
            ThemeManager.Refresh();
            main.Call("RecolorHighlightedRows");
            Assert.Equal(settings.ColorChangedBack, main.List.Items[0].BackColor.ToArgb());
            Assert.Equal(settings.ColorDoubleBack, main.List.Items[1].BackColor.ToArgb());
          }
          finally
          {
            settings.Theme = ThemeManager.THEME_LIGHT;
            ThemeManager.Refresh();
          }
        }
      });
    }

    [Fact]
    public void ToolbarLayoutRoundTrips()
    {
      Sta.Run(delegate ()
      {
        using (MainForm main = new MainForm())
        {
          main.Call("RestoreToolbarLayout", "mnuExtended,tspTop,0,0;mnuBtns,tspLeft,0,0");

          Assert.Same(main.Get<ToolStripPanel>("tspLeft"), main.Get<MenuStrip>("mnuBtns").Parent);
          Assert.Same(main.Get<ToolStripPanel>("tspTop"), main.Get<MenuStrip>("mnuExtended").Parent);

          string layout = (string)main.Call("GetToolbarLayout");
          Assert.Contains("mnuBtns,tspLeft,", layout);
          Assert.Contains("mnuExtended,tspTop,", layout);
        }
      });
    }

    [Fact]
    public void AnUnreadableToolbarLayoutKeepsTheDefault()
    {
      Sta.Run(delegate ()
      {
        using (MainForm main = new MainForm())
        {
          main.Call("RestoreToolbarLayout", "nonsense;mnuBtns,nowhere,x,y");

          Assert.Same(main.Get<ToolStripPanel>("tspTop"), main.Get<MenuStrip>("mnuBtns").Parent);
          Assert.Same(main.Get<ToolStripPanel>("tspTop"), main.Get<MenuStrip>("mnuExtended").Parent);
        }
      });
    }
  }
}
