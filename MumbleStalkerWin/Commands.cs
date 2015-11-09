using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MumbleStalkerWin {

    public class Commands {

        #region Public Properties

        public static RoutedUICommand Add {
            get;
            private set;
        }

        public static RoutedUICommand Remove {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        static Commands() {
            Add = new RoutedUICommand("Add", "Add", typeof(Commands));
            Remove = new RoutedUICommand("Remove", "Remove", typeof(Commands));
        }

        #endregion

    }

}
