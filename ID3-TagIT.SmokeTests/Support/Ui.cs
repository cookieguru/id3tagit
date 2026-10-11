using System;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // Reading and writing dialog controls by their WinForms names (their UI Automation IDs).
  internal static class Ui
  {
    public static AutomationElement Find(AutomationElement parent, string id)
    {
      AutomationElement element = parent.FindFirstDescendant(cf => cf.ByAutomationId(id));
      Assert.True(element != null, "No control " + id);
      return element;
    }

    // The editable part: a text box itself, or the edit inside a combo box or up-down control.
    private static AutomationElement Editable(AutomationElement element)
    {
      if (element.ControlType == ControlType.Edit || element.ControlType == ControlType.Document)
        return element;

      AutomationElement edit = element.FindFirstChild(cf => cf.ByControlType(ControlType.Edit));
      return edit ?? element;
    }

    // The editors' MaskedTextBoxes. Setting one through UI Automation leaves it unable to give up
    // the focus (its Validating handler cancels), so they are typed into, as a user does.
    private static readonly string[] MaskedBoxes = { "txtYear", "txtTrack", "txtTrack1", "txtTrack2", "txtPOS1", "txtPOS2", "txtBPM", "txtTLEN", "txtTORY" };

    public static void SetText(AutomationElement parent, string id, string value)
    {
      AutomationElement control = Find(parent, id);
      AutomationElement element = Editable(control);
      Assert.True(element.IsEnabled, id + " is disabled");

      if (MaskedBoxes.Contains(id))
      {
        element.Focus();
        Thread.Sleep(300);
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.HOME);
        Keyboard.Type(value);
        Thread.Sleep(100);
        Assert.Equal(value, GetText(parent, id));
        return;
      }

      if (element.Patterns.Value.IsSupported)
      {
        try
        {
          element.Patterns.Value.Pattern.SetValue(value);

          // A combo box's suggestion list may drop down and catch the next input.
          if (control.Patterns.ExpandCollapse.IsSupported && control.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value == ExpandCollapseState.Expanded)
            control.Patterns.ExpandCollapse.Pattern.Collapse();

          return;
        }
        catch (FlaUI.Core.Exceptions.ElementNotEnabledException)
        {
          // Some boxes (multi-line ones) refuse SetValue although they are enabled.
        }
      }

      // Type it instead, as a user would.
      // Clear it from the end without modifier keys: a Ctrl still settling would turn the first
      // characters typed into shortcuts.
      element.Focus();
      Thread.Sleep(300);
      Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.END);

      for (int i = GetText(parent, id).Length; i > 0; i--)
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.BACK);

      Keyboard.Type(value);
      Thread.Sleep(100);
      Assert.Equal(value, GetText(parent, id));
    }

    public static string GetText(AutomationElement parent, string id)
    {
      AutomationElement element = Editable(Find(parent, id));

      // Masked boxes report their empty positions as prompt characters.
      if (element.Patterns.Value.IsSupported)
        return MaskedBoxes.Contains(id) ? element.Patterns.Value.Pattern.Value.Value.Replace("_", "").TrimEnd(' ', '-', ':') : element.Patterns.Value.Pattern.Value.Value;

      return element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
    }

    // For buttons that open a modal dialog: invoking those through UI Automation blocks until the
    // dialog closes, so press them with the keyboard.
    public static void Press(AutomationElement parent, string id)
    {
      Find(parent, id).Focus();
      Thread.Sleep(200);
      Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.SPACE);
    }

    // Picks a file in a standard Open dialog shown by `owner`.
    public static void ChooseFile(Window owner, string path)
    {
      Window dialog = FlaUI.Core.Tools.Retry.WhileNull(() => owner.ModalWindows.FirstOrDefault(w => w.ClassName == "#32770"), TimeSpan.FromSeconds(15), null, true).Result;
      AutomationElement name = dialog.FindFirstDescendant(cf => cf.ByAutomationId("1148").And(cf.ByControlType(ControlType.Edit)));
      name.Patterns.Value.Pattern.SetValue(path);
      dialog.FindFirstChild(cf => cf.ByAutomationId("1")).AsButton().Invoke();
      FlaUI.Core.Tools.Retry.WhileFalse(() => owner.ModalWindows.Length == 0, TimeSpan.FromSeconds(15), null, true);
    }

    public static void Click(AutomationElement parent, string id)
    {
      Find(parent, id).AsButton().Invoke();
      Thread.Sleep(100);
    }

    public static void Check(AutomationElement parent, string id, bool value)
    {
      CheckBox box = Find(parent, id).AsCheckBox();

      if (box.IsChecked != value)
        box.Toggle();
    }

    public static string[] ListItems(AutomationElement parent, string id)
    {
      return Find(parent, id).FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)).Select(i => i.Name).ToArray();
    }

    // Shows one of the tag editor's panels through the link list on its left.
    public static void ShowPanel(AutomationElement dialog, string link)
    {
      AutomationElement hyperlink = dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Hyperlink)).First(h => h.Name == link);
      hyperlink.Patterns.Invoke.Pattern.Invoke();
      Thread.Sleep(200);
    }
  }
}
