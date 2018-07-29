using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
    public partial class MainWindow
    {
        private TcAdsClient _plcClient;
        private ISymbolLoader _symbolLoader;
        private readonly List<ISymbol> _symbols = new List<ISymbol>();
        private object[] _symbolValues;
        private bool _plcConnected;
        private readonly DispatcherTimer _refreshDataTimer = new DispatcherTimer(DispatcherPriority.Render);

        private ScrollViewer _scrollViewer;

        private ObservableCollection<SymbolInfo> _symbolListViewItems = new ObservableCollection<SymbolInfo>();
        public ObservableCollection<SymbolInfo> SymbolListViewItems
        {
            get => _symbolListViewItems ?? (_symbolListViewItems = new ObservableCollection<SymbolInfo>());
            set => _symbolListViewItems = value;
        }

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

            // check if twincat is installed
            if (CheckLibrary("tcadsdll.dll"))
            {
                ConnectPlc();
                if (_plcConnected)
                {
                    GetSymbols();
                }

                PopulateListView();

                _refreshDataTimer.Interval = TimeSpan.FromMilliseconds(200);
                _refreshDataTimer.Tick += RefreshDataTimerOnTick;
            }
            else
            {
                DumpData.IsEnabled = false;
                UpdateDumpStatus("Did not find tcadsdll.dll. Is TwinCat/ADS installed?!", Colors.Red);
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

                StateInfo stateInfo = _plcClient.ReadState();
                AdsState state = stateInfo.AdsState;
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
                _plcConnected = (state == AdsState.Run);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                _plcConnected = false;
            }
            DumpData.IsEnabled = _plcConnected;
        }

        private void GetSymbols()
        {
            foreach (ISymbol symbol in _symbolLoader.Symbols)
            {
                Tc3Symbols.AddSymbolRecursive(_symbols, symbol);
            }
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
            //Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < (int)_scrollViewer.ViewportHeight; i++)
            {
                SymbolInfo symbol = SymbolListViewItems[(int)_scrollViewer.VerticalOffset+i];
                SymbolListViewItems[(int) _scrollViewer.VerticalOffset + i].CurrentValue = Tc3Symbols.GetSymbolValue(symbol, _plcClient);
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
            if (e.Key == Key.Enter ) PopulateListView(TextBox1.Text);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
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
            
            SumSymbolRead sumSymbolRead = new SumSymbolRead(_plcClient, symbolColl);
            await Task.Run(() =>
            {
                _symbolValues = sumSymbolRead.Read();
            });
            //Debug.WriteLine($"Collecting data from PLC: {sw.Elapsed}");
        }

        private async void DumpData_OnClick(object sender, RoutedEventArgs e)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            UpdateDumpStatus("Dumping data...", Colors.AliceBlue);
            if (!_plcConnected)
            {
                UpdateDumpStatus("PLC not running", Colors.Orange);
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
                using (XmlWriter writer = XmlWriter.Create("VariableDump.xml"))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Symbols");

                    for (var i = 0; i < _symbolValues.Length; i++)
                    {
                        writer.WriteStartElement("Symbol");

                        writer.WriteElementString("Path", SymbolListViewItems[i].Path);
                        writer.WriteElementString("Type", SymbolListViewItems[i].Type);
                        writer.WriteElementString("IndexGroup", SymbolListViewItems[i].IndexGroup.ToString());
                        writer.WriteElementString("IndexOffset", SymbolListViewItems[i].IndexOffset.ToString());
                        writer.WriteElementString("Size", SymbolListViewItems[i].Size.ToString());
                        writer.WriteElementString("CurrentValue", _symbolValues[i].ToString());

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
            catch (Exception)
            {
                UpdateDumpStatus("Cannot access 'VariableDump.xml'. Opened in another application?!", Colors.Orange);
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
            catch (Exception)
            {
                UpdateDumpStatus("Cannot access 'VariableDump.csv'. Opened in another application?!", Colors.Orange);
                DumpSpinner(false);
                return;
            }

            UpdateDumpStatus("Done", Colors.GreenYellow);
            DumpSpinner(false);
            //Debug.WriteLine($"Dumping data from PLC to file: {sw.Elapsed}");
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
                TextBlockDumpStatus.Text = text;
                TextBlockDumpStatus.Foreground = new SolidColorBrush(fontColor);
            }));
        }
    }

}
