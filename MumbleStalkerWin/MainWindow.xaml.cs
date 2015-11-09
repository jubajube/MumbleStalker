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
        }

        #endregion

        #region Private Methods

        private void OnAdd(object sender, ExecutedRoutedEventArgs e) {
            Model.Add();
        }

        private void OnRemove(object sender, ExecutedRoutedEventArgs e) {
            Model.Remove(e.Parameter as Server);
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
