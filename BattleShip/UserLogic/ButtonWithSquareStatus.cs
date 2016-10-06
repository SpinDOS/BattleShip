using System.Windows;
using System.Windows.Controls;
using BattleShip.BusinessLogic;

namespace BattleShip.UserLogic
{
    public sealed class ButtonWithSquareStatus : Button
    {
        public ButtonWithSquareStatus() : base()
        {
            this.SquareStatus = SquareStatus.Empty;
        }
        public SquareStatus SquareStatus
        {
            get { return (SquareStatus)base.GetValue(SourceProperty); }
            set
            {
                Style = ButtonStylesResourceDictionary.GetStyleByKey(value.ToString());
                base.SetValue(SourceProperty, value);
            }
        }
        private static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("SquareStatus", typeof(SquareStatus), typeof(ButtonWithSquareStatus), null);
    }
}
