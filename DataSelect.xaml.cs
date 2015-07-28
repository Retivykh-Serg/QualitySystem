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
using System.Windows.Shapes;

namespace QualitySystem
{
    /// <summary>
    /// Логика взаимодействия для DataSelect.xaml
    /// </summary>
    public partial class DataSelect : Window
    {
        TaskConfig mainWindow;
        DBWorker dbConnection;
        DataTable plavka;
        public DataPlavka task; //задание - выбранные параметры и их расшифровка
        public bool isChanged = false;

        public DataSelect(TaskConfig w)
        {
            InitializeComponent();
            mainWindow = w;
            dbConnection = new DBWorker();
            
            //подключаемся к БД и извлекаем начальную инфу о параметрах и марках стали
            if (dbConnection.isConnected)
            {
                task = new DataPlavka(dbConnection.GetPlavka(DbSelect.Columns, null));
                for (int i = 0; i < task.xAll.Count; i++)
                        xListBox.Items.Add(task.xAll[i].description);
                for (int i = 0; i < task.yAll.Count; i++)
                    yListBox.Items.Add(task.yAll[i].description);

                DataTable dt = dbConnection.GetPlavka(DbSelect.Marks, null);
                for (int i = 0; i < dt.Rows.Count; i++)
                    markComboBox.Items.Add(dt.Rows[i].ItemArray[0]);

                gostComboBox.IsEnabled = false;
                typeComboBox.IsEnabled = false;
                dbConnection.CloseConnection();
            }
        }

        #region кнопки добавления параметров
        private void changeOne(ListBox From, ListBox To)
        {
            if (From.SelectedIndex != -1)
            {
                To.Items.Add(From.SelectedItem);
                int pos = From.SelectedIndex;
                From.Items.Remove(From.SelectedItem);
                From.SelectedIndex = pos;
                viewBtn.IsEnabled = false;
            }
        }

        private void changeAll(ListBox From, ListBox To)
        {
            for (int i = 0; i < From.Items.Count; i++)
                To.Items.Add(From.Items[i]);
            From.Items.Clear();
            viewBtn.IsEnabled = false;
        }

        private void addOnebtn_Click(object sender, RoutedEventArgs e)
        {
            changeOne(xListBox, xTaskListBox);
        }

        private void addAllbtn_Click(object sender, RoutedEventArgs e)
        {
            changeAll(xListBox, xTaskListBox);
        }

        private void deleteOnebtn_Click(object sender, RoutedEventArgs e)
        {
            changeOne(xTaskListBox, xListBox);
        }

        private void deleteAllbtn_Click(object sender, RoutedEventArgs e)
        {
            changeAll(xTaskListBox, xListBox);
        }

        private void addOnebtn2_Click(object sender, RoutedEventArgs e)
        {
            changeOne(yListBox, yTaskListBox);
        }

        private void addAllbtn2_Click(object sender, RoutedEventArgs e)
        {
            changeAll(yListBox, yTaskListBox);
        }

        private void deleteOnebtn2_Click(object sender, RoutedEventArgs e)
        {
            changeOne(yTaskListBox, yListBox);
        }

        private void deleteAllbtn2_Click(object sender, RoutedEventArgs e)
        {
            changeAll(yTaskListBox, yListBox);
        }
        #endregion

        private void markComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dbConnection = new DBWorker();
            if (dbConnection.isConnected)
            {
                DataTable dt = dbConnection.GetPlavka(DbSelect.GOSTS, markComboBox.SelectedItem.ToString());
                gostComboBox.Items.Clear();
                for (int i = 0; i < dt.Rows.Count; i++)
                    gostComboBox.Items.Add(dt.Rows[i].ItemArray[0]);
                gostComboBox.IsEnabled = true;
                typeComboBox.IsEnabled = false;
                typeComboBox.Items.Clear();
            }
            dbConnection.CloseConnection();
            viewBtn.IsEnabled = false;
        }

        private void gostComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gostComboBox.HasItems)
            {
                dbConnection = new DBWorker();
                if (dbConnection.isConnected)
                {
                    string[] conds = new string[2];
                    conds[0] = markComboBox.SelectedItem.ToString();
                    conds[1] = gostComboBox.SelectedItem.ToString();
                    DataTable dt = dbConnection.GetPlavka(DbSelect.SteelTypes, conds);
                    typeComboBox.Items.Clear();
                    for (int i = 0; i < dt.Rows.Count; i++)
                        typeComboBox.Items.Add(dt.Rows[i].ItemArray[0]);
                    typeComboBox.IsEnabled = true;
                }
                dbConnection.CloseConnection();
                viewBtn.IsEnabled = false;
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            isChanged = true;
            if (markComboBox.Text != "" && gostComboBox.Text != "" && typeComboBox.Text != "" && xTaskListBox.HasItems && yTaskListBox.HasItems)
            {
                string[] columns = new string[2]; //формируем запрос на извлечение данных
                task.x.Clear();
                task.y.Clear();
                //
                for (int i = 0; i < yTaskListBox.Items.Count; i++) //записываем в задачу и sql выборку всех у
                    for (int j = 0; j < task.yAll.Count; j++)
                        if (task.yAll[j].description == yTaskListBox.Items[i].ToString())
                        {
                            columns[0] += "P." + task.yAll[j].name + ",";
                            task.y.Add(j);
                        }
                for (int i = 0; i < xTaskListBox.Items.Count; i++)//записываем в задачу и sql выборку всех х
                    for (int j = 0; j < task.xAll.Count; j++)
                        if (task.xAll[j].description == xTaskListBox.Items[i].ToString())
                        {
                            columns[0] += task.xAll[j].name + ",";
                            task.x.Add(j);
                        }
                task.y.Sort();
                task.x.Sort();
                columns[0] = columns[0].Trim(','); // удаляем последнюю запятую
                columns[1] = "marka='" + markComboBox.SelectionBoxItem + "' AND gost_id='" + gostComboBox.SelectionBoxItem + 
                    "' AND vytyazhka='" + typeComboBox.SelectionBoxItem + "' AND date_plavka > to_date('" + date1picker.Text +
                    "', 'dd.mm.yyyy') AND date_plavka < to_date('" + date2picker.Text + "', 'dd.mm.yyyy')"; //выбор конкретной марки стали
                dbConnection = new DBWorker();
                plavka = dbConnection.GetPlavka(DbSelect.Data, columns); //извлекаем нужные данные полностью
                task.plavka = plavka.Copy();
                for (int i = 0; i < task.plavka.Rows.Count; i++) //удаляем из данных задачи строки с пустыми значениями
                    for (int j = 0; j < task.plavka.Rows[i].ItemArray.Length; j++)
                        if (task.plavka.Rows[i].ItemArray[j].ToString() == "") 
                        { 
                            task.plavka.Rows.RemoveAt(i);
                            i--;
                            break; 
                        }
                labelResult.Content = "Выбрано " + plavka.Rows.Count + " записей";
                labelResult2.Content = plavka.Rows.Count - task.plavka.Rows.Count + " с пустыми полями";

                viewBtn.IsEnabled = true;
                dbConnection.CloseConnection();

                task.mark = markComboBox.SelectionBoxItem + " " + typeComboBox.SelectionBoxItem;
                task.GOST = gostComboBox.SelectionBoxItem.ToString();
                mainWindow.labelDataMark.Text = "Марка: " + markComboBox.SelectionBoxItem + " " + typeComboBox.SelectionBoxItem;
                mainWindow.labelDataGOST.Text = "Стандарт: " + gostComboBox.SelectionBoxItem;
                mainWindow.labelDataSince.Text = "C: " + date1picker.Text;
                mainWindow.labelDataTo.Text = "По: " + date2picker.Text;
                task.GOST = gostComboBox.SelectionBoxItem.ToString();
                task.mark = markComboBox.SelectionBoxItem + " " + typeComboBox.SelectionBoxItem;
            }
            else labelResult.Content = "Укажите все параметры";
        }

        private void viewBtn_Click(object sender, RoutedEventArgs e)
        {
            Window viewer = new Window();
            viewer.MaxHeight = 600;
            DataGrid dg = new DataGrid();
            dg.ItemsSource = plavka.AsDataView();
            viewer.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            viewer.Content = dg;
            viewer.Show();
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

    }

}
