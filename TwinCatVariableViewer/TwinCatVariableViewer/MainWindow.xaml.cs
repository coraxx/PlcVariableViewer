using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.Ads.ValueAccess;
using TwinCAT.TypeSystem;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: INotifyPropertyChanged
    {
        #region Global variables

        //private TcAdsClient _plcClient;

        private AdsSession _session;
        private AdsConnection _connection;
        private readonly SymbolLoaderSettings _symbolLoaderSettings = new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree, ValueAccessMode.IndexGroupOffsetPreferred);

        private ISymbolLoader _symbolLoader;
        private readonly List<ISymbol> _symbols = new List<ISymbol>();
        private object[] _symbolValues;
        private readonly DispatcherTimer _refreshDataTimer = new DispatcherTimer(DispatcherPriority.Render);
        private ScrollViewer _scrollViewer;

        #endregion

        #region Properties

        private ObservableCollection<SymbolInfo> _symbolListViewItems = new ObservableCollection<SymbolInfo>();
        public ObservableCollection<SymbolInfo> SymbolListViewItems
        {
            get { return _symbolListViewItems ?? (_symbolListViewItems = new ObservableCollection<SymbolInfo>()); }
            set { _symbolListViewItems = value; }
        }

        private bool _plcConnected;
        public bool PlcConnected
        {
            get { return _plcConnected; }
            set
            {
                if (_plcConnected != value)
                {
                    if (value)
                    {
                        _refreshDataTimer.IsEnabled = true;
                        _refreshDataTimer.Tick += RefreshDataTimerOnTick;
                    }
                    else
                    {
                        _refreshDataTimer.IsEnabled = false;
                        _refreshDataTimer.Tick -= RefreshDataTimerOnTick;
                    }
                }
                _plcConnected = value;
                OnPropertyChanged("PlcConnected");
            }
        }

            #endregion

        #region Check if DLL exists
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        static bool CheckLibrary(string fileName)
        {
            return LoadLibrary(fileName) != IntPtr.Zero;
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _refreshDataTimer.Interval = TimeSpan.FromMilliseconds(200);

            // check if twincat is installed
            if (CheckLibrary("tcadsdll.dll"))
            {
                ConnectPlc();
            }
            else
            {
                ButtonDumpData.IsEnabled = false;
                ButtonReconnect.IsEnabled = false;
                UpdateDumpStatus("Did not find tcadsdll.dll. Is TwinCat/ADS installed?!", Colors.Red);
            }
        }

        private void ConnectPlc()
        {
            if (_session != null && _connection.ConnectionState == ConnectionState.Connected)
            {
                DisconnectPlc();
            }

            try
            {
                _session?.Dispose();
                _session = new AdsSession(new AmsAddress("127.0.0.1.1.1:851"), SessionSettings.Default);
                _connection = (AdsConnection)_session.Connect();

                _connection.AdsStateChanged += _plcClient_AdsStateChanged;
                _connection.ConnectionStateChanged += PlcClientOnConnectionStateChanged;
                _connection.AmsRouterNotification += PlcClientOnAmsRouterNotification;

                _symbolLoader = SymbolLoaderFactory.Create(_connection, _symbolLoaderSettings);

                StateInfo stateInfo = _connection.ReadState();
                AdsState state = stateInfo.AdsState;
                DisplayPlcState(state);
                if (state == AdsState.Run || state == AdsState.Stop)
                {
                    PlcConnected = true;
                    if (PlcConnected)
                    {
                        GetSymbols();
                        PopulateListView();
                    }
                }
                else
                {
                    DisconnectPlc();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                UpdateDumpStatus($"PLC state: {ex.Message}", Colors.Red);
                PlcConnected = false;
            }
            ButtonDumpData.IsEnabled = _plcConnected;
        }

        private void DisconnectPlc()
        {
            PlcConnected = false;
            _connection.Disconnect();
            UpdateDumpStatus("PLC disconnected", Colors.Orange);
        }

        private void DisplayPlcState(AdsState state)
        {
            switch (state)
            {
                case AdsState.Invalid:
                    UpdateDumpStatus("PLC state: Invalid", Colors.Red);
                    break;
                case AdsState.Idle:
                    UpdateDumpStatus("PLC state: Idle", Colors.Orange);
                    break;
                case AdsState.Reset:
                    UpdateDumpStatus("PLC state: Reset", Colors.Orange);
                    break;
                case AdsState.Init:
                    UpdateDumpStatus("PLC state: Init", Colors.Orange);
                    break;
                case AdsState.Start:
                    UpdateDumpStatus("PLC state: Start", Colors.Yellow);
                    break;
                case AdsState.Run:
                    UpdateDumpStatus("PLC state: Run", Colors.GreenYellow);
                    break;
                case AdsState.Stop:
                    UpdateDumpStatus("PLC state: Stop", Colors.Orange);
                    break;
                case AdsState.SaveConfig:
                    UpdateDumpStatus("PLC state: SaveConfig", Colors.Orange);
                    break;
                case AdsState.LoadConfig:
                    UpdateDumpStatus("PLC state: LoadConfig", Colors.Orange);
                    break;
                case AdsState.PowerFailure:
                    UpdateDumpStatus("PLC state: PowerFailure", Colors.Orange);
                    break;
                case AdsState.PowerGood:
                    UpdateDumpStatus("PLC state: PowerGood", Colors.Orange);
                    break;
                case AdsState.Error:
                    UpdateDumpStatus("PLC state: Error", Colors.Orange);
                    break;
                case AdsState.Shutdown:
                    UpdateDumpStatus("PLC state: Shutdown", Colors.Orange);
                    break;
                case AdsState.Suspend:
                    UpdateDumpStatus("PLC state: Suspend", Colors.Orange);
                    break;
                case AdsState.Resume:
                    UpdateDumpStatus("PLC state: Resume", Colors.Orange);
                    break;
                case AdsState.Config:
                    UpdateDumpStatus("PLC state: Config", Colors.Orange);
                    break;
                case AdsState.Reconfig:
                    UpdateDumpStatus("PLC state: Reconfig", Colors.Orange);
                    break;
                case AdsState.Stopping:
                    UpdateDumpStatus("PLC state: Stopping", Colors.Orange);
                    break;
                case AdsState.Incompatible:
                    UpdateDumpStatus("PLC state: Incompatible", Colors.Red);
                    break;
                case AdsState.Exception:
                    UpdateDumpStatus("PLC state: Exception", Colors.Red);
                    break;
                default:
                    Debug.WriteLine(state);
                    break;
            }
        }

        #region ADS events

        private void _plcClient_AdsStateChanged(object sender, AdsStateChangedEventArgs e)
        {
            Debug.WriteLine($"ADS state changed: {e.State.AdsState}; Device state: {e.State.DeviceState}");
            DisplayPlcState(e.State.AdsState);
            PlcConnected = (e.State.AdsState == AdsState.Run || e.State.AdsState == AdsState.Stop);
        }

        private void PlcClientOnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Debug.WriteLine($"Client connection state was {e.OldState} and is now {e.NewState} because of {e.Reason}");
            if (e.NewState != ConnectionState.Connected)
            {
                PlcConnected = false;
                UpdateDumpStatus("PLC state: Disconnected", Colors.Red);
            }
        }

        private void PlcClientOnAmsRouterNotification(object sender, AmsRouterNotificationEventArgs e)
        {
            Debug.WriteLine($"AMS router notification: {e.State}");
            if (e.State != AmsRouterState.Start)
            {
                PlcConnected = false;
                UpdateDumpStatus("ADS router stopped", Colors.Red);
            }
        }

        #endregion

        private void GetSymbols()
        {
            _symbols.Clear();
            foreach (ISymbol symbol in _symbolLoader.Symbols)
            {
                Tc3Symbols.AddSymbolRecursive(_symbols, symbol);
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Get scrollviewer
            Decorator border = VisualTreeHelper.GetChild(SymbolListView, 0) as Decorator;
            if (border != null) _scrollViewer = border.Child as ScrollViewer;
        }

        private void RefreshDataTimerOnTick(object sender, EventArgs eventArgs)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            if (_scrollViewer == null) return;
            for (int i = 0; i < (int)_scrollViewer.ViewportHeight; i++)
            {
                SymbolInfo symbol = SymbolListViewItems[(int)_scrollViewer.VerticalOffset + i];
                SymbolListViewItems[(int)_scrollViewer.VerticalOffset + i].CurrentValue = Tc3Symbols.GetSymbolValue(symbol, _connection);
            }
            //Debug.WriteLine($"Collecting data from PLC for ListView: {sw.Elapsed}");
        }

        private void PopulateListView(string filterName = null)
        {
            SymbolListViewItems?.Clear();

            foreach (ISymbol symbol in _symbols)
            {
                if (filterName == null || symbol.InstancePath.ToLower().Contains(filterName.ToLower()))
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
        }

        private void TextBox1_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) PopulateListView(TextBox1.Text);
        }

        private void ButtonFilter_OnClick(object sender, RoutedEventArgs e)
        {
            PopulateListView(TextBox1.Text);
        }

        private async Task ReadAll()
        {
            //Stopwatch sw = Stopwatch.StartNew();
            SymbolCollection symbolColl = new SymbolCollection();

            foreach (var symbol in _symbols)
            {
                symbolColl.Add(symbol);
            }

            SumSymbolRead sumSymbolRead = new SumSymbolRead(_connection, symbolColl);
            await Task.Run(() =>
            {
                _symbolValues = sumSymbolRead.Read();
            });
            //Debug.WriteLine($"Collecting data from PLC: {sw.Elapsed}");
        }

        private async void ButtonDumpData_OnClick(object sender, RoutedEventArgs e)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            UpdateDumpStatus("Dumping data...", Colors.AliceBlue);
            if (!PlcConnected)
            {
                UpdateDumpStatus("PLC not running", Colors.Orange);
                return;
            }

            if (_symbols.Count == 0)
            {
                UpdateDumpStatus("No Symbols to dump", Colors.Orange);
                return;
            }

            DumpSpinner(true);
            await ReadAll().ConfigureAwait(false);

            if (_symbolValues.Length != _symbols.Count)
            {
                UpdateDumpStatus("Error dumping data: Missmatch in symbol array sizes!", Colors.Red);
                DumpSpinner(false);
                return;
            }

            // Writing xml
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter writer = XmlWriter.Create("VariableDump.xml", settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Symbols");

                    for (var i = 0; i < _symbolValues.Length; i++)
                    {
                        writer.WriteStartElement("Symbol");

                        writer.WriteElementString("Path", ReplaceHexadecimalSymbols(SymbolListViewItems[i].Path));
                        writer.WriteElementString("Type", ReplaceHexadecimalSymbols(SymbolListViewItems[i].Type));
                        writer.WriteElementString("IndexGroup", ReplaceHexadecimalSymbols(SymbolListViewItems[i].IndexGroup.ToString()));
                        writer.WriteElementString("IndexOffset", ReplaceHexadecimalSymbols(SymbolListViewItems[i].IndexOffset.ToString()));
                        writer.WriteElementString("Size", ReplaceHexadecimalSymbols(SymbolListViewItems[i].Size.ToString()));
                        writer.WriteElementString("CurrentValue", ReplaceHexadecimalSymbols(_symbolValues[i].ToString()));

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (ex.Message.StartsWith("The process cannot access the file"))
                    UpdateDumpStatus("Cannot access 'VariableDump.xml'. Opened in another application?!", Colors.Orange);
                else UpdateDumpStatus(ex.Message, Colors.Orange);
                DumpSpinner(false);
                return;
            }

            // Writing csv
            try
            {
                using (var w = new StreamWriter("VariableDump.csv"))
                {
                    string delimiter = ";";
                    for (var i = 0; i < _symbolValues.Length; i++)
                    {
                        w.WriteLine($"{SymbolListViewItems[i].Path}{delimiter}{SymbolListViewItems[i].Type}{delimiter}{SymbolListViewItems[i].IndexGroup}{delimiter}{SymbolListViewItems[i].IndexOffset}{delimiter}{SymbolListViewItems[i].Size}{delimiter}{_symbolValues[i]}");
                        w.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (ex.Message.StartsWith("The process cannot access the file"))
                    UpdateDumpStatus("Cannot access 'VariableDump.csv'. Opened in another application?!", Colors.Orange);
                else UpdateDumpStatus(ex.Message, Colors.Orange);
                DumpSpinner(false);
                return;
            }

            UpdateDumpStatus("Done", Colors.GreenYellow);
            DumpSpinner(false);
            //Debug.WriteLine($"Dumping data from PLC to file: {sw.Elapsed}");
        }

        /// <summary>
        /// Replace unallowed characters in XML
        /// </summary>
        /// <param name="txt">text to parse for unallowed xml characters</param>
        /// <returns>filtered text</returns>
        private string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }

        private void DumpSpinner(bool show)
        {
            Spinner.Dispatcher.BeginInvoke(new Action(() =>
            {
                Storyboard sb = FindResource("SpinnerAnimation") as Storyboard;

                if (show)
                {
                    sb?.Begin();
                    Spinner.Visibility = Visibility.Visible;
                }
                else
                {
                    Spinner.Visibility = Visibility.Hidden;
                    sb?.Stop();
                }
            }));
        }

        private void UpdateDumpStatus(string text, Color fontColor)
        {
            TextBlockDumpStatus.Dispatcher.BeginInvoke(new Action(() =>
            {
                text = text.Length <= 120 ? text : $"{text.Substring(0, 120)}...";
                TextBlockDumpStatus.Text = text;
                TextBlockDumpStatus.Foreground = new SolidColorBrush(fontColor);
            }));
        }

        private void ButtonReconnect_OnClick(object sender, RoutedEventArgs e)
        {
            ConnectPlc();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
