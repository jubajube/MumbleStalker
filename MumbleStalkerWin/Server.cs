using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Ice;

namespace MumbleStalkerWin {

    public class Server: INotifyPropertyChanged {
        #region Public Properties

        private string _server;
        public string Name {
            get {
                return _server;
            }
            set {
                _server = value;
                Meta = null;
                NotifyPropertyChanged();
                Refresh();
            }
        }

        public string Users {
            get {
                if (Meta == null) {
                    return "???";
                } else if (UserNames == null) {
                    return "0";
                } else {
                    return UserNames.Length.ToString();
                }
            }
        }

        private string[] _userNames;
        public string[] UserNames {
            get {
                return _userNames;
            }
            set {
                _userNames = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("Users");
            }
        }

        #endregion

        #region Public Methods

        public Server(Ice.Communicator ice, string name) {
            IceCommunicator = ice;
            Name = name;
        }

        public void Refresh() {
            if (
                (IceCommunicator == null)
                || String.IsNullOrEmpty(Name)
                || (ConnectionAttempt != null)
            ) {
                return;
            }
            if (Meta == null) {
                var endpoint = String.Format("Meta:tcp -h {0} -p 6502", Name);
                var proxy = IceCommunicator.stringToProxy(endpoint);
                ConnectionAttempt = proxy.begin_ice_getConnection();
                ConnectionAttempt.whenCompleted(
                    connection => {
                        Meta = Murmur.MetaPrxHelper.checkedCast(proxy);
                        ConnectionAttempt = null;
                        Refresh();
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
                UserNames = null;
                try {
                    foreach (var server in Meta.getBootedServers()) {
                        server.begin_getUsers().whenCompleted(
                            users => {
                                lock (this) {
                                    var newUserNames = from user in users
                                                       select user.Value.name;
                                    if (UserNames == null) {
                                        UserNames = newUserNames.ToArray();
                                    } else {
                                        UserNames = UserNames.Concat(newUserNames).ToArray();
                                    }
                                }
                            },
                            e => {
                                System.Diagnostics.Debug.WriteLine("Could not get user dictionary for {0}: {1}", Name, e.ToString());
                            }
                        );
                    }
                } catch (Ice.Exception e) {
                    System.Diagnostics.Debug.WriteLine("Error talking to {0}: {1}", Name, e.ToString());
                    Meta = null;
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Methods

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Private Properties

        private Ice.Communicator IceCommunicator {
            get;
            set;
        }

        private Murmur.MetaPrx _meta;
        private Murmur.MetaPrx Meta {
            get {
                return _meta;
            }
            set {
                _meta = value;
                UserNames = null;
            }
        }

        private Ice.AsyncResult<Ice.Callback_Object_ice_getConnection> ConnectionAttempt {
            get;
            set;
        }

        #endregion

    }

}
