using System.Windows;

namespace BattleShip.UserLogic
{
    partial class ButtonStylesResourceDictionary
    {
        private static ButtonStylesResourceDictionary dictionary 
            = new ButtonStylesResourceDictionary();
        public static Style GetStyleByKey(string key)
        {
            return (Style) dictionary[key];
        }
        private ButtonStylesResourceDictionary()
        {
            InitializeComponent();
        }
    }
}
