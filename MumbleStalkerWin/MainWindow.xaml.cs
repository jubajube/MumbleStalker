using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MumbleStalkerWin {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window, INotifyPropertyChanged {
        #region Public Properties

        private Meta _selectedHost;
        public Meta SelectedHost {
            get {
                return _selectedHost;
            }
            set {
                if (SelectedHost != null) {
                    ServerList.SelectionChanged -= OnServerListSelectionChanged;
                    SelectedHost.Servers.CollectionChanged -= OnSelectedHostServersCollectionChanged;
                }
                _selectedHost = value;
                if (SelectedHost != null) {
                    ServerList.SelectionChanged += OnServerListSelectionChanged;
                    SelectedHost.Servers.CollectionChanged += OnSelectedHostServersCollectionChanged;
                }
                NotifyPropertyChanged();
            }
        }

        private Server _selectedServer;
        public Server SelectedServer {
            get {
                return _selectedServer;
            }
            set {
                _selectedServer = value;
                NotifyPropertyChanged();
            }
        }

        private void OnSelectedHostServersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (
                (SelectedServer == null)
                && (ServerList.Items.Count > 0)
            ) {
                ServerList.SelectedIndex = 0;
            }
        }

        #endregion

        #region Public Methods

        public MainWindow() {
            InitializeComponent();
            DataContext = Model;
            NewHostName.Text = Properties.Settings.Default.NewHostName;
            NewHostName.SelectionStart = NewHostName.Text.Length;
            HostList.SelectionChanged += OnHostListSelectionChanged;
        }

        private void OnHostListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (
                (SelectedHost == null)
                && (HostList.Items.Count > 0)
            ) {
                HostList.SelectedIndex = 0;
            }
            if (
                (SelectedServer == null)
                && (ServerList.Items.Count > 0)
            ) {
                ServerList.SelectedIndex = 0;
            }
        }

        private void OnServerListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (
                (SelectedServer == null)
                && (ServerList.Items.Count > 0)
            ) {
                ServerList.SelectedIndex = 0;
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

        private void OnAddHost(object sender, ExecutedRoutedEventArgs e) {
            bool firstHost = (Model.Hosts.Count == 0);
            Model.Add(NewHostName.Text);
            if (firstHost) {
                HostList.SelectedIndex = 0;
                if (ServerList.Items.Count > 0) {
                    ServerList.SelectedIndex = 0;
                }
            }
        }

        private void OnRemoveHost(object sender, ExecutedRoutedEventArgs e) {
            Model.Remove(e.Parameter as Meta);
        }

        private void OnNewHostNameTextChanged(object sender, TextChangedEventArgs e) {
            Properties.Settings.Default.NewHostName = NewHostName.Text;
            Properties.Settings.Default.Save();
        }

        private void OnWindowClosed(object sender, EventArgs e) {
            Model.Dispose();
        }

        #endregion

        #region Private Properties

        private MainModel _model = new MainModel();
        private MainModel Model {
            get {
                return _model;
            }
        }

        #endregion
    }
}
