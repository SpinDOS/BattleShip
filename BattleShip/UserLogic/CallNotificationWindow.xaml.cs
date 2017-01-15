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
    /// Interaction logic for CallNotification.xaml
    /// </summary>
    public partial class CallNotificationWindow : Window
    {
        // field for result of window showing
        private bool _result = false;
        public CallNotificationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show window and ask user if he wants to accept opponent's call
        /// </summary>
        /// <returns>true, if user wants to accept opponent's call</returns>
        public bool GetAnswer()
        {
            // default answer is false
            _result = false;
            // ask
            this.ShowDialog();
            // return result
            return _result;
        }

        // window never close, only hide
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // dont close, hide
            e.Cancel = true;
            this.Hide();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            // set result and close form
            _result = true;
            this.Hide();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e) => this.Hide();
    }
}
