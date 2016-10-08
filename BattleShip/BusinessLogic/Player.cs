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
        private static AggregateException _gameendedException= new AggregateException("Game ended");

        private volatile bool _gameEnd = false;
        protected SquareStatus[,] Enemy = new SquareStatus[10, 10];
        protected SquareStatus[,] Me = new SquareStatus[10, 10];

        public event EventHandler<OnlyBoolEventArgs> EndGame; 
        public byte MyShipsAlive { get; protected set; }
        public byte EnemyShipsAlive { get; protected set; }

        public bool GameEnded {
            get { return _gameEnd; }
            protected set { _gameEnd = value; }
        }

        protected Player(Field field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            foreach (var square in field.ShipSquares)
                Me[square.X, square.Y] = SquareStatus.Full;
        }

        public virtual void SetStatusOfMyShot(Square square, SquareStatus result)
        {
            if (result == SquareStatus.Empty || result == SquareStatus.Full)
                throw new ArgumentException(nameof(result) + " is bad after shot");
            if (GameEnded)
                throw _gameendedException;
            if (result != SquareStatus.Dead)
            {
                MarkSquareWithStatus(square, result, false);
                return;
            }
            Enemy[square.X, square.Y] = SquareStatus.Hurt; // for marking of squares
            Ship ship = FindShipBySquare(square, false);
            MarkShipAsDead(ship, false);
        }



        public abstract Square GetMyNextShot();

        public virtual void EnemyDisconnected(bool active)
        {
            if (GameEnded)
                throw _gameendedException;
            GameEnded = true; 
            EndGame?.Invoke(this, new OnlyBoolEventArgs(true));
        }

        public virtual SquareStatus ShotFromEnemy(Square square)
        {
            if (GameEnded)
                throw _gameendedException;
            SquareStatus current = Me[square.X, square.Y];
            if (current != SquareStatus.Empty && current != SquareStatus.Full)
                throw new AggregateException("Enemy has already shot at the square");
            SquareStatus newStatus = current == SquareStatus.Empty
                ? SquareStatus.Miss
                : SquareStatus.Hurt;

            MarkSquareWithStatus(square, newStatus, true);
            if (newStatus == SquareStatus.Miss)
                return newStatus;

            Ship ship = FindShipBySquare(square, true);
            if (!IsYourShipIsDead(ship))
                return newStatus;

            MarkShipAsDead(ship, true);
            return SquareStatus.Dead;
        }

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
                EndGame?.Invoke(this, new OnlyBoolEventArgs(false));
            else if (!myShip && --EnemyShipsAlive == 0)
                EndGame?.Invoke(this, new OnlyBoolEventArgs(true));
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
                for (i = square.X; i > 0; i--)
                    if (notEmpty.Any(s => s == field[i - 1, j]))
                        start = new Square((byte)(i - 1), j);
                    else
                        break;
                for (i = square.X; i < 9; i++)
                    if (notEmpty.Any(s => s == field[i + 1, j]))
                        end = new Square((byte)(i + 1), j);
                    else
                        break;
                return new Ship(start, end);
            }
            // horizontal or one-square
            i = square.X;
            for (j = square.Y; j > 0; j--)
                if (notEmpty.Any(s => s == field[i, j - 1]))
                    start = new Square(i, (byte)(j - 1));
                else
                    break;
            for (j = square.Y; j < 9; j++)
                if (notEmpty.Any(s => s == field[i, j + 1]))
                    start = new Square(i, (byte)(j + 1));
                else
                    break;
            return new Ship(start, end);
        }

        private bool IsYourShipIsDead(Ship ship)
        {
            return ship.InnerSquares().All(square => Me[square.X, square.Y] != SquareStatus.Full);
        }
    }
}
