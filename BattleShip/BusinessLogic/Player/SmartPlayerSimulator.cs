using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Simulate human logic with smart new ship search
    /// </summary>
    public sealed class SmartPlayerSimulator : LogicalPlayerSimulator
    {
        /// <summary>
        /// Initialize field with random ships
        /// </summary>
        public SmartPlayerSimulator() : base() { }

        /// <summary>
        /// Initialize field with param squares
        /// </summary>
        /// <param name="myField">squares of ships</param>
        public SmartPlayerSimulator(MyBattleField myField)
            : base(myField) { }

        /// <summary>
        /// Get new square when there is no info about hurt ships
        /// </summary>
        /// <returns>New square to shot</returns>
        protected override Square GetNewSquare()
        {
            // get empty square
            var squares = EnemyField.GetEmptySquares();
            if (!squares.Any())
                throw new GameStateException("No empty squares in enemy field");

            // find max rating
            int max = squares.Max(Rating);
            // get empty square with max rating
            var goodSquares = squares.Where(s => Rating(s) == max).ToArray();
            // get random element from good squares
            return goodSquares[new Random().Next(goodSquares.Length)];
        }

        // find rating of square 
        // watch count of empty squares in every direction
        // max count - 3 as max ship length=4 (1 square is argument)
        private int Rating(Square square)
        {
            int rating = 0;
            byte x = square.X, y = square.Y;

            // move up 3 times or until not empty
            for (byte i = x; i > 0 && x - i < 3; )
                if (EnemyField[new Square(--i, y)] == SquareStatus.Empty)
                    rating++;
                else
                    break;

            // move down 3 times or until not empty
            for (byte i = x; i < 9 && i - x < 3; )
                if (EnemyField[new Square(++i, y)] == SquareStatus.Empty)
                    rating++;
                else
                    break;

            // move left 3 times or until not empty
            for (byte i = y; i > 0 && y - i < 3; )
                if (EnemyField[new Square(x, --i)] == SquareStatus.Empty)
                    rating++;
                else
                    break;

            // move right 3 times or until not empty
            for (byte i = y; i < 9 && i - y < 3; )
                if (EnemyField[new Square(x, ++i)] == SquareStatus.Empty)
                    rating++;
                else
                    break;

            return rating;
        }
    }
}
