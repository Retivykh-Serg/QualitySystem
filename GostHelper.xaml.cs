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
    /// Логика взаимодействия для GostHelper.xaml
    /// </summary>
    public partial class GostHelper : Window
    {
        List<itemGrid> List = new List<itemGrid>();
        DataPlavka task;

        public GostHelper()
        {
            InitializeComponent();
            DBWorker dbConnection = new DBWorker();
            task = new DataPlavka(dbConnection.GetPlavka(DbSelect.Columns, null));
            for (int i = 0; i < task.yAll.Count; i++)
                List.Add(new itemGrid(task.yAll[i].description, -1, -1));
            dbConnection.CloseConnection();
            gridGOST.ItemsSource = List;
        }

        private void buttonGO_Click(object sender, RoutedEventArgs e)
        {
            bool flag = false;
            for (int i = 0; i < List.Count; i++)
                if (List[i].val1 != -1 || List[i].val2 != -1)
                {
                    string names = "GOST_ID,MARKA,VYTYAZHKA";
                    for (int j = 0; j < List.Count; j++)
                        names += "," + task.yAll[j].name + "_MIN," + task.yAll[j].name;
                    
                    string values = "'" + textBoxGost.Text + "','" + textBoxMarka.Text + "','" + textBoxType.Text + "'";
                    for (int j = 0; j < List.Count; j++)
                        values += "," + List[j].val1.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                            List[j].val2.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    
                    DBWorker dbConnection = new DBWorker();
                    int res = dbConnection.InsertGOST(names, values);
                    dbConnection.CloseConnection();
                    if (res == 1) MessageBox.Show("Запись добавлена!");
                    else MessageBox.Show("Ошибка добавления!");
                    this.Close();
                    flag = true;
                    break;
                }
            if (flag == false) MessageBox.Show("Введите границы!");
        }
    }
}
