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
        /// <returns>Returns enemy's ship square(20%) or random square(80%)</returns>
        protected override Square GetNewSquare()
        {
            Random rnd = new Random();
            Square square;
            // return enemy's ship square - 33%
            if (rnd.Next(5) == 0)
            {
                int start, i;
                start = i = rnd.Next(EnemySquares.Length);
                while (EnemyField[EnemySquares[i]] != SquareStatus.Empty)
                {
                    if (++i == EnemySquares.Length)
                        i = 0;
                    if (i == start)
                        throw new AggregateException("All enemy squares have been shot");
                }
                return EnemySquares[i];
            }

            // return random square - 80%
            return base.GetNewSquare();
        }
    }
}
