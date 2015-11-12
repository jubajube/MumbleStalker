using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MumbleStalkerWin {

    public sealed class Meta: ModelObject {
        #region Public Properties

        private string _name;
        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
            }
        }

        private ObservableCollection<Server> _servers = new ObservableCollection<Server>();
        public ObservableCollection<Server> Servers {
            get {
                return _servers;
            }
        }

        public string NumServers {
            get {
                if (!IsConnected) {
                    return "???";
                } else {
                    return Servers.Count.ToString();
                }
            }
        }

        public bool IsConnected {
            get {
                return (Proxy != null);
            }
        }

        #endregion

        #region Public Methods

        public Meta(Ice.Communicator ice, string name) {
            IceCommunicator = ice;
            Name = name;
            Servers.CollectionChanged += OnServersCollectionChanged;
            Refresh();
        }

        public void Refresh() {
            if (
                (IceCommunicator == null)
                || String.IsNullOrEmpty(Name)
                || (ConnectionAttempt != null)
            ) {
                return;
            }
            if (Proxy == null) {
                var endpoint = String.Format("Meta:tcp -h {0} -p 6502", Name);
                var proxy = IceCommunicator.stringToProxy(endpoint);
                ConnectionAttempt = proxy.begin_ice_getConnection();
                ConnectionAttempt.whenCompleted(
                    connection => {
                        CompleteConnection(connection, proxy);
                    },
                    e => {
                        System.Diagnostics.Debug.WriteLine("Could not connect to {0}: {1}", Name, e.ToString());
                        ConnectionAttempt = null;
                    }
                );
            }
        }

        #endregion

        #region ModelObject

        protected override void DisposeUnmanagedState() {
            ClearServers();
        }

        #endregion

        #region Private Methods

        private void CompleteConnection(Ice.Connection connection, Ice.ObjectPrx proxy) {
            if (!App.Current.Dispatcher.CheckAccess()) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => CompleteConnection(connection, proxy)));
                return;
            }
            var localAddress = (connection.getInfo() as Ice.TCPConnectionInfo).localAddress;
            var clientEndpoint = String.Format("tcp -h {0}", localAddress);
            Proxy = Murmur.MetaPrxHelper.checkedCast(proxy);
            ConnectionAttempt = null;
            try {
                foreach (var serverProxy in Proxy.getBootedServers()) {
                    var server = new Server(IceCommunicator, clientEndpoint, serverProxy, Name);
                    Servers.Add(server);
                }
            } catch (Ice.Exception e) {
                System.Diagnostics.Debug.WriteLine("Error talking to {0}: {1}", Name, e.ToString());
                Proxy = null;
            }
        }

        private void OnServersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            NotifyPropertyChanged("NumServers");
        }

        private void ClearServers() {
            foreach (var server in Servers) {
                server.Dispose();
            }
            Servers.Clear();
        }

        #endregion

        #region Private Properties

        private Ice.Communicator IceCommunicator {
            get;
            set;
        }

        private Murmur.MetaPrx _proxy;
        private Murmur.MetaPrx Proxy {
            get {
                return _proxy;
            }
            set {
                _proxy = value;
                ClearServers();
                NotifyPropertyChanged("IsConnected");
            }
        }

        private Ice.AsyncResult<Ice.Callback_Object_ice_getConnection> ConnectionAttempt {
            get;
            set;
        }

        #endregion
    }

}
