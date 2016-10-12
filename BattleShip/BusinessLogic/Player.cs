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

        #region Properties

        protected static AggregateException GameEndedException= new AggregateException("Game ended");
        protected static AggregateException NotInitializedException
            = new AggregateException("You must initialize object by calling SetMeShotFirst");

        public event EventHandler<ShotEventArgs> MyShot;
        public event EventHandler<ShotEventArgs> EnemysShot;
        public event EventHandler<Ship> MyShipDead;
        public event EventHandler<Ship> EnemyShipDead;
        public event EventHandler<bool> GameEnded;

        protected MyBattleField MyField = null;
        protected EnemyBattleField EnemyField = null;

        public byte MyShipsAlive { get; private set; } = 10;
        public byte EnemyShipsAlive { get; private set; } = 10;

        private volatile bool _isGameEnded = false;

        public bool IsGameEnded {
            get { return _isGameEnded; }
            private set { _isGameEnded = value; }
        }

        public bool? MyTurn { get; private set; }

        public bool Initialized { get; private set; }

        #endregion

        protected Player(Field field)
        {
            MyField = new MyBattleField(field);
            EnemyField = new EnemyBattleField();
            MyShot += (sender, args) =>
            {
                EnemyField.SetStatusOfSquare(args.Square, args.SquareStatus);
                if (args.SquareStatus == SquareStatus.Dead)
                    EnemyShipDead?.Invoke(this, EnemyField.FindShipBySquare(args.Square));
            };
            EnemyShipDead += (sender, ship) =>
            {
                foreach (var innerSquare in ship.InnerSquares())
                    EnemyField.SetStatusOfSquare(innerSquare, SquareStatus.Dead);
                foreach (var nearSquare in ship.NearSquares())
                    EnemyField.SetStatusOfSquare(nearSquare, SquareStatus.Miss);
                if (--EnemyShipsAlive == 0)
                    EndGame(true);
            };
            EnemysShot += (sender, args) =>
            {
                if (args.SquareStatus == SquareStatus.Dead)
                    MyShipDead?.Invoke(this, MyField.FindShipBySquare(args.Square));
            };
            MyShipDead += (sender, ship) =>
            {
                if (--MyShipsAlive == 0)
                    EndGame(false);
            };
            GameEnded += (sender, b) =>
            {
                MyTurn = null;
                IsGameEnded = true;
            };
        }

        protected void SetMeShotFirst(bool meFirst)
        {
            if (Initialized)
                throw new AggregateException("This method must be called only once");
            MyTurn = meFirst;
            Initialized = true;
        }

        #region My shot

        public Square GetMyNextShot()
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (!Initialized)
                throw NotInitializedException;
            if (MyTurn == null || !MyTurn.Value)
                throw new AggregateException("I cannot shot now");
            Square square = GenerateNextShot();
            MyTurn = null;
            return square;
        }

        protected abstract Square GenerateNextShot();

        public void SetStatusOfMyShot(Square square, SquareStatus result)
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (!Initialized)
                throw NotInitializedException;
            if (MyTurn != null)
                throw new AggregateException("I cannot receive result of my shot now!");
            if (result == SquareStatus.Empty || result == SquareStatus.Full)
                throw new ArgumentException(nameof(result) + " must be Miss or Hurt or Dead");

            MyTurn = result != SquareStatus.Miss;
            MyShot?.Invoke(this, new ShotEventArgs(square, result));
        }

        #endregion

        #region Enemy shot

        public SquareStatus ShotFromEnemy(Square square)
        {
            if (IsGameEnded)
                throw GameEndedException;
            if (!Initialized)
                throw NotInitializedException;
            if (MyTurn == null || MyTurn.Value)
                throw new AggregateException("I cannot receive shot now");

            SquareStatus newStatus = MyField[square];
            MyTurn = newStatus == SquareStatus.Miss;
            EnemysShot?.Invoke(this, new ShotEventArgs(square, newStatus));
            return newStatus;
        }

        #endregion

        protected void EndGame(bool win)
        {
            if (IsGameEnded)
                return;
            GameEnded?.Invoke(this, win);
        }

    }
}
