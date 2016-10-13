using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    public abstract class SimulatedPlayer : Player
    {
        private readonly PVEConnection _pveConnection = null;

        protected SimulatedPlayer(ClearField clearField) : base(clearField)
        {_pveConnection = new PVEConnection(this); }

        public IEnemyConnection GetConnectionToMe() => _pveConnection;

        private class PVEConnection : IEnemyConnection
        {
            private SimulatedPlayer me;

            public PVEConnection(SimulatedPlayer player)
            {
                if (player == null)
                    throw new NullReferenceException(nameof(player));
                me = player;
            }

            public void SetEnemyShotFirst(bool enemyFirst)
            { me.SetMeShotFirst(enemyFirst);}

            public Square GetShotFromEnemy()
            { return me.GetMyNextShot(); }

            public void SendStatusOfEnemysShot(Square square, SquareStatus result)
            { me.SetStatusOfMyShot(square, result);}

            public SquareStatus ShotEnemy(Square square)
            { return me.ShotFromEnemy(square); }

            public void Disconnect()
            { me.EndGame(true); }

            public IEnumerable<Square> GetEnemyFullSquares()
            {
                if (!me.IsGameEnded)
                    throw new AggregateException("You can call it only after game end");
                return me.MyField.GetFullSquares();
            } 
        }

    }
}
