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
using TwinCAT.TypeSystem;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: INotifyPropertyChanged
    {
        #region Fields
        
        private readonly List<object> _symbolValues = new List<object>();
        private int[] _activePorts;

        private readonly DispatcherTimer _refreshDataTimer = new DispatcherTimer(DispatcherPriority.Render);
        private ScrollViewer _scrollViewer;

        private readonly List<PlcConnection> _plcConnections = new List<PlcConnection>();

        private int _activePlc;

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
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static bool CheckLibrary(string fileName)
        {
            return LoadLibrary(fileName) != IntPtr.Zero;
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            Title = $"TwinCAT Variable Viewer - v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd('0').TrimEnd('.')}";

            _refreshDataTimer.Interval = TimeSpan.FromMilliseconds(200);

            // check if twincat is installed
            if (CheckLibrary("tcadsdll.dll"))
            {
                SplashScreen.LoadingStatus("Connecting to PLC");
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
            _activePlc = 0;
            _activePorts = GetActivePlcPorts("127.0.0.1.1.1", 851);
            _plcConnections.Clear();
            ComboBoxPlc.Items.Clear();
            SymbolListViewItems?.Clear();
            foreach (int port in _activePorts)
            {
                PlcConnection plcCon = new PlcConnection(new AmsAddress($"127.0.0.1.1.1:{port}"));
                plcCon.PlcConnectionError += PlcOnPlcConnectionError;
                plcCon.Connect();
                if (plcCon.Connected)
                {
                    plcCon.Connection.AdsStateChanged += PlcAdsStateChanged;
                    plcCon.Connection.ConnectionStateChanged += PlcOnConnectionStateChanged;
                    plcCon.Connection.AmsRouterNotification += PlcOnAmsRouterNotification;
                }
                _plcConnections.Add(plcCon);
            }
            
            if (_plcConnections.Count == 0)
            {
                UpdateDumpStatus("No active PLCs found", Colors.Orange);
                return;
            }

            if (_plcConnections[_activePlc].Connected)
            {
                UpdateDumpStatus("Retrieving symbols", Colors.GreenYellow);
                PopulateListView();
            }

            // Populate Combobox
            if (_plcConnections.Count > 0)
            {
                foreach (int activePort in _activePorts)
                {
                    ComboBoxItem cbi = new ComboBoxItem();
                    cbi.Content = activePort.ToString();
                    ComboBoxPlc.Items.Add(cbi);
                }

                ComboBoxPlc.SelectedIndex = 0;
            }

            PlcConnected = _plcConnections[_activePlc].Connected;
            ButtonDumpData.IsEnabled = PlcConnected;
        }

        private void PlcOnPlcConnectionError(object sender, PlcConnectionErrorEventArgs e)
        {
            UpdateDumpStatus(e.Message, Colors.Red);
            PlcConnected = _plcConnections[_activePlc].Connected;
        }

        /// <summary>
        /// Get active ports from PLC. This simply tries to establish a connection to the PLC with the starting port
        /// and increases that by one. After 5 ports with unsuccessful connection this function returns a list of port numbers
        /// </summary>
        /// <param name="amsIp">PLC AMS IP</param>
        /// <param name="startPort">Start port, increased by one after succesful connection</param>
        /// <returns>Array of int with active ports</returns>
        private static int[] GetActivePlcPorts(string amsIp, int startPort)
        {
            List<int> activePorts = new List<int>();
            int port = startPort;
            int unsuccessfulAttempts = 0;
            while (true)
            {
                if (unsuccessfulAttempts >= 5 || activePorts.Count >= 25) break;
                using (AdsSession session = new AdsSession(new AmsAddress($"{amsIp}:{port}")))
                {
                    session.Connect();
                    try
                    {
                        StateInfo stateInfo = session.Connection.ReadState();
                        if (stateInfo.AdsState != AdsState.Invalid)
                        {
                            activePorts.Add(port);
                        }
                        else
                        {
                            unsuccessfulAttempts++;
                        }
                        session.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        break;
                    }
                }
                port++;
            }

            return activePorts.ToArray();
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

        private void PlcAdsStateChanged(object sender, AdsStateChangedEventArgs e)
        {
            Debug.WriteLine($"ADS state changed: {e.State.AdsState}; Device state: {e.State.DeviceState}");
            DisplayPlcState(e.State.AdsState);
            PlcConnected = e.State.AdsState == AdsState.Run || e.State.AdsState == AdsState.Stop;
        }

        private void PlcOnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Debug.WriteLine($"Client connection state was {e.OldState} and is now {e.NewState} because of {e.Reason}");
            if (e.NewState == ConnectionState.Connected) return;
            PlcConnected = false;
            UpdateDumpStatus("PLC state: Disconnected", Colors.Red);
        }

        private void PlcOnAmsRouterNotification(object sender, AmsRouterNotificationEventArgs e)
        {
            Debug.WriteLine($"AMS router notification: {e.State}");
            if (e.State == AmsRouterState.Start) return;
            PlcConnected = false;
            UpdateDumpStatus("ADS router stopped", Colors.Red);
        }

        #endregion
        
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Get scrollviewer
            Decorator border = VisualTreeHelper.GetChild(SymbolListView, 0) as Decorator;
            if (border != null) _scrollViewer = border.Child as ScrollViewer;

            SplashScreen.EndDisplay();
        }

        private void RefreshDataTimerOnTick(object sender, EventArgs eventArgs)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            if (_scrollViewer == null) return;
            for (int i = 0; i < (int)_scrollViewer.ViewportHeight; i++)
            {
                SymbolInfo symbol = SymbolListViewItems[(int)_scrollViewer.VerticalOffset + i];
                SymbolListViewItems[(int)_scrollViewer.VerticalOffset + i].CurrentValue = Tc3Symbols.GetSymbolValue(symbol, _plcConnections[_activePlc].Connection);
            }
            //Debug.WriteLine($"Collecting data from PLC for ListView: {sw.Elapsed}");
        }

        private void PopulateListView(string filterName = null)
        {
            SymbolListViewItems?.Clear();
            if (_plcConnections.Count == 0) return;

            foreach (ISymbol symbol in _plcConnections[_activePlc].Symbols)
            {
                if (filterName == null || symbol.InstancePath.ToLower().Contains(filterName.ToLower()))
                {
                    SymbolListViewItems?.Add(new SymbolInfo
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

        private async Task ReadAll(PlcConnection plcCon)
        {
            // Stopwatch sw = Stopwatch.StartNew();
            _symbolValues.Clear();

            // Split up load in packages of 500 Variables at once. More would be faster but stresses the PLC more.
            SymbolCollection symbolColl = new SymbolCollection();
            int cnt = 0;
            foreach (var symbol in plcCon.Symbols)
            {
                if (cnt >= 500)
                {
                    SumSymbolRead sumSymbolRead = new SumSymbolRead(plcCon.Connection, symbolColl);
                    await Task.Run(() =>
                    {
                        object[] symbolValuesReturn = sumSymbolRead.Read();
                        _symbolValues.AddRange(symbolValuesReturn);
                    });
                    cnt = 0;
                    symbolColl = new SymbolCollection();
                }
                symbolColl.Add(symbol);
                cnt++;
            }

            // Final sum symbol read for the rest in symbolColl
            if (cnt != 0)
            {
                SumSymbolRead sumSymbolRead = new SumSymbolRead(plcCon.Connection, symbolColl);
                await Task.Run(() =>
                {
                    object[] symbolValuesReturn = sumSymbolRead.Read();
                    _symbolValues.AddRange(symbolValuesReturn);
                });
            }
            // Debug.WriteLine($"Collecting data from PLC: {sw.Elapsed}");
        }

        private async void ButtonDumpData_OnClick(object sender, RoutedEventArgs e)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            DumpSpinner(true);
            foreach (PlcConnection plcConnection in _plcConnections)
            {
                UpdateDumpStatus("Dumping data...", Colors.AliceBlue);
                if (!PlcConnected)
                {
                    UpdateDumpStatus($"PLC {plcConnection.Session.Port} not running", Colors.Orange);
                    return;
                }

                if (plcConnection.Symbols.Count == 0)
                {
                    UpdateDumpStatus($"No Symbols to dump for port: {plcConnection.Session.Port}", Colors.Orange);
                    return;
                }

                await ReadAll(plcConnection).ConfigureAwait(false);

                if (_symbolValues.Count != plcConnection.Symbols.Count)
                {
                    UpdateDumpStatus("Error dumping data: Missmatch in symbol array sizes!", Colors.Red);
                    DumpSpinner(false);
                    return;
                }

                // Write xml
                try
                {
                    XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                    using (XmlWriter writer = XmlWriter.Create($"VariableDump_{plcConnection.Session.Port}.xml", settings))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Symbols");

                        for (var i = 0; i < _symbolValues.Count; i++)
                        {
                            writer.WriteStartElement("Symbol");

                            writer.WriteElementString("Path", ReplaceHexadecimalSymbols(plcConnection.Symbols[i].InstancePath));
                            writer.WriteElementString("Type", ReplaceHexadecimalSymbols(plcConnection.Symbols[i].TypeName));
                            writer.WriteElementString("IndexGroup", ReplaceHexadecimalSymbols(((IAdsSymbol)plcConnection.Symbols[i]).IndexGroup.ToString()));
                            writer.WriteElementString("IndexOffset", ReplaceHexadecimalSymbols(((IAdsSymbol)plcConnection.Symbols[i]).IndexOffset.ToString()));
                            writer.WriteElementString("Size", ReplaceHexadecimalSymbols(plcConnection.Symbols[i].Size.ToString()));
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
                        UpdateDumpStatus($"Cannot access 'VariableDump_{plcConnection.Session.Port}.xml'. Opened in another application?!", Colors.Orange);
                    else UpdateDumpStatus(ex.Message, Colors.Orange);
                    DumpSpinner(false);
                    return;
                }

                // Write csv
                try
                {
                    using (var w = new StreamWriter($"VariableDump_{plcConnection.Session.Port}.csv"))
                    {
                        const string delimiter = ";";
                        for (var i = 0; i < _symbolValues.Count; i++)
                        {
                            w.WriteLine($"{plcConnection.Symbols[i].InstancePath}{delimiter}{plcConnection.Symbols[i].TypeName}{delimiter}{((IAdsSymbol)plcConnection.Symbols[i]).IndexGroup}{delimiter}{((IAdsSymbol)plcConnection.Symbols[i]).IndexOffset}{delimiter}{plcConnection.Symbols[i].Size}{delimiter}{_symbolValues[i]}");
                            w.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (ex.Message.StartsWith("The process cannot access the file"))
                        UpdateDumpStatus($"Cannot access 'VariableDump_{plcConnection.Session.Port}.csv'. Opened in another application?!", Colors.Orange);
                    else UpdateDumpStatus(ex.Message, Colors.Orange);
                    DumpSpinner(false);
                    return;
                }
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
        private static string ReplaceHexadecimalSymbols(string txt)
        {
            const string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
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
            bool isLoaded = false;
            Application.Current.Dispatcher.Invoke(() => isLoaded = IsLoaded);
            if (!isLoaded) SplashScreen.LoadingStatus(text);
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

        private void ComboBoxPlc_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _activePlc = ComboBoxPlc.SelectedIndex >= 0 ? ComboBoxPlc.SelectedIndex : 0;
            PopulateListView();
        }
    }
}
