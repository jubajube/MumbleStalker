using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Ice;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace MumbleStalkerWin {

    public sealed class Server: ModelObject {
        #region Public Properties

        public string Name {
            get;
            private set;
        }

        public int ID {
            get {
                // NOTE: id() requires a round-trip transaction with the
                // server and so may block; consider using begin_id instead.
                return ServerProxy.id();
            }
        }

        public int NumUsers {
            get {
                return Users.Count;
            }
        }

        private ObservableCollection<User> _users = new ObservableCollection<User>();
        public ObservableCollection<User> Users {
            get {
                return _users;
            }
        }

        #endregion

        #region Public Methods

        public Server(Ice.Communicator iceCommunicator, string clientEndpoint, Murmur.ServerPrx proxy, string name) {
            Users.CollectionChanged += OnUsersCollectionChanged;
            ServerProxy = proxy;
            Name = name;
            try {
                var servant = new ServerCallback(this);
                var adapter = iceCommunicator.createObjectAdapterWithEndpoints("", clientEndpoint);
                var servantProxy = adapter.addWithUUID(servant);
                ServerCallbackProxy = Murmur.ServerCallbackPrxHelper.checkedCast(servantProxy);
                adapter.activate();

                // TODO: Allow user to provide Ice secret
                var context = new Dictionary<string, string>();
                context["secret"] = "";

                ServerProxy.ice_getConnection().setAdapter(adapter);
                ServerProxy.addCallback(ServerCallbackProxy, context);
                ServerProxy.begin_getUsers().whenCompleted(
                    users => {
                        CompleteGetUsers(users);
                    },
                    e => {
                        System.Diagnostics.Debug.WriteLine("Could not get user dictionary for {0}: {1}", Name, e.ToString());
                    }
                );
            } catch (Ice.Exception e) {
                System.Diagnostics.Debug.WriteLine("Error talking to {0}: {1}", Name, e.ToString());
            }
        }

        public void OnUserConnected(Murmur.User user) {
            if (!App.Current.Dispatcher.CheckAccess()) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => OnUserConnected(user)));
                return;
            }
            CheckedAddUser(user);
        }

        public void OnUserDisconnected(Murmur.User user) {
            if (!App.Current.Dispatcher.CheckAccess()) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => OnUserDisconnected(user)));
                return;
            }
            CheckedRemoveUser(user);
        }

        #endregion

        #region ModelObject

        protected override void DisposeUnmanagedState() {
            ServerProxy?.removeCallback(ServerCallbackProxy);
        }

        #endregion

        #region Private Methods

        private void CompleteGetUsers(Dictionary<int, Murmur.User> users) {
            if (!App.Current.Dispatcher.CheckAccess()) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => CompleteGetUsers(users)));
                return;
            }
            foreach (var user in users) {
                CheckedAddUser(user.Value);
            }
        }

        private void CheckedAddUser(Murmur.User user) {
            var newUser = new User(user);
            if (Users.Where(existingUser => existingUser == newUser).Count() == 0) {
                Users.Add(newUser);
            }
        }

        private void CheckedRemoveUser(Murmur.User user) {
            var oldUser = new User(user);
            for (int i = 0; i < Users.Count; ++i) {
                if (Users[i] == oldUser) {
                    Users.RemoveAt(i);
                    break;
                }
            }
        }

        private void OnUsersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            NotifyPropertyChanged("NumUsers");
        }

        #endregion

        #region Private Properties

        private Murmur.ServerPrx ServerProxy {
            get;
            set;
        }

        private Murmur.ServerCallbackPrx ServerCallbackProxy {
            get;
            set;
        }

        #endregion
    }

}
