using System;
using System.Collections.Generic;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    public interface IPlayerInterface
    {
        void Start(BattlefieldExtensions clearField);
        Square GetMyShot();
        void MarkEnemySquareWithStatus(Square square, SquareStatus status);
        void MarkMySquareWithStatus(Square square, SquareStatus status);
        void ShowGameEnd(bool win);
        void ShowInfo(string info, bool blockInterface);
        event EventHandler InterfaceClose;
    }
}
