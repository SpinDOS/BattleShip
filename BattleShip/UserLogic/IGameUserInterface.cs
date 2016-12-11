using System;
using System.Collections.Generic;
using BattleShip.BusinessLogic;
using BattleShip.Shared;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interface for providing info to user
    /// </summary>
    public interface IGameUserInterface : IMyShotSource
    {
        /// <summary>
        /// Start game
        /// </summary>
        /// <param name="shipSquares">Squares of my ships</param>
        void Start(IEnumerable<Square> shipSquares);

        /// <summary>
        /// Mark square in enemy field
        /// </summary>
        void MarkEnemySquareWithStatus(Square square, SquareStatus status);

        /// <summary>
        /// Mark square in my field
        /// </summary>
        void MarkMySquareWithStatus(Square square, SquareStatus status);

        /// <summary>
        /// Provide any info to user
        /// </summary>
        /// <param name="blockInterface">if true, block ability to shoot</param>
        void ShowInfo(string info, bool blockInterface);

        /// <summary>
        /// Show info about game end
        /// </summary>
        /// <param name="win">true, if i win</param>
        void ShowGameEnd(bool win);

        /// <summary>
        /// Discovers enemy's ships
        /// </summary>
        /// <param name="fullSquares">enemy's ships squares</param>
        void ShowEnemyFullSquares(IEnumerable<Square> fullSquares);

        /// <summary>
        /// Trigger when player wants to give up
        /// </summary>
        event EventHandler GiveUp;

        /// <summary>
        /// Trigger when user closes window
        /// </summary>
        event EventHandler InterfaceForceClose;
    }
}
