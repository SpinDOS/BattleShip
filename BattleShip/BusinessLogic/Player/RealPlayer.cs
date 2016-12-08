using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.DataLogic;
using BattleShip.Shared;
using BattleShip.UserLogic;

namespace BattleShip.BusinessLogic
{
    class RealPlayer : Player
    {
        private IEnemyConnection enemy;
        private IMyShotSource me;

        public event EventHandler<ShotEventArgs> ShotMade;
        public event EventHandler<Tuple<Ship, bool>> ShipDead;
        public event EventHandler<bool> GameEnd; 

        public RealPlayer(MyBattleField myField, IEnemyConnection enemy, IMyShotSource me) : base(myField)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            if (me == null)
                throw new ArgumentNullException(nameof(me));
            this.enemy = enemy;
            this.me = me;

            ShotMade += (sender, args) =>
            {
                BattleField field = args.IsMyShip ? MyField : EnemyField;
                field.SetStatusOfSquare(args.Square, args.SquareStatus);
            };

            ShipDead += (sender, tuple) =>
            {
                BattleField field = tuple.Item2 ? MyField : EnemyField;
                Ship ship = tuple.Item1;
                field.MarkShipAsDead(ship);
            };

            GameEnd += (sender, b) => IsGameEnded = true;
        }

        public void Start(CancellationToken ct)
        {
            bool myTurn = enemy.IsMeShotFirst();
            while (!ct.IsCancellationRequested && !IsGameEnded)
            {
                BattleField field = myTurn ? MyField : EnemyField;
                bool my = myTurn;
                Square square;
                SquareStatus status;
                if (myTurn)
                {
                    square = me.GetMyShot();
                    status = enemy.ShotEnemy(square);
                    myTurn = status != SquareStatus.Miss;
                }
                else
                {
                    square = enemy.GetShotFromEnemy();
                    status = MyField.GetResultOfShot(square);
                    enemy.SendStatusOfEnemysShot(square, status);
                    myTurn = status == SquareStatus.Miss;
                }

                ShotMade(this, new ShotEventArgs(square, status, !my)); // initialized in .ctor

                if (status != SquareStatus.Dead)
                    continue;

                Ship ship = field.FindShipBySquare(square);
                ShipDead.Invoke(this, Tuple.Create(ship, !my)); // initialized in .ctor

                if (my)
                {
                    if (--MyShipsAlive != 0)
                        continue;
                }
                else
                {
                    if (--EnemyShipsAlive != 0)
                        continue;
                }

                GameEnd(this, my); // initialized in .ctor
            }
        }
    }
}
