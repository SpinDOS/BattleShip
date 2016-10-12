using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class BattleField
    {
        protected readonly SquareStatus[,] Squares = new SquareStatus[10, 10];

        protected BattleField()
        {
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                    Squares[i, j] = SquareStatus.Empty;
        }

        public SquareStatus this[Square square]
        {
            get { return Squares[square.X, square.Y]; }
            protected set { Squares[square.X, square.Y] = value; }
        }

        public Ship FindShipBySquare(Square square)
        {
            SquareStatus[] notEmpty = new[] {SquareStatus.Full, SquareStatus.Hurt, SquareStatus.Dead};
            if (!notEmpty.Contains(Squares[square.X, square.Y]))
                throw new ArgumentException("This square is not a part of ship");
            Square start = square, end = square;
            byte i = square.X, j = square.Y;
            
            if ((square.X > 0 && notEmpty.Contains(Squares[square.X - 1, square.Y])) ||
                (square.X < 9 && notEmpty.Contains(Squares[square.X + 1, square.Y]))) // vertical
            {
                for (i = square.X; i > 0; i--) // set start to top ship square
                    if (notEmpty.Contains(Squares[i - 1, j]))
                        start = new Square((byte) (i - 1), j);
                    else
                        break;

                for (i = square.X; i < 9; i++) // set end to bottom ship square
                    if (notEmpty.Contains(Squares[i + 1, j]))
                        end = new Square((byte) (i + 1), j);
                    else
                        break;
            }
            else // horizontal or one-square
            {
                i = square.X;
                for (j = square.Y; j > 0; j--) // set start to left ship square
                    if (notEmpty.Contains(Squares[i, j - 1]))
                        start = new Square(i, (byte) (j - 1));
                    else
                        break;
                for (j = square.Y; j < 9; j++) // set end to right ship square
                    if (notEmpty.Contains(Squares[i, j + 1]))
                        start = new Square(i, (byte) (j + 1));
                    else
                        break;
            }
            return new Ship(start, end);
        }

        protected void MarkShipAsDead(Ship ship)
        {
            foreach (var innerSquare in ship.InnerSquares())
                this[innerSquare] = SquareStatus.Dead;
            foreach (var nearSquare in ship.NearSquares())
                this[nearSquare] = SquareStatus.Miss;
        }

    }
}
