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

        protected static AggregateException GameEndedException= new AggregateException("Game ended");

        protected MyBattleField MyField = null;
        protected EnemyBattleField EnemyField = null;

        public byte MyShipsAlive { get; protected set; } = 10;
        public byte EnemyShipsAlive { get; protected set; } = 10;

        public bool IsGameEnded { get; protected set; }

        public bool MyTurn { get; protected set; } // сделать приватным

        #endregion

        protected Player(Field field)
        {
            MyField = new MyBattleField(field);
            EnemyField = new EnemyBattleField();
        }

        #region My shot

        protected Square GetMyNextShot()
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (!MyTurn)
                throw new AggregateException("It's not my turn to shot");
            Square square = GenerateNextShot();
            MyTurn = false;
            return square;
        }

        protected abstract Square GenerateNextShot();

        protected virtual void SetStatusOfMyShot(Square square, SquareStatus result)
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (MyTurn)
                throw new AggregateException("I must shot now!");
            if (result == SquareStatus.Empty || result == SquareStatus.Full)
                throw new ArgumentException(nameof(result) + " must be Miss or Hurt or Dead");

            EnemyField.SetStatusOfSquare(square, result);
            MyTurn = result != SquareStatus.Miss;
        }

        #endregion

        #region Enemy shot

        protected virtual SquareStatus ShotFromEnemy(Square square)
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (MyTurn)
                throw new AggregateException("I must shot now!");
            SquareStatus current = Me[square.X, square.Y];
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

        protected virtual void EndGame(bool win)
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

        #region Marking squares

        protected virtual void MarkSquareWithStatus(Square square, SquareStatus status, bool myField)
        {
            SquareStatus[,] field = myField ? Me : Enemy;
            field[square.X, square.Y] = status;
        }
        private void MarkShipAsDead(Ship ship, bool myShip)
        {
            int min_x = ship.Start.X == 0 ? 0 : ship.Start.X - 1;
            int max_x = ship.End.X == 9 ? 9 : ship.End.X + 1;
            int min_y = ship.Start.Y == 0 ? 0 : ship.Start.Y - 1;
            int max_y = ship.End.Y == 9 ? 9 : ship.End.Y + 1;

            SquareStatus[,] field = myShip ? Me : Enemy;

            for (int i = min_x; i <= max_x; i++)
                for (int j = min_y; j <= max_y; j++)
                {
                    SquareStatus status = field[i, j] == SquareStatus.Hurt
                        ? SquareStatus.Dead
                        : SquareStatus.Miss;
                    MarkSquareWithStatus(new Square((byte) i, (byte) j), status, myShip);
                }

            if (myShip && --MyShipsAlive == 0)
                EndGame(false);
            else if (!myShip && --EnemyShipsAlive == 0)
                EndGame(true);
        }

        private Ship FindShipBySquare(Square square, bool myShip)
        {
            SquareStatus[,] field = myShip ? Me : Enemy;
            Square start = square, end = square;
            byte i = square.X, j = square.Y;
            SquareStatus[] notEmpty = new[] {SquareStatus.Full, SquareStatus.Hurt,};
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
            return ship.InnerSquares().All(square => Me[square.X, square.Y] != SquareStatus.Full);
        }

        #endregion
    }
}
