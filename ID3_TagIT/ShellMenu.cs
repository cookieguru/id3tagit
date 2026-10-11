using Microsoft.Win32;

namespace ID3_TagIT
{
  // The "ID3-TagIT" entry in the right-click menu of folders. An all-users entry lives under
  // HKLM (written by the installer, or by an elevated run of the app); each user can add their
  // own under HKCU, or hide the all-users one for themselves with a LegacyDisable value.
  internal sealed class ShellMenu
  {
    public const string Key = @"Directory\shell\ID3-TagIT";

    private readonly RegistryKey objMachine;
    private readonly RegistryKey objUser;
    private readonly string vstrExePath;

    // machineClasses and userClasses are the HKLM and HKCU Software\Classes keys; tests pass
    // throwaway keys.
    public ShellMenu(RegistryKey machineClasses, RegistryKey userClasses, string exePath)
    {
      this.objMachine = machineClasses;
      this.objUser = userClasses;
      this.vstrExePath = exePath;
    }

    // HKLM is opened for writing only when elevated; without admin it can only be read.
    public static ShellMenu ForThisExe()
    {
      return new ShellMenu(Registry.LocalMachine.OpenSubKey(@"Software\Classes", Id3TagIT_Main.IsElevated()), Registry.CurrentUser.CreateSubKey(@"Software\Classes"), System.Windows.Forms.Application.ExecutablePath);
    }

    public string Command
    {
      get { return "\"" + this.vstrExePath + "\" \"/P=%1\""; }
    }

    public bool ForAllUsers
    {
      get { return CommandIn(this.objMachine) != null; }
    }

    public bool ForMe
    {
      get
      {
        if (CommandIn(this.objUser) != null)
          return true;

        return this.ForAllUsers && !this.HiddenForMe;
      }
    }

    private bool HiddenForMe
    {
      get
      {
        using (RegistryKey key = this.objUser.OpenSubKey(Key))
          return key != null && key.GetValue("LegacyDisable") != null;
      }
    }

    // Shows or hides the entry for the current user only; never needs admin. Showing it also
    // repairs an all-users entry that points at another exe.
    public void SetForMe(bool show)
    {
      this.objUser.DeleteSubKeyTree(Key, false);

      if (show)
      {
        if (CommandIn(this.objMachine) != this.Command)
          WriteCommand(this.objUser);
      }
      else if (this.ForAllUsers)
      {
        using (RegistryKey key = this.objUser.CreateSubKey(Key))
          key.SetValue("LegacyDisable", "");
      }
    }

    // Needs admin outside tests.
    public void SetForAllUsers(bool show)
    {
      if (show)
        WriteCommand(this.objMachine);
      else
        this.objMachine.DeleteSubKeyTree(Key, false);
    }

    private void WriteCommand(RegistryKey root)
    {
      using (RegistryKey key = root.CreateSubKey(Key + @"\command"))
        key.SetValue(null, this.Command);
    }

    private static string CommandIn(RegistryKey root)
    {
      if (root == null)
        return null;

      using (RegistryKey key = root.OpenSubKey(Key + @"\command"))
        return (key == null) ? null : key.GetValue(null) as string;
    }
  }
}
