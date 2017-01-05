using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.DataLogic
{
    /// <summary>
    /// Interface to send and receive messages
    /// </summary>
    public interface ICommunicationConnection
    {
        /// <summary>
        /// Send message to peer
        /// </summary>
        /// <param name="mesage">message to send</param>
        void SendMessage(byte[] mesage);

        /// <summary>
        /// Raised on message from peer received
        /// </summary>
        event EventHandler<byte[]> MessageReceived;
    }
}
