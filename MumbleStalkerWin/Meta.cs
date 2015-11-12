using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MumbleStalkerWin {

    public class Meta: ModelObject {
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
                lock(this) {
                    if (Proxy == null) {
                        return "???";
                    } else {
                        return Servers.Count.ToString();
                    }
                }
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
                        CompleteConnection(proxy);
                    },
                    e => {
                        System.Diagnostics.Debug.WriteLine("Could not connect to {0}: {1}", Name, e.ToString());
                        ConnectionAttempt = null;
                    }
                );
            } else {
                // TODO: Rather than poll an already-established connection,
                // we could use addCallback on each server in order to
                // update the list only when there are actual changes...
                lock(this) {
                    Servers.Clear();
                    try {
                        foreach (var serverProxy in Proxy.getBootedServers()) {
                            var server = new Server(serverProxy, Name);
                            Servers.Add(server);
                        }
                    } catch (Ice.Exception e) {
                        System.Diagnostics.Debug.WriteLine("Error talking to {0}: {1}", Name, e.ToString());
                        Proxy = null;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void CompleteConnection(Ice.ObjectPrx proxy) {
            if (!App.Current.Dispatcher.CheckAccess()) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => CompleteConnection(proxy)));
                return;
            }
            Proxy = Murmur.MetaPrxHelper.checkedCast(proxy);
            ConnectionAttempt = null;
            Refresh();
        }

        private void OnServersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            NotifyPropertyChanged("NumServers");
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
                Servers.Clear();
            }
        }

        private Ice.AsyncResult<Ice.Callback_Object_ice_getConnection> ConnectionAttempt {
            get;
            set;
        }

        #endregion
    }

}
