using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    interface IEnemyConnectionWithPeople : IEnemyConnection
    {
        event EventHandler<MessageEventArgs> MessageReceived;
        void SendMessage(string message);
        event EventHandler EnemyAsksToRestart;
        void AnswerEnemyForRestartRequest(bool accept);

        event EventHandler EnemyDisconnected;
    }
}
