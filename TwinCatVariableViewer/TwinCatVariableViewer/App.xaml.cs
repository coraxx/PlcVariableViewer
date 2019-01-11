using System.Windows;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SplashScreen.BeginDisplay();
        }
    }
}
