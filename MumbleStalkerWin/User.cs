using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MumbleStalkerWin {

    public sealed class User: ModelObject {
        #region Public Properties

        public int ID {
            get {
                return UserInfo.userid;
            }
        }

        public string Name {
            get {
                return UserInfo.name;
            }
        }

        #endregion

        #region Public Methods

        public User(Murmur.User userInfo) {
            UserInfo = userInfo.Clone() as Murmur.User;
        }

        #endregion

        #region Private Properties

        private Murmur.User UserInfo {
            get;
            set;
        }

        #endregion
    }

}
