using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class Game
    {
        #region Properties

        protected SquareStatus[,] Enemy = new SquareStatus[10, 10];
        protected SquareStatus[,] Me = new SquareStatus[10, 10];

        public SquareStatus this[bool isYourField, byte x, byte y] => this[isYourField, new Square(x, y)];
        public SquareStatus this[bool isYourField, Square square] => 
            isYourField? Me[square.X, square.Y]: Enemy[square.X, square.Y];

        protected IGameInterface UserInterface = null;

        #endregion

        protected Game(Field field, IGameInterface userInterface)
        {
            if (userInterface == null)
                throw new ArgumentNullException(nameof(userInterface));
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            foreach (var square in field.ShipSquares)
                Me[square.X, square.Y] = SquareStatus.Full;
        }

    }
}
