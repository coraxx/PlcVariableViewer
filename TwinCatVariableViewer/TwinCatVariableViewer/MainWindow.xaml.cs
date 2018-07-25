using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
        private TcAdsClient _plcClient;
        private ISymbolLoader _symbolLoader;
        private readonly List<ISymbol> _symbols = new List<ISymbol>();
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
            for (int i = 0; i < (int)_scrollViewer.ViewportHeight; i++)
            {
                SymbolInfo symbol = SymbolListViewItems[(int)_scrollViewer.VerticalOffset+i];
                SymbolListViewItems[(int) _scrollViewer.VerticalOffset + i].CurrentValue = Tc3Symbols.GetSymbolValue(symbol, _plcClient);
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
    }

}
