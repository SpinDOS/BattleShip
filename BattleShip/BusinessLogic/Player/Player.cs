using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using BattleShip.DataLogic;
using BattleShip.Shared;

namespace BattleShip.BusinessLogic
{
    /// <summary>
    /// Real player or simulated player
    /// </summary>
    public abstract class Player
    {
        /// <summary>
        /// Id for identifying this object for battlefields
        /// </summary>
        protected readonly BattleField.Identifier myId = new BattleField.Identifier();

        /// <summary>
        /// My field 
        /// </summary>
        public MyBattleField MyField { get; }

        /// <summary>
        /// Enemy field
        /// </summary>
        public EnemyBattleField EnemyField { get; }

        /// <summary>
        /// Create player with field and owns it
        /// </summary>
        protected Player(MyBattleField myField)
        {
            if (myField == null)
                throw new ArgumentNullException(nameof(myField));
            MyField = myField;
            MyField.SetOwner(this, myId);
            EnemyField = new EnemyBattleField();
            EnemyField.SetOwner(this, myId);
        }

        private bool _isGameEnded = false;

        /// <summary>
        /// True, if game has ended
        /// </summary>
        public bool IsGameEnded
        {
            get { return _isGameEnded; }
            protected set // only false -> true
            {
                if (!value)
                    throw new ArgumentException("Can't set game end to false");
                _isGameEnded = true;
            }
        }

        /// <summary>
        /// Confirm id for battlefield request
        /// </summary>
        public bool ConfirmId(BattleField.Identifier id) => ReferenceEquals(id, myId);

        /// <summary>
        /// End game if enemy or me gave up
        /// </summary>
        public virtual void ForceEndGame(bool win)
        {
            if (IsGameEnded)
                throw new AggregateException("Game ended");
            IsGameEnded = true;
        }
    }
}
