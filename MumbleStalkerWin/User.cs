using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MumbleStalkerWin {

    public class User: ModelObject {
        #region Public Properties

        public string Name {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public User(string name) {
            Name = name;
        }

        #endregion
    }

}
