namespace Xilium.CefGlue
{
    public partial class CefBrowser
    {
        public void SendProcessMessage(CefProcessId targetProcess, CefProcessMessage message)
        {
            this.GetMainFrame().SendProcessMessage(targetProcess, message);
        }
    }
}