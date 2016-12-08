using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    public interface IEnemyConnectionWithPeople : IEnemyConnection
    {
        bool Connected { get; }

        bool GetMeShotFirst();

        byte[] ReceiveData();
        void SendData(byte[] data);

        event EventHandler EnemyAsksToRestart;
        void AnswerEnemyForRestartRequest(bool accept);
        bool AskEnemyToRestart();

        void SendEnemyFullSquares(IEnumerable<Square> FullSquares);

        event EventHandler EnemyDisconnected;
    }
}
