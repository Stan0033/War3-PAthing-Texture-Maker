using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Custom_Pathing_Texture_Maker
{
    /// <summary>
    /// Interaction logic for colorspicker.xaml
    /// </summary>
    public partial class colorspicker : Window
    {
        public colorspicker()
        {
            InitializeComponent();

        }

        private void ok(object sender, RoutedEventArgs e)
        {
            string one  =(Combo1.SelectedItem as ComboBoxItem).Content.ToString();
            string two  =(Combo2.SelectedItem as ComboBoxItem).Content.ToString();
            if (one == two)
            {
                return;
            }
            DialogResult = true;
        }
    }
}
