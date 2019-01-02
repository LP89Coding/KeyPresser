using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KeyPresser
{
    /// <summary>
    /// Interaction logic for PressKeyDialog.xaml
    /// </summary>
    public partial class PressKeyDialog : Window
    {
        public string PressedKeyText { get; set; }
        public Key PressedKey { get; set; }

        public PressKeyDialog(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
        }

        public new bool? ShowDialog()
        {
            tbPresedKey.Focus();
            this.Visibility = Visibility.Visible;
            return base.ShowDialog();
        }

        private Key _LastPressedKey = Key.None;
        private void tbPresedKey_KeyDown(object sender, KeyEventArgs e)
        {
            this._LastPressedKey = e.Key;
        }

        private void tbPresedKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(tbPresedKey.Text))
            {
                tbPresedKey.IsReadOnly = true;
                this.PressedKeyText = tbPresedKey.Text.Trim();
                this.PressedKey = this._LastPressedKey;
                Thread.Sleep(1000);
                DialogResult = true;
                this.Visibility = Visibility.Hidden;
            }

        }
    }
}
