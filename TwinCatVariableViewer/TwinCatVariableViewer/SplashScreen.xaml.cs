using System.Windows;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        private static SplashScreen splashScreen = new SplashScreen();

        // Delegate to refresh UI elements
        private delegate void RefreshDelegate();
        private static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, (RefreshDelegate)delegate { });
        }

        public SplashScreen()
        {
            InitializeComponent();
        }

        public static void BeginDisplay()
        {
            splashScreen.Show();
        }

        public static void EndDisplay()
        {
            splashScreen.Close();
        }

        public static void LoadingStatus(string status)
        {
            splashScreen.LoadingStatusLabel.Content = status;
            Refresh(splashScreen.LoadingStatusLabel);
        }
    }
}
