using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class MainWindow: Window {
        #region Public Methods

        public MainWindow() {
            InitializeComponent();
            DataContext = Model;
            NewHostName.Text = Properties.Settings.Default.NewHostName;
            NewHostName.SelectionStart = NewHostName.Text.Length;
        }

        #endregion

        #region Private Methods

        private void OnAddHost(object sender, ExecutedRoutedEventArgs e) {
            Model.Add(NewHostName.Text);
        }

        private void OnRemoveHost(object sender, ExecutedRoutedEventArgs e) {
            Model.Remove(e.Parameter as Meta);
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

        private void OnNewHostNameTextChanged(object sender, TextChangedEventArgs e) {
            Properties.Settings.Default.NewHostName = NewHostName.Text;
            Properties.Settings.Default.Save();
        }
    }
}
