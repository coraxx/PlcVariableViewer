using System.Windows;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        private static SplashScreen splash = new SplashScreen();

        public SplashScreen()
        {
            InitializeComponent();
        }

        public static void BeginDisplay()
        {
            splash.Show();
        }

        public static void EndDisplay()
        {
            splash.Close();
        }
    }
}
