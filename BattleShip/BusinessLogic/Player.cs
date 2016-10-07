using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class Player
    {
        protected SquareStatus[,] Enemy = new SquareStatus[10, 10];
        protected SquareStatus[,] Me = new SquareStatus[10, 10];
        protected Player(Field field)
        {
            foreach (var square in field.ShipSquares)
                Me[square.X, square.Y] = SquareStatus.Full;
        }

        protected abstract SquareStatus ShotEnemy(Square square);
        protected abstract Square NextShot();

        public virtual SquareStatus ShotFromEnemy(Square square)
        {
            SquareStatus current = Me[square.X, square.Y];
            if (current != SquareStatus.Empty && current != SquareStatus.Full)
                throw new AggregateException("Enemy has already shot at the square");
            SquareStatus newStatus = current == SquareStatus.Empty
                ? SquareStatus.Miss
                : SquareStatus.Hurt;
            if (newStatus == SquareStatus.Miss)
                return SquareStatus.Miss;

            Ship ship = FindShipBySquare(square);
            if (!IsShipIsDead(ship))
                return newStatus;

            MarkShipAsDead(ship, Me);
            return SquareStatus.Dead;
        }

        protected virtual void MarkShipAsDead(Ship ship, SquareStatus[,] field)
        {
            int min_x = ship.Start.X == 0 ? 0 : ship.Start.X - 1;
            int max_x = ship.Start.X == 9 ? 9 : ship.Start.X + 1;
            int min_y = ship.Start.Y == 0 ? 0 : ship.Start.Y - 1;
            int max_y = ship.Start.Y == 9 ? 9 : ship.Start.Y + 1;
            for (int i = min_x; i <= max_x; i++)
                for (int j = min_y; j <= max_y; j++)
                    field[i, j] = field[i, j] == SquareStatus.Hurt
                        ? SquareStatus.Dead
                        : SquareStatus.Miss;
        }

        protected Ship FindShipBySquare(Square square)
        {
            Square start = square, end = square;
            byte i = square.X, j = square.Y;
            SquareStatus[] notEmpty = new[] {SquareStatus.Full, SquareStatus.Hurt,};
            if ((square.X > 0 && notEmpty.Any(s => s == Me[square.X - 1, square.Y])) ||
                (square.X < 9 && notEmpty.Any(s => s == Me[square.X + 1, square.Y]))) // vertical
            {
                j = square.Y;
                for (i = square.X; i > 0; i--)
                    if (!notEmpty.Any(s => s == Me[i - 1, j]))
                    {
                        start = new Square(i, j);
                        break;
                    }
                for (i = square.X; i < 9; i++)
                    if (!notEmpty.Any(s => s == Me[i + 1, j]))
                    {
                        end = new Square(i, j);
                        break;
                    }
                return new Ship(start, end);
            }
            // horizontal or one-square
            i = square.X;
            for (j = square.Y; j > 0; j--)
                if (!notEmpty.Any(s => s == Me[i, j - 1]))
                {
                    start = new Square(i, j);
                    break;
                }
            for (j = square.Y; j < 9; j++)
                if (!notEmpty.Any(s => s == Me[i, j + 1]))
                {
                    end = new Square(i, j);
                    break;
                }
            return new Ship(start, end);
        }

        protected bool IsShipIsDead(Ship ship)
        {
            return ship.InnerSquares().All(square => Me[square.X, square.Y] != SquareStatus.Full);
        }
    }
}
