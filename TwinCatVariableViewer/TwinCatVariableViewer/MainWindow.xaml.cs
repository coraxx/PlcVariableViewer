using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TwinCAT.Ads;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcAdsClient _plcClient;
        private TcAdsSymbolInfoLoader _symbolLoader;
        private TcAdsSymbolInfoCollection _symbols;
        private bool _plcConnected;
        private readonly DispatcherTimer _refreshDataTimer;

        private ScrollViewer _scrollViewer;

        private ObservableCollection<SymbolInfo> _symbolListViewItems = new ObservableCollection<SymbolInfo>();
        public ObservableCollection<SymbolInfo> SymbolListViewItems
        {
            get { return _symbolListViewItems ?? (_symbolListViewItems = new ObservableCollection<SymbolInfo>()); }
            set { _symbolListViewItems = value; }
        }

        public MainWindow()
        {
            InitializeComponent();

            ConnectPlc();
            GetSymbols();
            PopulateListView();

            _refreshDataTimer = new DispatcherTimer(DispatcherPriority.Render);
            _refreshDataTimer.Interval = TimeSpan.FromMilliseconds(500);
            _refreshDataTimer.Tick += RefreshDataTimerOnTick;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Get scrollviewer
            if (VisualTreeHelper.GetChild(SymbolListView, 0) is Decorator border) _scrollViewer = border.Child as ScrollViewer;
            if (_scrollViewer != null)
            {
                _refreshDataTimer.IsEnabled = _plcConnected;
            }
        }

        private void RefreshDataTimerOnTick(object sender, EventArgs eventArgs)
        {
            for (int i = 0; i < (int)_scrollViewer.ViewportHeight; i++)
            {
                SymbolInfo symbol = SymbolListViewItems[(int)_scrollViewer.VerticalOffset+i];
                string data = "";
                if (symbol.Type == "BOOL")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(bool)).ToString();
                if (symbol.Type == "BYTE")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                if (symbol.Type == "SINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(sbyte)).ToString();
                if (symbol.Type == "INT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(short)).ToString();
                if (symbol.Type == "DINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(int)).ToString();
                if (symbol.Type == "LINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(long)).ToString();
                if (symbol.Type == "USINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                if (symbol.Type == "UINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                if (symbol.Type == "ULINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();
                if (symbol.Type == "UDINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ulong)).ToString();
                if (symbol.Type == "REAL")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(float)).ToString();
                if (symbol.Type == "LREAL")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(double)).ToString();
                if (symbol.Type == "WORD")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                if (symbol.Type == "DWORD")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();

                SymbolListViewItems[(int) _scrollViewer.VerticalOffset + i].CurrentValue = data;
            }
        }

        private void PopulateListView()
        {
            SymbolListViewItems?.Clear();

            foreach (TcAdsSymbolInfo symbol in _symbols)
            {
                string data = "";
                if (symbol.TypeName == "BOOL")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(bool)).ToString();
                if (symbol.TypeName == "BYTE")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                if (symbol.TypeName == "SINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(sbyte)).ToString();
                if (symbol.TypeName == "INT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(short)).ToString();
                if (symbol.TypeName == "DINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(int)).ToString();
                if (symbol.TypeName == "LINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(long)).ToString();
                if (symbol.TypeName == "USINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                if (symbol.TypeName == "UINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                if (symbol.TypeName == "ULINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();
                if (symbol.TypeName == "UDINT")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ulong)).ToString();
                if (symbol.TypeName == "REAL")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(float)).ToString();
                if (symbol.TypeName == "LREAL")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(double)).ToString();
                if (symbol.TypeName == "WORD")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                if (symbol.TypeName == "DWORD")
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();

                SymbolListViewItems?.Add(new SymbolInfo()
                {
                    Path = symbol.Name,
                    Type = symbol.TypeName,
                    Size = symbol.Size,
                    IndexGroup = symbol.IndexGroup,
                    IndexOffset = symbol.IndexOffset,
                    IsStatic = symbol.IsStatic,
                    CurrentValue = data
                });
            }
        }

        private void GetSymbols()
        {
            _symbols = _symbolLoader.GetSymbols(false);
        }

        private void ConnectPlc()
        {
            try
            {
                _plcClient = new TcAdsClient();
                _plcClient.Connect("127.0.0.1.1.1", 851);
                _symbolLoader = _plcClient.CreateSymbolInfoLoader();
                _plcConnected = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _plcConnected = false;
            }
        }
    }

    public class SymbolInfo: INotifyPropertyChanged
    {
        private string _path;
        public string Path
        {
            get { return _path; }
            set
            {
                if (value == _path) return;
                _path = value;
                OnPropertyChanged();
            }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                if (value == _type) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        private int _size;
        public int Size
        {
            get { return _size; }
            set
            {
                if (value == _size) return;
                _size = value;
                OnPropertyChanged();
            }
        }

        private long _indexGroup;
        public long IndexGroup
        {
            get { return _indexGroup; }
            set
            {
                if (value == _indexGroup) return;
                _indexGroup = value;
                OnPropertyChanged();
            }
        }

        private long _indexOffset;
        public long IndexOffset
        {
            get { return _indexOffset; }
            set
            {
                if (value == _indexOffset) return;
                _indexOffset = value;
                OnPropertyChanged();
            }
        }

        private bool _isStatic;
        public bool IsStatic
        {
            get { return _isStatic; }
            set
            {
                if (value == _isStatic) return;
                _isStatic = value;
                OnPropertyChanged();
            }
        }

        private string _currentValue;
        public string CurrentValue
        {
            get { return _currentValue; }
            set
            {
                if (value == _currentValue) return;
                _currentValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));  
        }
    }

    [ValueConversion(typeof(object), typeof(int))]
    public class BoolToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            var _bool = (bool)System.Convert.ChangeType(value, typeof(bool));

            if (_bool)
                return 1;

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
