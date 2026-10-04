using System.Windows.Forms;

namespace ID3_TagIT
{
  // Read-only list of the main window's keyboard shortcuts. This replaces the old
  // Help\shortcuts.htm page. The entries are fixed; keep them in step with the ShortcutKeys
  // set in frmMain.Designer.cs.
  public partial class frmShortcuts : Form
  {
    // "(left of ...)" refers to the menu item's position next to its Ver. 1 counterpart.
    private static readonly string[,] NormalKeys = new string[,]
    {
      { "Ctrl+A", "Select all files" },
      { "Ctrl+B", "Move files" },
      { "Ctrl+D", "Filename to TAG Ver. 2 (left of Filename to TAG Ver. 1)" },
      { "Ctrl+E", "Edit TAG Ver. 1" },
      { "Ctrl+F", "Filename to TAG Ver. 1" },
      { "Ctrl+G", "Group selection (at the top of list)" },
      { "Ctrl+I", "Invert selection" },
      { "Ctrl+K", "Remove selected files from list" },
      { "Ctrl+L", "Create File list/Playlist" },
      { "Ctrl+M", "Multiple Edit TAG Ver. 1" },
      { "Ctrl+N", "Multiple Edit TAG Ver. 2 (left of Multiple Edit TAG Ver. 1)" },
      { "Ctrl+O", "Organize files" },
      { "Ctrl+R", "TAG Ver. 2 to Filename (left of TAG Ver. 1 to Filename)" },
      { "Ctrl+S", "Save changes" },
      { "Ctrl+T", "TAG Ver. 1 to Filename" },
      { "Ctrl+W", "Edit TAG Ver. 2 (left of Edit TAG Ver. 1)" },
      { "Ctrl+Y", "Copy files" },
      { "Ctrl+Z", "Undo" },
      { "Ctrl+Shift+C", "Copy TAG Ver. 1 and TAG Ver. 2" },
      { "Ctrl+Shift+D", "Delete files" },
      { "Ctrl+Shift+F", "Remove empty folders" },
      { "Ctrl+Shift+V", "Paste TAG Ver. 1 and TAG Ver. 2" },
      { "Ctrl+Shift+Z", "Redo" }
    };

    // Key, then the action with no modifier, Shift, Ctrl and Alt. "---" marks a key the
    // original list showed as unused; an empty string has no action.
    private static readonly string[,] FunctionKeys = new string[,]
    {
      { "F1", "Keyboard shortcuts (this list)", "Swap Artist - Title (Ver. 1)", "View TAG Ver. 1", "" },
      { "F2", "Rename", "Swap Artist - Album (Ver. 1)", "View TAG Ver. 2", "" },
      { "F3", "Search and Replace", "Swap Title - Album (Ver. 1)", "", "" },
      { "F4", "Query FreeDB", "Swap Artist - Title (Ver. 2)", "", "Quit" },
      { "F5", "Refresh", "Swap Artist - Album (Ver. 2)", "", "Select by format" },
      { "F6", "Case Conversion", "Swap Title - Album (Ver. 2)", "", "Select changed files" },
      { "F7", "Transfer/Convert TAGs", "Split Artist into Artist - Title (Ver. 1)", "Find duplicated files", "Select duplicated files" },
      { "F8", "Add directory", "Split Artist into Artist - Title (Ver. 2)", "Find TAG - Filename mismatches", "Select mismatches" },
      { "F9", "Play", "Enumerate in filename", "", "Scan subdirectories" },
      { "F10", "---", "---", "", "Quick filename editing" },
      { "F11", "File properties", "Enumerate in TAG Ver. 1", "Remove TAG Ver. 1", "Sync. TAG Ver. 1 and Ver. 2 editing" },
      { "F12", "Preferences", "Enumerate in TAG Ver. 2", "Remove TAG Ver. 2", "Edit libraries" }
    };

    public frmShortcuts()
    {
      this.InitializeComponent();
      ThemeManager.Register(this);
    }

    protected override void OnLoad(System.EventArgs e)
    {
      // Columns can only be measured once the list views have handles.
      Fill(this.lvwNormalKeys, NormalKeys);
      Fill(this.lvwFunctionKeys, FunctionKeys);

      // Widen the dialog so the F-key columns fit without scrolling, within the screen.
      int needed = 0;

      foreach (ColumnHeader column in this.lvwFunctionKeys.Columns)
        needed += column.Width;

      int extra = needed + SystemInformation.VerticalScrollBarWidth + 8 - this.lvwFunctionKeys.ClientSize.Width;
      int available = Screen.FromControl(this).WorkingArea.Width - this.Width;

      if (extra > 0)
        this.Width += System.Math.Min(extra, System.Math.Max(available, 0));

      base.OnLoad(e);
    }

    private static void Fill(ListView list, string[,] rows)
    {
      list.BeginUpdate();

      for (int row = 0; row < rows.GetLength(0); row++)
      {
        ListViewItem item = new ListViewItem(rows[row, 0]);

        for (int column = 1; column < rows.GetLength(1); column++)
          item.SubItems.Add(rows[row, column]);

        list.Items.Add(item);
      }

      list.EndUpdate();

      // Fit each column to the wider of its header and its contents. (HeaderSize on the last
      // column stretches it to fill the list, so measure the header with TextRenderer.)
      foreach (ColumnHeader column in list.Columns)
      {
        column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        int headerWidth = TextRenderer.MeasureText(column.Text, list.Font).Width + 16;

        if (headerWidth > column.Width)
          column.Width = headerWidth;
      }
    }
  }
}
