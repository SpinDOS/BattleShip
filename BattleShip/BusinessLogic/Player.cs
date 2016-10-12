using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class Player
    {
        #region Properties

        protected static AggregateException GameEndedException = new AggregateException("Game ended");
        protected static AggregateException NotInitializedException =
            new AggregateException("You must initialize the object with SetMeShotFirst(bool)");

        private SquareStatus[,] _enemyField = new SquareStatus[10, 10];
        private SquareStatus[,] _myField = new SquareStatus[10, 10];

        public SquareStatus this[Square square, bool myField] =>
            myField ? _myField[square.X, square.Y] : _enemyField[square.X, square.Y];

        public SquareStatus this[byte x, byte y, bool myField] => this[new Square(x, y), myField];

        public byte MyShipsAlive { get; private set; } = 10;
        public byte EnemyShipsAlive { get; private set; } = 10;

        public bool IsGameEnded { get; private set; } = false;

        public bool MyTurn { get; private set; }

        protected bool Initialized { get; private set; } = false;

        #endregion

        #region Initialization

        protected Player(Field field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            foreach (var square in field.ShipSquares)
                _myField[square.X, square.Y] = SquareStatus.Full;
        }

        protected void SetMeShotFirst(bool first)
        {
            if (Initialized)
                throw new AggregateException("You can use it only on initialization");
            MyTurn = first;
            Initialized = true;
        }

        #endregion

        #region Shot logic

        protected void SetStatusOfMyShot(Square square, SquareStatus result)
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (!Initialized)
                throw NotInitializedException;
            if (!MyTurn)
                throw new AggregateException("I couldnot shot now!");
            if (result == SquareStatus.Empty || result == SquareStatus.Full)
                throw new ArgumentException(nameof(result) + " must be Miss or Hurt or Dead");

            if (result == SquareStatus.Dead)
            {
                _enemyField[square.X, square.Y] = SquareStatus.Hurt; // for marking squares
                Ship ship = FindShipBySquare(square, false);
                MarkShipAsDead(ship, false);
            }

            MarkSquareWithStatus(square, result, false);

            MyTurn = result != SquareStatus.Miss;
        }

        protected SquareStatus ShotFromEnemy(Square square)
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (!Initialized)
                throw NotInitializedException;
            if (MyTurn)
                throw new AggregateException("I cannot receive shot now!");

            SquareStatus current = _myField[square.X, square.Y];
            if (current != SquareStatus.Empty && current != SquareStatus.Full)
                throw new AggregateException("Enemy has already shot at the square");

            SquareStatus newStatus = current == SquareStatus.Empty
                ? SquareStatus.Miss
                : SquareStatus.Hurt;

            MarkSquareWithStatus(square, newStatus, true);
            if (newStatus == SquareStatus.Miss)
            {
                MyTurn = true;
                return newStatus;
            }

            Ship ship = FindShipBySquare(square, true);
            if (!IsMyShipIsDead(ship))
                return newStatus;

            MarkShipAsDead(ship, true);
            return SquareStatus.Dead;
        }

        #endregion

        #region EndGame

        protected virtual void EndGame(bool b)
        {
            if (IsGameEnded)
                throw GameEndedException;
            MyTurn = false;
            IsGameEnded = true;
        }

        

        public virtual void EnemyDisconnected(bool active)
        {
            EndGame(true);
        }

        #endregion

        #region Changing square status

        protected virtual void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            SquareStatus[,] field = myField ? _myField : _enemyField;
            field[square.X, square.Y] = status;
        }

        protected virtual void MarkShipAsDead(Ship ship, bool myShip)
        {
            int min_x = ship.Start.X == 0 ? 0 : ship.Start.X - 1;
            int max_x = ship.End.X == 9 ? 9 : ship.End.X + 1;
            int min_y = ship.Start.Y == 0 ? 0 : ship.Start.Y - 1;
            int max_y = ship.End.Y == 9 ? 9 : ship.End.Y + 1;

            SquareStatus[,] field = myShip ? _myField : _enemyField;

            for (int i = min_x; i <= max_x; i++)
                for (int j = min_y; j <= max_y; j++)
                {
                    SquareStatus status = field[i, j] == SquareStatus.Hurt
                        ? SquareStatus.Dead
                        : SquareStatus.Miss;
                    MarkSquareWithStatus(new Square((byte)i, (byte)j), status, myShip);
                }

            if (myShip && --MyShipsAlive == 0)
                EndGame(false);
            else if (!myShip && --EnemyShipsAlive == 0)
                EndGame(true);
        }

        private Ship FindShipBySquare(Square square, bool myShip)
        {
            SquareStatus[,] field = myShip ? _myField : _enemyField;
            Square start = square, end = square;
            byte i = square.X, j = square.Y;
            SquareStatus[] notEmpty = new[] { SquareStatus.Full, SquareStatus.Hurt, };
            if ((square.X > 0 && notEmpty.Any(s => s == field[square.X - 1, square.Y])) ||
                (square.X < 9 && notEmpty.Any(s => s == field[square.X + 1, square.Y]))) // vertical
            {
                j = square.Y;
                for (i = square.X; i > 0; i--) // set start to top ship square
                    if (notEmpty.Any(s => s == field[i - 1, j]))
                        start = new Square((byte)(i - 1), j);
                    else
                        break;
                for (i = square.X; i < 9; i++) // set end to bottom ship square
                    if (notEmpty.Any(s => s == field[i + 1, j]))
                        end = new Square((byte)(i + 1), j);
                    else
                        break;
                return new Ship(start, end);
            }
            // horizontal or one-square
            i = square.X;
            for (j = square.Y; j > 0; j--) // set start to left ship square
                if (notEmpty.Any(s => s == field[i, j - 1]))
                    start = new Square(i, (byte)(j - 1));
                else
                    break;
            for (j = square.Y; j < 9; j++) // set end to right ship square
                if (notEmpty.Any(s => s == field[i, j + 1]))
                    start = new Square(i, (byte)(j + 1));
                else
                    break;
            return new Ship(start, end);
        }

        private bool IsMyShipIsDead(Ship ship)
        {
            return ship.InnerSquares().All(square => _myField[square.X, square.Y] != SquareStatus.Full);
        }

        #endregion
    }
}
