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

namespace QualitySystem
{
    /// <summary>
    /// Логика взаимодействия для saveHelper.xaml
    /// </summary>
    public partial class SaveHelper : Window
    {
        public int result = -1;

        public SaveHelper(bool[] local)
        {
            InitializeComponent();
            if (!local[0]) radioReg.IsEnabled = false;
            if (!local[1]) radioDes.IsEnabled = false;
            if (!local[2]) 
            {
                radioAll.IsEnabled = false;
                MessageBox.Show("Нет новых построенных моделей", "Ошибка");
                IsEnabled = false;
                this.Close();
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (radioReg.IsChecked == true) result = 0;
            if (radioDes.IsChecked == true) result = 1;
            if (radioAll.IsChecked == true) result = 2;
            this.Hide();
        }
    }
}
