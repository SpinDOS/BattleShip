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
    public sealed class CheaterPlayerSimulator : LogicalPlayerSimulator
    {
        private Square[] EnemySquares;

        /// <summary>
        /// Initialize field with random ships
        /// </summary>
        public CheaterPlayerSimulator(IEnumerable<Square> enemySquares) 
            : this(new MyBattleField(BattlefieldExtensions.RandomizeSquares()), enemySquares)
        { }

        /// <summary>
        /// Initialize field with param squares
        /// </summary>
        /// <param name="myField">squares of ships</param>
        public CheaterPlayerSimulator(MyBattleField myField, IEnumerable<Square> enemySquares)
            : base(myField)
        {
            if (enemySquares == null)
                throw new ArgumentNullException(nameof(enemySquares));
            EnemySquares = enemySquares.ToArray();
            if (EnemySquares.Length != 20)
                throw new ArgumentException("enemySquares does not contain all enemy's squares");
        }

        /// <summary>
        /// Generates next shot with knowledge about enemy's ships
        /// </summary>
        /// <returns>Returns enemy's ship square(33%) or random square(67%)</returns>
        protected override Square GetNewSquare()
        {
            Random rnd = new Random();
            Square square;
            // return enemy's ship square - 33%
            if (rnd.Next(3) == 0)
            {
                while (true)
                {
                    square = EnemySquares[rnd.Next(20)];
                    if (EnemyField[square] == SquareStatus.Empty)
                        return square;
                }
            }

            // return random square - 67%
            return base.GetNewSquare();
        }
    }
}
