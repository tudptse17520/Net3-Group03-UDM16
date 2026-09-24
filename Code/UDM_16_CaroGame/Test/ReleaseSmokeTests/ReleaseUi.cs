using System.Diagnostics;
using System.Windows.Automation;

internal static class ReleaseUi
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    internal static AutomationElement? Across(Process p, string id) => FindAcrossWindows(p, AutomationElement.AutomationIdProperty, id);
    internal static void ClickAcross(Process p, string id) => ((InvokePattern)Across(p, id)!.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    internal static void Chat(Process p, string panelId, string text)
    {
        var panel = Across(p, panelId)!;
        var input = panel.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "ChatInput"));
        ((ValuePattern)input.GetCurrentPattern(ValuePattern.Pattern)).SetValue(text);
        var send = panel.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "ChatSend"));
        ((InvokePattern)send.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    }
    internal static bool ChatContains(Process p, string panelId, string text)
    {
        var panel = Across(p, panelId);
        var history = panel?.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "ChatHistory"));
        return history != null && ((TextPattern)history.GetCurrentPattern(TextPattern.Pattern)).DocumentRange.GetText(-1).Contains(text);
    }
    internal static void DoubleClickRoom(Process p)
    {
        var list = Find(p, "LstRooms")!;
        var item = list.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
        ((SelectionItemPattern)item!.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        Program.Assert(PostMessage(new IntPtr(list.Current.NativeWindowHandle), 0x203, new IntPtr(1), new IntPtr(20 | (10 << 16))), "Post double-click to test room list.");
        Program.Assert(PostMessage(new IntPtr(list.Current.NativeWindowHandle), 0x202, IntPtr.Zero, new IntPtr(20 | (10 << 16))), "Complete mouse-up for WinForms DoubleClick event.");
    }
    internal static AutomationElement Window(Process process)
    {
        process.Refresh();
        return AutomationElement.FromHandle(process.MainWindowHandle);
    }
    internal static AutomationElement? Find(Process process, string id) => Window(process).FindFirst(TreeScope.Descendants,
        new PropertyCondition(AutomationElement.AutomationIdProperty, id));
    internal static void Set(Process process, string id, string value) =>
        ((ValuePattern)Find(process, id)!.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
    internal static void Click(Process process, string id) =>
        ((InvokePattern)Find(process, id)!.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    internal static void Login(Process process, string name)
    {
        Set(process, "TxtNickname", name);
        Click(process, "BtnConnect");
    }
    internal static void RemoteLogin(Process process, string host, int port, string name)
    {
        ((SelectionItemPattern)Find(process, "RemoteMode")!.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        Set(process, "TxtServerIp", host);
        Set(process, "TxtPort", port.ToString());
        Login(process, name);
    }
    internal static bool InLobby(Process process)
    {
        process.Refresh(); return process.MainWindowTitle.Contains("Sảnh Chờ");
    }
    internal static bool Sees(Process process, string opponent)
    {
        var list = Find(process, "LstPlayers");
        return list?.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, opponent)) != null;
    }
    internal static void Invite(Process process, string opponent)
    {
        var item = Find(process, "LstPlayers")!.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, opponent));
        ((SelectionItemPattern)item!.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        Click(process, "BtnChallenge");
    }
    internal static bool AcceptInvite(Process process)
    {
        var yes = FindAcrossWindows(process, AutomationElement.NameProperty, "XÁC NHẬN");
        if (yes == null) return false;
        ((InvokePattern)yes.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        return true;
    }
    internal static AutomationElement? FindAcrossWindows(Process process, AutomationProperty property, string value)
    {
        foreach (AutomationElement window in AutomationElement.RootElement.FindAll(TreeScope.Children,
                     new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id)))
        {
            var element = window.FindFirst(TreeScope.Descendants, new PropertyCondition(property, value));
            if (element != null) return element;
        }
        return null;
    }
    internal static string? Count(Process process, int player) =>
        FindAcrossWindows(process, AutomationElement.AutomationIdProperty, $"lblPlayer{player}MoveCount")?.Current.Name;
    internal static void Move(Process process, int row, int col) =>
        ((InvokePattern)FindAcrossWindows(process, AutomationElement.AutomationIdProperty, $"Cell_{row}_{col}")!.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    internal static bool ReturnFromResult(Process process)
    {
        var button = FindAcrossWindows(process, AutomationElement.AutomationIdProperty, "btnVeSanh");
        if (button?.Current.IsEnabled != true) return false;
        ((InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        return true;
    }
}
