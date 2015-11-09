using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MumbleStalkerWin {

    public class MainModel: INotifyPropertyChanged {

        #region Public Properties

        private string _newServerName;
        public string NewServerName {
            get {
                return _newServerName;
            }
            set {
                _newServerName = value;
                NotifyPropertyChanged();
            }
        }

        private ObservableCollection<Server> _servers = new ObservableCollection<Server>();
        public ObservableCollection<Server> Servers {
            get {
                return _servers;
            }
        }

        #endregion

        #region Public Methods

        public MainModel() {
            try {
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                var properties = Ice.Util.createProperties(ref commandLineArgs);
                properties.setProperty("Ice.Default.EncodingVersion", "1.0");
                var initializationData = new Ice.InitializationData();
                initializationData.properties = properties;
                IceCommunicator = Ice.Util.initialize(initializationData);
            } catch (Ice.Exception e) {
                MessageBox.Show(e.ToString(), "Error initializing ICE", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshTimer.Interval = new TimeSpan(0, 0, 5);
            RefreshTimer.Tick += OnRefreshTimerTick;
            RefreshTimer.Start();
        }

        public void Add() {
            Servers.Add(new Server(IceCommunicator, NewServerName));
        }

        public void Remove(Server server) {
            Servers.Remove(server);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Methods

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnRefreshTimerTick(object sender, EventArgs e) {
            foreach (var server in Servers) {
                server.Refresh();
            }
        }

        #endregion

        #region Private Properties

        private DispatcherTimer _refreshTimer = new DispatcherTimer();
        private DispatcherTimer RefreshTimer {
            get {
                return _refreshTimer;
            }
        }

        private Ice.Communicator IceCommunicator {
            get;
            set;
        }

        #endregion
    }

}
