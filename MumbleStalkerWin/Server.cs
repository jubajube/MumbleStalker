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

    public class Server: ModelObject {
        #region Public Properties

        public string Name {
            get;
            private set;
        }

        public int ID {
            get {
                return Proxy.id();
            }
        }

        public string NumUsers {
            get {
                lock(this) {
                    if (Users == null) {
                        return "???";
                    } else {
                        return Users.Count.ToString();
                    }
                }
            }
        }

        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users {
            get {
                return _users;
            }
            set {
                _users = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("NumUsers");
                if (Users != null) {
                    Users.CollectionChanged += OnUsersCollectionChanged;
                }
            }
        }

        #endregion

        #region Public Methods

        public Server(Murmur.ServerPrx proxy, string name) {
            Proxy = proxy;
            Name = name;
            Refresh();
        }

        public void Refresh() {
            try {
                Proxy.begin_getUsers().whenCompleted(
                    users => {
                        CompleteGetUsers(users);
                    },
                    e => {
                        System.Diagnostics.Debug.WriteLine("Could not get user dictionary for {0}: {1}", Name, e.ToString());
                    }
                );
            } catch (Ice.Exception e) {
                System.Diagnostics.Debug.WriteLine("Error talking to {0}: {1}", Name, e.ToString());
                Proxy = null;
            }
        }

        private void CompleteGetUsers(Dictionary<int, Murmur.User> users) {
            if (!App.Current.Dispatcher.CheckAccess()) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => CompleteGetUsers(users)));
                return;
            }
            lock (this) {
                if (Users == null) {
                    Users = new ObservableCollection<User>();
                }
                foreach (var user in users) {
                    Users.Add(new User(user.Value.name));
                }
            }
        }

        private void OnUsersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            NotifyPropertyChanged("NumUsers");
        }

        #endregion

        #region Private Properties

        private Murmur.ServerPrx _proxy;
        private Murmur.ServerPrx Proxy {
            get {
                return _proxy;
            }
            set {
                _proxy = value;
                Users = null;
            }
        }

        #endregion
    }

}
