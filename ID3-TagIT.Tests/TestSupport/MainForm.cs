using System;
using System.Collections;
using System.Reflection;
using System.Windows.Forms;

namespace ID3_TagIT.Tests
{
  // A constructed but never shown frmMain, with access to its private members. Not showing it
  // keeps its Load handler (folder tree, settings, file loading) from running.
  internal sealed class MainForm : IDisposable
  {
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public MainForm()
    {
      TestApp.EnsureInitialized();
      Declarations.MP3s.Clear();
      // The app reports background failures with Application.OnThreadException, which would
      // otherwise show a dialog.
      Application.ThreadException += this.OnThreadException;
      this.Form = new frmMain();
    }

    private Exception objError;

    private void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
      this.objError = e.Exception;
    }

    public frmMain Form { get; private set; }

    public ListView List
    {
      get { return this.Get<ListView>("MP3View"); }
    }

    public T Get<T>(string field)
    {
      return (T)typeof(frmMain).GetField(field, Members).GetValue(this.Form);
    }

    public object Call(string method, params object[] args)
    {
      object result;

      try
      {
        result = typeof(frmMain).GetMethod(method, Members).Invoke(this.Form, args);
      }
      catch (TargetInvocationException exception)
      {
        throw exception.InnerException;
      }

      if (this.objError != null)
        throw new Exception(method + " reported an error", this.objError);

      return result;
    }

    // Adds list rows for the given MP3s, as loading a folder would.
    public void ShowFiles(IEnumerable mp3s)
    {
      foreach (MP3 mp3 in mp3s)
        this.List.Items.Add(new ListViewItem(mp3.FI.Name) { Tag = mp3 });
    }

    public void Dispose()
    {
      this.Call("StopAudioCheckSum");
      this.Form.Dispose();
      Application.ThreadException -= this.OnThreadException;
      Declarations.MP3s.Clear();
    }
  }
}
