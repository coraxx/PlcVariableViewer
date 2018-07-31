using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.Ads.ValueAccess;
using TwinCAT.TypeSystem;

namespace TwinCatVariableViewer
{
    public class PlcConnection : INotifyPropertyChanged
    {
        private AmsAddress _address;
        public AdsSession Session { get; private set; }
        public AdsConnection Connection { get; private set; }

        private readonly SymbolLoaderSettings _symbolLoaderSettings = new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree, ValueAccessMode.IndexGroupOffsetPreferred);
        private ISymbolLoader _symbolLoader;
        public List<ISymbol> Symbols { get; private set; } = new List<ISymbol>();

        #region Events

        public event PlcConnectionErrorEventHandler PlcConnectionError;

        #endregion

        private bool _connected;
        public bool Connected
        {   
            get { return _connected; }
            private set
            {
                _connected = value;
                OnPropertyChanged("Connected");
            }
        }

        public PlcConnection(AmsAddress address)
        {
            _address = address;
        }

        public void Connect()
        {
            if (Session != null && Connection.ConnectionState == ConnectionState.Connected)
            {
                Disconnect();
            }

            try
            {
                Session?.Dispose();
                Session = new AdsSession(new AmsAddress(_address), SessionSettings.Default);
                Connection = (AdsConnection)Session.Connect();

                _symbolLoader = SymbolLoaderFactory.Create(Connection, _symbolLoaderSettings);

                StateInfo stateInfo = Connection.ReadState();
                AdsState state = stateInfo.AdsState;
                if (state == AdsState.Run || state == AdsState.Stop)
                {
                    Connected = true;
                    GetSymbols();
                }
                else
                {
                    Disconnect();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                OnPlcConnectionError(new PlcConnectionErrorEventArgs(ex.Message));
            }
        }

        public void Disconnect()
        {
            Connected = false;
            Connection.Disconnect();
        }

        private void GetSymbols()
        {
            Symbols.Clear();
            foreach (ISymbol symbol in _symbolLoader.Symbols)
            {
                Tc3Symbols.AddSymbolRecursive(Symbols, symbol);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPlcConnectionError(PlcConnectionErrorEventArgs e)
        {
            PlcConnectionError?.Invoke(this, e);
        }
    }

    public delegate void PlcConnectionErrorEventHandler(object sender, PlcConnectionErrorEventArgs e);

    public class PlcConnectionErrorEventArgs : EventArgs
    {
        public PlcConnectionErrorEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
