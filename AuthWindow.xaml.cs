using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QualitySystem
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void taskBtn_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxServ.Text == "localhost" && textBoxLogin.Text == "admin" && textBoxPass.Text == "nimda")
            {
                TaskConfig taskWindow = new TaskConfig();
                taskWindow.Show();
                this.Close();
            }
            else
            {
                textBoxPass.Text = "";
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
