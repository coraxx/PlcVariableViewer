using System.Windows;
using System.Windows.Threading;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        private static readonly SplashScreen Splash = new SplashScreen();

        // Delegate to refresh UI elements
        private delegate void RefreshDelegate();
        private static void Refresh(DispatcherObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, (RefreshDelegate)delegate { });
        }

        public SplashScreen()
        {
            InitializeComponent();
        }

        public static void BeginDisplay()
        {
            Splash.Show();
        }

        public static void EndDisplay()
        {
            Splash.Close();
        }

        public static void LoadingStatus(string status)
        {
            Splash.LoadingStatusLabel.Content = status;
            Refresh(Splash.LoadingStatusLabel);
        }
    }
}
