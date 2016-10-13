using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public class BattleField
    {
        protected readonly SquareStatus[,] Squares = new SquareStatus[10, 10];

        public BattleField()
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

        public virtual void SetStatusOfSquare(Square square, SquareStatus squareStatus)
        {
            SquareStatus oldStatus = this[square];
            if (squareStatus == SquareStatus.Empty)
                throw new AggregateException("You cannot set status Empty");
            if ((squareStatus == SquareStatus.Miss || squareStatus == SquareStatus.Full) 
                    && oldStatus != SquareStatus.Empty)
                throw new AggregateException("This square is already has status");
            if (squareStatus == SquareStatus.Dead && oldStatus != SquareStatus.Hurt)
                throw new AggregateException("This square is not hurt");
            if (squareStatus == SquareStatus.Hurt && 
                (oldStatus != SquareStatus.Full && oldStatus != SquareStatus.Empty))
                throw new AggregateException("This square is not full or empty");

            this[square] = squareStatus;
            if (squareStatus == SquareStatus.Hurt)
            {
                Ship ship = FindShipBySquare(square);
                var x = ship.NearSquares().ToArray();
                if (ship.NearSquares().Any(nearSquare => this[nearSquare] != SquareStatus.Empty &&
                                                         this[nearSquare] != SquareStatus.Miss))
                {
                    this[square] = oldStatus;
                    throw new AggregateException("There is a ship near this square");
                }
            }
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
                        end = new Square(i, (byte) (j + 1));
                    else
                        break;
            }
            return new Ship(start, end);
        }

        public IEnumerable<Square> GetFullSquares()
        {
            for (byte i = 0; i < 10; i++)
                for (byte j = 0; j < 10; j++)
                    if (Squares[i, j] == SquareStatus.Full)
                        yield return new Square(i, j);
        } 
    }
}
