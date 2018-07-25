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
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.Ads.ValueAccess;
using TwinCAT.TypeSystem;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const bool DEBUG = false;
        private TcAdsClient _plcClient;
        private ISymbolLoader _symbolLoader;
        private List<ISymbol> _symbols = new List<ISymbol>();
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
                SymbolListViewItems[(int) _scrollViewer.VerticalOffset + i].CurrentValue = GetSymbolValue(symbol);
            }
        }

        private void PopulateListView()
        {
            SymbolListViewItems?.Clear();

            foreach (ISymbol symbol in _symbols)
            {
                SymbolListViewItems?.Add(new SymbolInfo()
                {
                    Path = symbol.InstancePath,
                    Type = symbol.TypeName,
                    Size = symbol.Size,
                    IndexGroup = ((IAdsSymbol)symbol).IndexGroup,
                    IndexOffset = ((IAdsSymbol)symbol).IndexOffset,
                    IsStatic = symbol.IsStatic,
                    CurrentValue = "pending..." // GetSymbolValue(symbol) // startup takes to long with loads of variables
                });
            }
        }

        private string GetSymbolValue(ISymbol symbol)
        {
            string data = "";
            TimeSpan t;
            DateTime dt;
            switch (symbol.TypeName)
            {
                case "BOOL":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(bool)).ToString();
                    break;
                case "BYTE":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(byte)).ToString();
                    break;
                case "SINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(sbyte)).ToString();
                    break;
                case "INT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(short)).ToString();
                    break;
                case "DINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(int)).ToString();
                    break;
                case "LINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(long)).ToString();
                    break;
                case "USINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(byte)).ToString();
                    break;
                case "UINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ushort)).ToString();
                    break;
                case "ULINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)).ToString();
                    break;
                case "UDINT":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ulong)).ToString();
                    break;
                case "REAL":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(float)).ToString();
                    break;
                case "LREAL":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(double)).ToString();
                    break;
                case "WORD":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ushort)).ToString();
                    break;
                case "DWORD":
                    data = _plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)).ToString();
                    break;
                case "TIME":
                    t = TimeSpan.FromMilliseconds((uint)_plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                    if (t.Minutes > 0) data = $"T#{t.Minutes}m{t.Seconds}s{t.Milliseconds}ms";
                    else if (t.Seconds > 0) data = $"T#{t.Seconds}s{t.Milliseconds}ms";
                    else data = $"T#{t.Milliseconds}ms";
                    break;
                case "TIME_OF_DAY":
                case "TOD":
                    t = TimeSpan.FromMilliseconds((uint)_plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                    if (t.Hours > 0) data = $"TOD#{t.Hours}:{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                    else if (t.Minutes > 0) data = $"TOD#{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                    else data = $"TOD#{t.Seconds}.{t.Milliseconds}";
                    break;
                case "DATE":
                    dt = new DateTime(1970, 1, 1);
                    dt = dt.AddSeconds((uint)_plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                    data = $"D#{dt.Year}-{dt.Month}-{dt.Day}";
                    break;
                case "DATE_AND_TIME":
                case "DT":
                    dt = new DateTime(1970, 1, 1);
                    dt = dt.AddSeconds((uint)_plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                    data = $"DT#{dt.Year}-{dt.Month}-{dt.Day}-{dt.Hour}:{dt.Minute}:{dt.Second}";
                    break;
            }

            return data;
        }

        private string GetSymbolValue(SymbolInfo symbol)
        {
            string data = "";
            TimeSpan t;
            DateTime dt;
            switch (symbol.Type)
            {
                case "BOOL":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(bool)).ToString();
                    break;
                case "BYTE":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                    break;
                case "SINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(sbyte)).ToString();
                    break;
                case "INT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(short)).ToString();
                    break;
                case "DINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(int)).ToString();
                    break;
                case "LINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(long)).ToString();
                    break;
                case "USINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                    break;
                case "UINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                    break;
                case "ULINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();
                    break;
                case "UDINT":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ulong)).ToString();
                    break;
                case "REAL":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(float)).ToString();
                    break;
                case "LREAL":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(double)).ToString();
                    break;
                case "WORD":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                    break;
                case "DWORD":
                    data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();
                    break;
                case "TIME":
                    t = TimeSpan.FromMilliseconds((uint)_plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                    if (t.Minutes > 0) data = $"T#{t.Minutes}m{t.Seconds}s{t.Milliseconds}ms";
                    else if (t.Seconds > 0) data = $"T#{t.Seconds}s{t.Milliseconds}ms";
                    else data = $"T#{t.Milliseconds}ms";
                    break;
                case "TIME_OF_DAY":
                case "TOD":
                    t = TimeSpan.FromMilliseconds((uint)_plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                    if (t.Hours > 0) data = $"TOD#{t.Hours}:{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                    else if (t.Minutes > 0) data = $"TOD#{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                    else data = $"TOD#{t.Seconds}.{t.Milliseconds}";
                    break;
                case "DATE":
                    dt = new DateTime(1970, 1, 1);
                    dt = dt.AddSeconds((uint)_plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                    data = $"D#{dt.Year}-{dt.Month}-{dt.Day}";
                    break;
                case "DATE_AND_TIME":
                case "DT":
                    dt = new DateTime(1970, 1, 1);
                    dt = dt.AddSeconds((uint)_plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                    data = $"DT#{dt.Year}-{dt.Month}-{dt.Day}-{dt.Hour}:{dt.Minute}:{dt.Second}";
                    break;
                default:
                    if (symbol.Type.StartsWith("STRING"))
                    {
                        int charCount = Convert.ToInt32(symbol.Type.Replace("STRING(", "").Replace(")", ""));
                        data = _plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(string), new[] { charCount }).ToString();
                    }
                    break;
            }

            return data;
        }

        private void GetSymbols()
        {
            System.Diagnostics.Debug.WriteLine("Adding '{0}' Symbols:", _symbolLoader.Symbols.Count);
            foreach (ISymbol symbol in _symbolLoader.Symbols)
            {
                AddSymbolRecursive(_symbols, symbol);
            }
        }
        public void AddSymbolRecursive(List<ISymbol> symbols, ISymbol symbol)
        {
            IDataType type = symbol.DataType as IDataType;

            foreach (ITypeAttribute attribute in symbol.Attributes)
            {
                if (DEBUG) Debug.WriteLine(string.Format("{{ {0} : {1} }}", attribute.Name, attribute.Value));
            }

            if (DEBUG) Debug.WriteLine(string.Format("{0} : {1} (IG: 0x{2} IO: 0x{3} size: {4})", symbol.InstancePath, symbol.TypeName, ((IAdsSymbol)symbol).IndexGroup.ToString("x"), ((IAdsSymbol)symbol).IndexOffset.ToString("x"), symbol.Size));

            if (symbol.Category == DataTypeCategory.Array)
            {
                IArrayInstance arrInstance = (IArrayInstance)symbol;
                IArrayType arrType = (IArrayType)symbol.DataType;

                int count = 0;

                foreach (ISymbol arrayElement in arrInstance.Elements)
                {
                    AddSymbolRecursive(symbols, arrayElement);
                    count++;

                    if (count > 20) // Write only the first 20 to limit output
                        break;
                }
            }
            else if (symbol.Category == DataTypeCategory.Struct)
            {
                IStructInstance structInstance = (IStructInstance)symbol;
                IStructType structType = (IStructType)symbol.DataType;

                foreach (ISymbol member in structInstance.MemberInstances)
                {
                    AddSymbolRecursive(symbols, member);
                }
            }
            else
            {
                symbols.Add(symbol);
            }
        }

        private void ConnectPlc()
        {
            try
            {
                _plcClient = new TcAdsClient();
                _plcClient.Connect("127.0.0.1.1.1", 851);
                SymbolLoaderSettings settings = new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree, ValueAccessMode.IndexGroupOffsetPreferred);
                _symbolLoader = SymbolLoaderFactory.Create(_plcClient, settings);
                _plcConnected = true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
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
