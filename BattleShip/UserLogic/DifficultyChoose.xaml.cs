using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Interaction logic for DifficultyChoose.xaml
    /// </summary>
    public partial class DifficultyChoose : Window
    {
        private bool chose = false;
        public DifficultyChoose()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Ask user for difficulty level
        /// </summary>
        /// <returns>Return difficulty level 1-3</returns>
        public int AskDifficulty()
        {
            chose = false;
            this.ShowDialog();
            if (!chose)
                throw new OperationCanceledException("No level choosed");
            if (Level1.IsChecked.Value)
                return 1;
            if (Level2.IsChecked.Value)
                return 2;
            if (Level3.IsChecked.Value)
                return 3;
            throw new NotImplementedException("There is no another difficulty levels");
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            chose = true;
            this.Close();
        }
    }
}
