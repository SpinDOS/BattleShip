using System;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    public interface IPlayerInterface
    {
        void Start(Field field);
        Square GetMyShot();
        void MarkSquareWithStatus(Square square, SquareStatus status, bool yourField);
        event EventHandler InterfaceClose;
    }
}
