using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace BattleShip.UserLogic
{
    public interface IGameUserPvpInterface : IGameUserInterface
    {
        /// <summary>
        /// Notificate user that enemy gave up
        /// </summary>
        void ShowEnemyGaveUp();

        /// <summary>
        /// Notificate user that enemy disconnected
        /// </summary>
        /// <param name="reason">reason of disconnect</param>
        void ShowEnemyDisconnected(DisconnectReason reason);

        /// <summary>
        /// Ask user if he wants to keep connection with this enemy
        /// </summary>
        /// <returns>true, if user wants to keep connection</returns>
        bool AskIfKeepConnection();

    }
}
