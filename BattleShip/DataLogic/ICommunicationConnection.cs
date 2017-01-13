using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;
using LiteNetLib.Utils;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Interface to send and receive messages
    /// </summary>
    public interface ICommunicationConnection : IDisposable
    {
        /// <summary>
        /// Send message to peer
        /// </summary>
        /// <param name="data">array with message to send</param>
        void SendMessage(DataEventArgs data);

        /// <summary>
        /// Raised on message from peer received
        /// </summary>
        event EventHandler<DataEventArgs> MessageReceived;
    }
}
