using Xunit;

namespace ID3_TagIT.Tests
{
  public class CommandLineTests
  {
    private static CommandLine Resolve(string commandLine)
    {
      CommandLine line = new CommandLine();
      line.Resolve(commandLine);
      return line;
    }

    [Fact]
    public void ParsesShellMenuOn()
    {
      Assert.Equal(true, Resolve("/shellmenu=allusers:on").ShellMenuAllUsers);
    }

    [Fact]
    public void ParsesShellMenuOff()
    {
      Assert.Equal(false, Resolve("/SHELLMENU=AllUsers:Off").ShellMenuAllUsers);
    }

    [Fact]
    public void NoShellMenuArgumentIsNull()
    {
      CommandLine line = Resolve("\"/P=C:\\Music\\\"");

      Assert.Null(line.ShellMenuAllUsers);
      Assert.Equal(@"C:\Music\", line.Path);
    }
  }
}
