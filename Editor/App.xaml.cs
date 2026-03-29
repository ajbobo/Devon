using System.IO;
using System.Windows;

namespace Devon.Editor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            LogException(ex);
            try
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch { }
            Shutdown();
        }
    }

    private void LogException(Exception ex)
    {
        try
        {
            var logPath = "editor-error.log";
            var msg = $"{DateTime.Now:O} {ex}";
            File.AppendAllText(logPath, msg + Environment.NewLine);
        }
        catch { }
    }
}
