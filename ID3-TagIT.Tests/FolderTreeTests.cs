using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class FolderTreeTests
  {
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    // Restoring the start folder fills in folders outside an expand, when the progress bar is
    // already full. That used to step it past Maximum and throw (hidden by a catch).
    [Fact]
    public void FillingAFolderOutsideAnExpandDoesNotOverfillTheProgressBar()
    {
      Sta.Run(delegate ()
      {
        using (TempFolder folder = new TempFolder())
        using (FolderTreeView tree = new FolderTreeView())
        {
          folder.Folder(@"root\sub");
          TreeNode node = new TreeNode("root") { Tag = Path.Combine(folder.Path, "root") };
          ProgressBar progress = (ProgressBar)typeof(FolderTreeView).GetField("prgFileTreeView", Private).GetValue(tree);
          progress.Maximum = 1;
          progress.Value = 1;
          Exception thrown = null;
          EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> watch = delegate (object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
          {
            if (e.Exception is ArgumentOutOfRangeException)
              thrown = e.Exception;
          };

          AppDomain.CurrentDomain.FirstChanceException += watch;

          try
          {
            typeof(FolderTreeView).GetMethod("_ExpandBranch", Private).Invoke(tree, new object[] { node });
          }
          finally
          {
            AppDomain.CurrentDomain.FirstChanceException -= watch;
          }

          Assert.Null(thrown);
          Assert.Equal(1, progress.Value);
          Assert.Equal("sub", node.Nodes[0].Text);
        }
      });
    }
  }
}
