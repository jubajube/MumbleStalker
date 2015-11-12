using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ice;
using Murmur;

namespace MumbleStalkerWin {

    public sealed class ServerCallback: Murmur.ServerCallbackDisp_ {
        #region Public Methods

        public ServerCallback(Server server) {
            Server = server;
        }

        #endregion

        #region Murmur.ServerCallbackDisp_

        public override void channelCreated(Channel state, Current current__) {
            System.Diagnostics.Debug.Print("Channel created: {0}", state.name);
        }

        public override void channelRemoved(Channel state, Current current__) {
            System.Diagnostics.Debug.Print("Channel removed: {0}", state.name);
        }

        public override void channelStateChanged(Channel state, Current current__) {
            System.Diagnostics.Debug.Print("Channel changed: {0}", state.name);
        }

        public override void userConnected(Murmur.User state, Current current__) {
            Server?.OnUserConnected(state);
            System.Diagnostics.Debug.Print("User connected: {0}", state.name);
        }

        public override void userDisconnected(Murmur.User state, Current current__) {
            Server?.OnUserDisconnected(state);
            System.Diagnostics.Debug.Print("User disconnected: {0}", state.name);
        }

        public override void userStateChanged(Murmur.User state, Current current__) {
            System.Diagnostics.Debug.Print("User changed: {0}", state.name);
        }

        public override void userTextMessage(Murmur.User state, TextMessage message, Current current__) {
            System.Diagnostics.Debug.Print("User {0} send text message: {1}", state.name, message.text);
        }

        #endregion

        #region Private Properties

        private Server Server {
            get;
            set;
        }

        #endregion
    }

}
