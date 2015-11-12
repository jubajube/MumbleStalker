using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MumbleStalkerWin {

    public abstract class ModelObject: INotifyPropertyChanged {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Protected Methods

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

}
