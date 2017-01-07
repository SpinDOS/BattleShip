using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib.Utils;

namespace BattleShip.UserLogic
{
    public interface ICommunicationUserInterface
    {
        /// <summary>
        /// Show message to user
        /// </summary>
        /// <param name="reader">NetDataReader with message to show</param>
        void ShowMessage(NetDataReader reader);

        /// <summary>
        /// Raise when user sends message
        /// </summary>
        event EventHandler<NetDataWriter> UserSentMessage;
    }
}
