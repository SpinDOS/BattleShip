using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;
using LiteNetLib.Utils;

namespace BattleShip.UserLogic
{
    public interface ICommunicationUserInterface
    {
        /// <summary>
        /// Show message to user
        /// </summary>
        /// <param name="data">array with data to send</param>
        void ShowMessage(DataContainer data);

        /// <summary>
        /// Raise when user sends message
        /// </summary>
        event EventHandler<DataContainer> UserSentMessage;
    }
}
