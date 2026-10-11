using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ID3_TagIT
{
  [StandardModule]
  internal sealed class Id3TagIT_Main
  {
    internal static bool IS_ALPHA = false;
    internal static bool IS_BETA = true;

    public static MemoryStream objDebugStream = new MemoryStream();

    // True when running with administrator rights. Registering the Explorer context menu entry
    // writes under HKEY_CLASSES_ROOT, which fails with a SecurityException without them.
    internal static bool IsElevated()
    {
      using (System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent())
        return new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    public static bool CheckFilename(string vstrName, bool vbooCompletePath = false)
    {
      if (vbooCompletePath)
        if (((((((vstrName.IndexOf("<") >= 0) |
                 (vstrName.IndexOf(">") >= 0)) |
                 (vstrName.IndexOf('"') >= 0)) |
                 (vstrName.IndexOf("/") >= 0)) |
                 (vstrName.IndexOf("*") >= 0)) |
                 (vstrName.IndexOf("|") >= 0)) |
                 (vstrName.IndexOf("?") >= 0))
          return false;
        else if ((((((((vstrName.IndexOf(@"\") >= 0) |
                       (vstrName.IndexOf("<") >= 0)) |
                       (vstrName.IndexOf(">") >= 0)) |
                       (vstrName.IndexOf('"') >= 0)) |
                       (vstrName.IndexOf("/") >= 0)) |
                       (vstrName.IndexOf("*") >= 0)) |
                       (vstrName.IndexOf("|") >= 0)) |
                       (vstrName.IndexOf("?") >= 0))
          return false;

      return true;
    }

    public static string CreateTempFile(byte[] abytData, [Optional, DefaultParameterValue("")] string vstrExt)
    {
      string tempFileName = AppEnvironment.NewTempFile(vstrExt);

      FileStream stream = new FileStream(tempFileName, FileMode.Open, FileAccess.ReadWrite);

      stream.Write(abytData, 0, abytData.Length);
      stream.Flush();
      stream.Close();
      Application.DoEvents();

      return tempFileName;
    }

    public static Icon GetAppIcon(string vstrPath, bool vbooSmallIcon)
    {
      Declarations.SHGFI shgfi;
      Declarations.SHFILEINFO structure = new Declarations.SHFILEINFO(true);
      int cbFileInfo = Marshal.SizeOf(structure);

      if (vbooSmallIcon)
        shgfi = Declarations.SHGFI.ATTRIBUTES | Declarations.SHGFI.ICON | Declarations.SHGFI.ADDOVERLAYS | Declarations.SHGFI.SMALLICON;
      else
        shgfi = Declarations.SHGFI.ATTRIBUTES | Declarations.SHGFI.ICON | Declarations.SHGFI.ADDOVERLAYS;

      Declarations.SHGetFileInfo(ref vstrPath, 0x100, ref structure, cbFileInfo, shgfi);
      Icon icon2 = (Icon)Icon.FromHandle(structure.hIcon).Clone();
      Declarations.DestroyIcon(structure.hIcon);

      return icon2;
    }

    public static int[] GetColumnOrder(ListView ListView)
    {
      int msg = 0x103b;
      int[] numArray = new int[0];
      int[] numArray2 = new int[(ListView.Columns.Count - 1) + 1];

      if (!Declarations.ListViewColumnOrder(ListView.Handle.ToInt32(), msg, numArray2.Length, ref numArray2[0]).Equals(0))
        return numArray2;

      return numArray;
    }

    public static string GetRelativePath(string vstrBaseDir, string vstrFilePath)
    {
      int num;
      string str2 = "";
      int index = 0;

      if (StringType.StrCmp(vstrBaseDir.Substring(0, 2), vstrFilePath.Substring(0, 2), false) != 0)
        return vstrFilePath;

      string[] strArray = vstrBaseDir.Split(new char[] { '\\' });
      string[] strArray2 = vstrFilePath.Split(new char[] { '\\' });

      if (strArray.Length <= strArray2.Length)
      {
        int upperBound = strArray.GetUpperBound(0);

        for (num = 0; num <= upperBound; num++)
        {
          if (StringType.StrCmp(strArray[num], strArray2[num], false) != 0)
            break;

          index = num;
        }
      }
      else
      {
        int num4 = strArray2.GetUpperBound(0);

        for (num = 0; num <= num4; num++)
        {
          if (StringType.StrCmp(strArray[num], strArray2[num], false) != 0)
            break;
          index = num;
        }
      }

      if (index == strArray.GetUpperBound(0))
      {
        index++;

        while (index <= strArray2.GetUpperBound(0))
        {
          str2 = str2 + strArray2[index] + @"\";
          index++;
        }

        return str2.Substring(0, str2.Length - 1);
      }

      int num3 = index + 1;

      while (index < strArray.GetUpperBound(0))
      {
        str2 = str2 + @"..\";
        index++;
      }

      while (num3 <= strArray2.GetUpperBound(0))
      {
        str2 = str2 + strArray2[num3] + @"\";
        num3++;
      }

      return str2.Substring(0, str2.Length - 1);
    }

    [STAThread]
    public static void Main()
    {
      Debug.Listeners.Add(new TextWriterTraceListener(objDebugStream));
      Debug.AutoFlush = true;

      AppEnvironment.Initialize(Application.StartupPath, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.GetTempPath());
      Declarations.vstrUserAppData = AppEnvironment.SettingsFolder;
      Declarations.Declaration();
      CommandLine line = new CommandLine();

      Version previous = null;
      bool settingsReadOnly = false;

      try
      {
        Version version = new Version(Application.ProductVersion);
        previous = SettingsMigration.Run(Declarations.vstrUserAppData, new Version(version.Major, version.Minor, version.Build));
      }
      // Typically a portable copy on read-only media: run on the built-in defaults.
      catch (IOException)
      {
        settingsReadOnly = true;
      }
      catch (UnauthorizedAccessException)
      {
        settingsReadOnly = true;
      }

      Declarations.objSettings.ReadOnly = settingsReadOnly;

      Declarations.objSettings.ReadSettings();

      if (previous != null)
      {
        DataColumn column;

        try
        {
          Declarations.objSettings.SettingsTable.Columns.Remove("PlaylistPath");
        }
        catch (Exception exception2)
        {
          ProjectData.SetProjectError(exception2);
          ProjectData.ClearProjectError();
        }

        try
        {
          Declarations.objSettings.SettingsTable.Columns.Remove("SearchPlaceholders");
        }
        catch (Exception exception3)
        {
          ProjectData.SetProjectError(exception3);
          ProjectData.ClearProjectError();
        }

        try
        {
          Declarations.objSettings.SettingsTable.Columns.Remove("FilterIndex");
        }
        catch (Exception exception4)
        {
          ProjectData.SetProjectError(exception4);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("FreeDBLastSearch", System.Type.GetType("System.String"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["FreeDBLastSearch"] = "";
        }
        catch (Exception exception5)
        {
          ProjectData.SetProjectError(exception5);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("FreeDBSearchOptions", System.Type.GetType("System.Byte"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["FreeDBSearchOptions"] = 0;
        }
        catch (Exception exception6)
        {
          ProjectData.SetProjectError(exception6);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("RebuildOFilename", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["RebuildOFilename"] = false;
        }
        catch (Exception exception7)
        {
          ProjectData.SetProjectError(exception7);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("SaveOFilename", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["SaveOFilename"] = false;
        }
        catch (Exception exception8)
        {
          ProjectData.SetProjectError(exception8);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("SearchRegExpression", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["SearchRegExpression"] = false;
        }
        catch (Exception exception9)
        {
          ProjectData.SetProjectError(exception9);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("ListExtInfo", System.Type.GetType("System.String"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["ListExtInfo"] = "<A> - <T>";
        }
        catch (Exception exception10)
        {
          ProjectData.SetProjectError(exception10);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("SplitSeparator", System.Type.GetType("System.String"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["SplitSeparator"] = " - ";
        }
        catch (Exception exception11)
        {
          ProjectData.SetProjectError(exception11);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("Language", System.Type.GetType("System.Byte"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["Language"] = 0;
        }
        catch (Exception exception12)
        {
          ProjectData.SetProjectError(exception12);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("OrganizeCopy", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["OrganizeCopy"] = false;
        }
        catch (Exception exception13)
        {
          ProjectData.SetProjectError(exception13);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("NavPanWidth", System.Type.GetType("System.Int32"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["NavPanWidth"] = 0xec;
        }
        catch (Exception exception14)
        {
          ProjectData.SetProjectError(exception14);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("SideBarWidth", System.Type.GetType("System.Int32"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["SideBarWidth"] = 0xd4;
        }
        catch (Exception exception15)
        {
          ProjectData.SetProjectError(exception15);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("ErrorHeight", System.Type.GetType("System.Int32"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["ErrorHeight"] = 120;
        }
        catch (Exception exception16)
        {
          ProjectData.SetProjectError(exception16);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("NavPanExpanded", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["NavPanExpanded"] = true;
        }
        catch (Exception exception17)
        {
          ProjectData.SetProjectError(exception17);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("SideBarExpanded", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["SideBarExpanded"] = true;
        }
        catch (Exception exception18)
        {
          ProjectData.SetProjectError(exception18);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("ErrorExpanded", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["ErrorExpanded"] = true;
        }
        catch (Exception exception19)
        {
          ProjectData.SetProjectError(exception19);
          ProjectData.ClearProjectError();
        }

        try
        {
          column = new DataColumn("AudioChecksumCalc", System.Type.GetType("System.Boolean"));
          Declarations.objSettings.SettingsTable.Columns.Add(column);
          Declarations.objSettings.SettingsTable.Rows[0]["AudioChecksumCalc"] = true;
        }
        catch (Exception exception20)
        {
          ProjectData.SetProjectError(exception20);
          ProjectData.ClearProjectError();
        }
      }

      Declarations.objResources.ReadResources();

      if (settingsReadOnly)
        MessageBox.Show(StringType.FromObject(Declarations.objResources.ResStrings["SettingsReadOnly"]).Replace("%P", Declarations.vstrUserAppData), "ID3-TagIT", MessageBoxButtons.OK, MessageBoxIcon.Warning);

      Declarations.objSettings.AddNewColumns();
      ThemeManager.Initialize();
      line.Resolve();

      if ((StringType.StrCmp(line.Path, "", false) != 0) & Directory.Exists(line.Path))
        Declarations.objSettings.CurrentPath = line.Path;

      if (StringType.StrCmp(line.DataFilePath, "", false) == 0)
      {
        frmMain mainForm = new frmMain();
        Application.Run(mainForm);
      }

      Application.Exit();
    }

    public static void RestoreFormSettings(ref Form Form)
    {
      int num2 = Declarations.objSettings.Forms.Rows.Count - 1;

      for (int i = 0; i <= num2; i++)
      {
        if (ObjectType.ObjTst(Form.Name, Declarations.objSettings.Forms.Rows[i]["Name"], false) == 0)
        {
          DataRow row = Declarations.objSettings.Forms.Rows[i];

          if (ObjectType.ObjTst(row["State"], "Normal", false) == 0)
          {
            Form.Top = IntegerType.FromObject(row["Top"]);
            Form.Left = IntegerType.FromObject(row["Left"]);

            if (Form is frmMain)
            {
              Form.Height = IntegerType.FromObject(row["Height"]);
              Form.Width = IntegerType.FromObject(row["Width"]);
            }
          }
          else if (ObjectType.ObjTst(row["State"], "Maximized", false) == 0)
            Form.WindowState = FormWindowState.Maximized;
          else
            Form.WindowState = FormWindowState.Minimized;

          break;
        }
      }
    }

    public static void SaveFormSettings(ref Form Form)
    {
      int num2 = Declarations.objSettings.Forms.Rows.Count - 1;

      for (int i = 0; i <= num2; i++)
      {
        if (ObjectType.ObjTst(Form.Name, Declarations.objSettings.Forms.Rows[i]["Name"], false) == 0)
        {
          DataRow row2 = Declarations.objSettings.Forms.Rows[i];

          row2["Name"] = Form.Name.ToString();
          row2["State"] = Form.WindowState.ToString();
          row2["Top"] = Form.Top;
          row2["Left"] = Form.Left;
          row2["Height"] = Form.Height;
          row2["Width"] = Form.Width;

          return;
        }
      }

      DataRow row = Declarations.objSettings.Forms.NewRow();

      row["Name"] = Form.Name.ToString();
      row["State"] = Form.WindowState.ToString();
      row["Top"] = Form.Top;
      row["Left"] = Form.Left;
      row["Height"] = Form.Height;
      row["Width"] = Form.Width;

      Declarations.objSettings.Forms.Rows.Add(row);
    }

    // Adds the files in alstFolders matching vstrSearchPattern to alstFile, including subfolders
    // when vbooIncludeSubDir is set. Touches no UI, so it can run on a background thread: canceled
    // is polled between folders, and folderScanned is called with each folder as it is scanned.
    public static void ScanForFiles(ArrayList alstFolders, string vstrSearchPattern, bool vbooIncludeSubDir, ref ArrayList alstFile, Func<bool> canceled, Action<string> folderScanned)
    {
      foreach (string str in alstFolders)
      {
        try
        {
          string path = str;

          if (path.EndsWith(":"))
            path = path + @"\";

          DirectoryInfo info = new DirectoryInfo(path);
          DirectoryInfo[] directories = info.GetDirectories();

          if (vbooIncludeSubDir)
          {
            foreach (DirectoryInfo info2 in directories)
            {
              if (canceled())
                return;

              ArrayList list = new ArrayList { info2.FullName };
              ScanForFiles(list, vstrSearchPattern, vbooIncludeSubDir, ref alstFile, canceled, folderScanned);
            }
          }

          folderScanned(info.FullName);

          foreach (FileInfo info3 in info.GetFiles(vstrSearchPattern))
            alstFile.Add(info3.FullName);

          continue;
        }
        catch (Exception exception1)
        {
          ProjectData.SetProjectError(exception1);
          ProjectData.ClearProjectError();
          continue;
        }
      }
    }

    public static void SetColumnOrder(ListView ListView, int[] SortOrder)
    {
      if ((SortOrder.Length >= 1) && (SortOrder.Length <= ListView.Columns.Count))
      {
        // https://autohotkey.com/board/topic/79966-help-how-to-change-listview-column-headers-in-fly/
        // https://forum.powerbasic.com/forum/user-to-user-discussions/powerbasic-for-windows/780526-unable-to-get-wh_getmessage-hook-to-work-globally
        const int LVM_SETCOLUMNORDERARRAY = 4154;

        if (!Declarations.ListViewColumnOrder(ListView.Handle.ToInt32(), LVM_SETCOLUMNORDERARRAY, SortOrder.Length, ref SortOrder[0]).Equals(0))
          ListView.Refresh();
      }
    }

    public static bool ShellFileOp(string vstrFilenames, string vstrDestFolder, Declarations.ShellOperation Operation, Declarations.ShellOperationFlags Flags, Form objForm)
    {
      var lpFileOp = new Declarations.SHFILEOPSTRUCT();

      vstrFilenames = vstrFilenames.TrimEnd(new char[] { '\0' });
      vstrDestFolder = vstrDestFolder.TrimEnd(new char[] { '\0' });
      lpFileOp.hWnd = objForm.Handle.ToInt32();
      lpFileOp.wFunc = (int)Operation;
      lpFileOp.pFrom = vstrFilenames + "\0\0";
      lpFileOp.pTo = vstrDestFolder + "\0\0";
      lpFileOp.fFlags = (short)Flags;
      Application.DoEvents();

      return (Declarations.SHFileOperation(ref lpFileOp) == 0);
    }
  }
}