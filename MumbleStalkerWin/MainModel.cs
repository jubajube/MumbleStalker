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

    public class MainModel: ModelObject {

        #region Public Properties

        private ObservableCollection<Meta> _hosts = new ObservableCollection<Meta>();
        public ObservableCollection<Meta> Hosts {
            get {
                return _hosts;
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

        public void Add(string newHostName) {
            Hosts.Add(new Meta(IceCommunicator, newHostName));
        }

        public void Remove(Meta host) {
            Hosts.Remove(host);
        }

        #endregion

        #region Private Methods

        private void OnRefreshTimerTick(object sender, EventArgs e) {
            foreach (var host in Hosts) {
                host.Refresh();
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
