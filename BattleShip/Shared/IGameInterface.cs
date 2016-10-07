using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BattleShip.Shared
{
    public interface IGameInterface
    {
        void Show();
        event EventHandler<MessageEventArgs> MessageSend;
        void MessageReceived(string message);
        void EnemyDisconnect();
        bool EnemyWantsToRestart();
        SquareStatus EnemyShot(Square square);
        event EventHandler<Square> YourShot;
        void ChangeStatusOfEnemySquare(Square square, SquareStatus status);
    }
}
