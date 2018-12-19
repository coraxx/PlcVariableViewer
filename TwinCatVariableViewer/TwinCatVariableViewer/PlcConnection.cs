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
        #region Variables

        private AmsAddress _address;
        private readonly SymbolLoaderSettings _symbolLoaderSettings = new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree, ValueAccessMode.IndexGroupOffsetPreferred);
        private ISymbolLoader _symbolLoader;

        #endregion

        #region Properties

        public AdsSession Session { get; private set; }
        public AdsConnection Connection { get; private set; }
        public List<ISymbol> Symbols { get; private set; } = new List<ISymbol>();

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

        #endregion

        #region Events

        public event PlcConnectionErrorEventHandler PlcConnectionError;

        #endregion

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="address">PLC AMS address</param>
        public PlcConnection(AmsAddress address)
        {
            _address = address;
        }

        /// <summary>
        /// Connect to PLC
        /// </summary>
        public void Connect()
        {
            // If already connected, disconnect first
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
                string port = (Session == null) ? String.Empty : $"Port {Session.Port}: ";
                OnPlcConnectionError(new PlcConnectionErrorEventArgs($"{port}{ex.Message}"));
                Disconnect();
            }
        }

        /// <summary>
        /// Wrapper for disconnecting from PLC
        /// </summary>
        public void Disconnect()
        {
            Connected = false;
            Connection.Disconnect();
        }

        /// <summary>
        /// Get symbols (variables) from PLC
        /// </summary>
        private void GetSymbols()
        {
            Symbols.Clear();
            foreach (ISymbol symbol in _symbolLoader.Symbols)
            {
                Tc3Symbols.AddSymbolRecursive(Symbols, symbol);
            }
        }

        protected virtual void OnPlcConnectionError(PlcConnectionErrorEventArgs e)
        {
            PlcConnectionError?.Invoke(this, e);
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
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
