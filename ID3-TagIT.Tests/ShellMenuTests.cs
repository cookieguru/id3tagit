using System;
using Microsoft.Win32;
using Xunit;

namespace ID3_TagIT.Tests
{
  // Runs against throwaway keys standing in for HKLM and HKCU Software\Classes.
  public class ShellMenuTests
  {
    private const string TestKey = @"Software\ID3-TagIT.Tests";
    private const string Exe = @"C:\Program Files (x86)\ID3-TagIT 3\ID3-TagIT.exe";

    private static void Test(Action<ShellMenu, RegistryKey, RegistryKey> test)
    {
      try
      {
        using (RegistryKey machine = Registry.CurrentUser.CreateSubKey(TestKey + @"\Machine"))
        using (RegistryKey user = Registry.CurrentUser.CreateSubKey(TestKey + @"\User"))
          test(new ShellMenu(machine, user, Exe), machine, user);
      }
      finally
      {
        Registry.CurrentUser.DeleteSubKeyTree(TestKey, false);
      }
    }

    private static string CommandIn(RegistryKey root)
    {
      using (RegistryKey key = root.OpenSubKey(ShellMenu.Key + @"\command"))
        return (key == null) ? null : key.GetValue(null) as string;
    }

    private static bool Hidden(RegistryKey user)
    {
      using (RegistryKey key = user.OpenSubKey(ShellMenu.Key))
        return key != null && key.GetValue("LegacyDisable") != null;
    }

    [Fact]
    public void OnForAllUsersShowsForMe()
    {
      Test(delegate (ShellMenu menu, RegistryKey machine, RegistryKey user)
      {
        menu.SetForAllUsers(true);

        Assert.True(menu.ForAllUsers);
        Assert.True(menu.ForMe);
        Assert.Equal(menu.Command, CommandIn(machine));
      });
    }

    [Fact]
    public void HidingForMeLeavesAllUsersAlone()
    {
      Test(delegate (ShellMenu menu, RegistryKey machine, RegistryKey user)
      {
        menu.SetForAllUsers(true);
        menu.SetForMe(false);

        Assert.False(menu.ForMe);
        Assert.True(menu.ForAllUsers);
        Assert.True(Hidden(user));
      });
    }

    [Fact]
    public void ShowingForMeAgainRemovesTheOverride()
    {
      Test(delegate (ShellMenu menu, RegistryKey machine, RegistryKey user)
      {
        menu.SetForAllUsers(true);
        menu.SetForMe(false);
        menu.SetForMe(true);

        Assert.True(menu.ForMe);
        Assert.Null(user.OpenSubKey(ShellMenu.Key));
      });
    }

    [Fact]
    public void PerUserOnAndOff()
    {
      Test(delegate (ShellMenu menu, RegistryKey machine, RegistryKey user)
      {
        Assert.False(menu.ForMe);

        menu.SetForMe(true);
        Assert.True(menu.ForMe);
        Assert.Equal(menu.Command, CommandIn(user));

        menu.SetForMe(false);
        Assert.False(menu.ForMe);
        Assert.Null(user.OpenSubKey(ShellMenu.Key));
      });
    }

    [Fact]
    public void AllUsersOff()
    {
      Test(delegate (ShellMenu menu, RegistryKey machine, RegistryKey user)
      {
        menu.SetForAllUsers(true);
        menu.SetForAllUsers(false);

        Assert.False(menu.ForAllUsers);
        Assert.False(menu.ForMe);
        Assert.Null(machine.OpenSubKey(ShellMenu.Key));
      });
    }

    [Fact]
    public void CommandQuotesThePath()
    {
      ShellMenu menu = new ShellMenu(null, null, @"C:\A & B%\ID3-TagIT.exe");

      Assert.Equal("\"C:\\A & B%\\ID3-TagIT.exe\" \"/P=%1\"", menu.Command);
    }

    [Fact]
    public void ForMeRepairsAStalePath()
    {
      Test(delegate (ShellMenu menu, RegistryKey machine, RegistryKey user)
      {
        using (RegistryKey command = machine.CreateSubKey(ShellMenu.Key + @"\command"))
          command.SetValue(null, "\"D:\\Old\\ID3-TagIT.exe\" \"/P=%1\"");

        menu.SetForMe(true);

        Assert.True(menu.ForMe);
        Assert.Equal(menu.Command, CommandIn(user));
      });
    }
  }
}
