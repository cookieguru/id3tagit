using System.Windows.Forms;

namespace ID3_TagIT
{
  partial class frmShortcuts
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
        components.Dispose();

      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
      this.tabShortcuts = new System.Windows.Forms.TabControl();
      this.tabNormalKeys = new System.Windows.Forms.TabPage();
      this.lvwNormalKeys = new System.Windows.Forms.ListView();
      this.colNormalKey = new System.Windows.Forms.ColumnHeader();
      this.colNormalAction = new System.Windows.Forms.ColumnHeader();
      this.tabFunctionKeys = new System.Windows.Forms.TabPage();
      this.lvwFunctionKeys = new System.Windows.Forms.ListView();
      this.colFunctionKey = new System.Windows.Forms.ColumnHeader();
      this.colFunctionNormal = new System.Windows.Forms.ColumnHeader();
      this.colFunctionShift = new System.Windows.Forms.ColumnHeader();
      this.colFunctionCtrl = new System.Windows.Forms.ColumnHeader();
      this.colFunctionAlt = new System.Windows.Forms.ColumnHeader();
      this.pnlButtons = new System.Windows.Forms.Panel();
      this.btnClose = new System.Windows.Forms.Button();
      this.tabShortcuts.SuspendLayout();
      this.tabNormalKeys.SuspendLayout();
      this.tabFunctionKeys.SuspendLayout();
      this.pnlButtons.SuspendLayout();
      this.SuspendLayout();
      //
      // tabShortcuts
      //
      this.tabShortcuts.Controls.Add(this.tabNormalKeys);
      this.tabShortcuts.Controls.Add(this.tabFunctionKeys);
      this.tabShortcuts.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabShortcuts.Location = new System.Drawing.Point(8, 8);
      this.tabShortcuts.Name = "tabShortcuts";
      this.tabShortcuts.SelectedIndex = 0;
      this.tabShortcuts.Size = new System.Drawing.Size(728, 398);
      this.tabShortcuts.TabIndex = 0;
      //
      // tabNormalKeys
      //
      this.tabNormalKeys.Controls.Add(this.lvwNormalKeys);
      this.tabNormalKeys.Location = new System.Drawing.Point(4, 22);
      this.tabNormalKeys.Name = "tabNormalKeys";
      this.tabNormalKeys.Padding = new System.Windows.Forms.Padding(6);
      this.tabNormalKeys.Size = new System.Drawing.Size(720, 372);
      this.tabNormalKeys.TabIndex = 0;
      this.tabNormalKeys.Text = "Normal keys";
      //
      // lvwNormalKeys
      //
      this.lvwNormalKeys.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colNormalKey,
            this.colNormalAction});
      this.lvwNormalKeys.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lvwNormalKeys.FullRowSelect = true;
      this.lvwNormalKeys.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
      this.lvwNormalKeys.HideSelection = false;
      this.lvwNormalKeys.Location = new System.Drawing.Point(6, 6);
      this.lvwNormalKeys.MultiSelect = false;
      this.lvwNormalKeys.Name = "lvwNormalKeys";
      this.lvwNormalKeys.Size = new System.Drawing.Size(708, 360);
      this.lvwNormalKeys.TabIndex = 0;
      this.lvwNormalKeys.UseCompatibleStateImageBehavior = false;
      this.lvwNormalKeys.View = System.Windows.Forms.View.Details;
      //
      // colNormalKey
      //
      this.colNormalKey.Text = "Shortcut";
      this.colNormalKey.Width = 110;
      //
      // colNormalAction
      //
      this.colNormalAction.Text = "Action";
      this.colNormalAction.Width = 560;
      //
      // tabFunctionKeys
      //
      this.tabFunctionKeys.Controls.Add(this.lvwFunctionKeys);
      this.tabFunctionKeys.Location = new System.Drawing.Point(4, 22);
      this.tabFunctionKeys.Name = "tabFunctionKeys";
      this.tabFunctionKeys.Padding = new System.Windows.Forms.Padding(6);
      this.tabFunctionKeys.Size = new System.Drawing.Size(720, 372);
      this.tabFunctionKeys.TabIndex = 1;
      this.tabFunctionKeys.Text = "F-Keys";
      //
      // lvwFunctionKeys
      //
      this.lvwFunctionKeys.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colFunctionKey,
            this.colFunctionNormal,
            this.colFunctionShift,
            this.colFunctionCtrl,
            this.colFunctionAlt});
      this.lvwFunctionKeys.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lvwFunctionKeys.FullRowSelect = true;
      this.lvwFunctionKeys.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
      this.lvwFunctionKeys.HideSelection = false;
      this.lvwFunctionKeys.Location = new System.Drawing.Point(6, 6);
      this.lvwFunctionKeys.MultiSelect = false;
      this.lvwFunctionKeys.Name = "lvwFunctionKeys";
      this.lvwFunctionKeys.Size = new System.Drawing.Size(708, 360);
      this.lvwFunctionKeys.TabIndex = 0;
      this.lvwFunctionKeys.UseCompatibleStateImageBehavior = false;
      this.lvwFunctionKeys.View = System.Windows.Forms.View.Details;
      //
      // colFunctionKey
      //
      this.colFunctionKey.Text = "Key";
      this.colFunctionKey.Width = 40;
      //
      // colFunctionNormal
      //
      this.colFunctionNormal.Text = "Normal";
      this.colFunctionNormal.Width = 150;
      //
      // colFunctionShift
      //
      this.colFunctionShift.Text = "Shift+";
      this.colFunctionShift.Width = 150;
      //
      // colFunctionCtrl
      //
      this.colFunctionCtrl.Text = "Ctrl+";
      this.colFunctionCtrl.Width = 150;
      //
      // colFunctionAlt
      //
      this.colFunctionAlt.Text = "Alt+";
      this.colFunctionAlt.Width = 150;
      //
      // pnlButtons
      //
      this.pnlButtons.Controls.Add(this.btnClose);
      this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.pnlButtons.Location = new System.Drawing.Point(8, 406);
      this.pnlButtons.Name = "pnlButtons";
      this.pnlButtons.Size = new System.Drawing.Size(728, 36);
      this.pnlButtons.TabIndex = 1;
      //
      // btnClose
      //
      this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.btnClose.Location = new System.Drawing.Point(640, 8);
      this.btnClose.Name = "btnClose";
      this.btnClose.Size = new System.Drawing.Size(88, 24);
      this.btnClose.TabIndex = 0;
      this.btnClose.Text = "Close";
      //
      // frmShortcuts
      //
      this.AcceptButton = this.btnClose;
      this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
      this.CancelButton = this.btnClose;
      this.ClientSize = new System.Drawing.Size(744, 450);
      this.Controls.Add(this.tabShortcuts);
      this.Controls.Add(this.pnlButtons);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(500, 300);
      this.Name = "frmShortcuts";
      this.Padding = new System.Windows.Forms.Padding(8);
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Keyboard Shortcuts";
      this.tabShortcuts.ResumeLayout(false);
      this.tabNormalKeys.ResumeLayout(false);
      this.tabFunctionKeys.ResumeLayout(false);
      this.pnlButtons.ResumeLayout(false);
      this.ResumeLayout(false);
    }

    #endregion

    private TabControl tabShortcuts;
    private TabPage tabNormalKeys;
    private ListView lvwNormalKeys;
    private ColumnHeader colNormalKey;
    private ColumnHeader colNormalAction;
    private TabPage tabFunctionKeys;
    private ListView lvwFunctionKeys;
    private ColumnHeader colFunctionKey;
    private ColumnHeader colFunctionNormal;
    private ColumnHeader colFunctionShift;
    private ColumnHeader colFunctionCtrl;
    private ColumnHeader colFunctionAlt;
    private Panel pnlButtons;
    private Button btnClose;
  }
}
