using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MumbleStalkerWin {

    public abstract class ModelObject: INotifyPropertyChanged, IDisposable {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IDisposable

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void DisposeManagedState() {
        }

        protected virtual void DisposeUnmanagedState() {
        }

        #endregion

        #region Finalizer

        ~ModelObject() {
            Dispose(false);
        }

        #endregion

        #region Private Methods

        private void Dispose(bool disposing) {
            if (!Disposed) {
                if (disposing) {
                    DisposeManagedState();
                }
                DisposeUnmanagedState();
                Disposed = true;
            }
        }

        #endregion

        #region Private Properties

        private bool Disposed {
            get;
            set;
        }

        #endregion
    }

}
