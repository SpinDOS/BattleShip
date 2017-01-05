using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.Shared;

namespace BattleShip.DataLogic
{
    public interface IEnemyConnection : IGameConnection
    {
        /// <summary>
        /// Give up
        /// </summary>
        void GiveUp();

        /// <summary>
        /// Enemy gave up
        /// </summary>
        event EventHandler EnemyGaveUp;

        /// <summary>
        /// Get not hurt squares of enemy
        /// </summary>
        IEnumerable<Square> GetEnemyFullSquares();
    }
}
