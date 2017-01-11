using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShip.Shared
{
    /// <summary>
    /// Exception is thrown when game has ended due to give up request
    /// </summary>
    public class GiveUpException : AggregateException
    {
        public GiveUpException(string message)
            : base(message) { }
    }

    /// <summary>
    /// Exception is thrown when game condition does not allow to use called method
    /// </summary>
    public class GameStateException : AggregateException
    {
        public GameStateException(string message)
            : base(message) { }
    }
}
