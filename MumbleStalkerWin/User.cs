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

        #region Object

        public override bool Equals(object obj) {
            var other = obj as User;
            if (other == null) {
                return false;
            }
            if (ID != other.ID) {
                return false;
            }
            if (Name != other.Name) {
                return false;
            }
            return true;
        }

        public override int GetHashCode() {
            return ID.GetHashCode() + Name.GetHashCode();
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
