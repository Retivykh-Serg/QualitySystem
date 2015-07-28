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
using System.Data;

namespace QualitySystem
{
    /// <summary>
    /// Логика взаимодействия для LoadHelper.xaml
    /// </summary>
    public partial class LoadHelper : Window
    {
        TaskConfig W;
        public int type = -1;
        public int resInfo = -1;

        public LoadHelper(TaskConfig main)
        {
            InitializeComponent();
            W = main;
        }

        private void radioLoadOpt_Checked(object sender, RoutedEventArgs e)
        {
            if (radioLoadOpt.IsChecked == true) checkLoadAll.IsEnabled = true;
            if (radioLoadOpt.IsChecked == false) checkLoadAll.IsEnabled = false;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            resInfo = 0;
            if (radioLoadReg.IsChecked == true) type = 1;
            if (radioLoadDes.IsChecked == true) type = 2;
            if (radioLoadOpt.IsChecked == true) type = 3;
            int id = Convert.ToInt32(textBoxID.Text);

            if (type == 1) resInfo = LoadRegress(id);
            if (type == 2) resInfo = LoadDescret(id);
            if (type == 3) 
            {
                resInfo = LoadTechnology(id);
                if (checkLoadAll.IsChecked == true)
                {
                    type = 4;
                    if (resInfo == 1) resInfo = LoadDescret(W.technology.descretID);
                    if (resInfo == 1) resInfo = LoadRegress(W.technology.regressID);
                }
            }

            if (resInfo == 1) MessageBox.Show("Успешно загружено!");
            if (resInfo == 0) MessageBox.Show("Модель не найдена!");
            if (resInfo == -1) MessageBox.Show("Ошибка загрузки модели");
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private int LoadRegress(int id) //грузим регрессионную модель
        {
                DBWorker dbConnection = new DBWorker();
                if (dbConnection.isConnected)
                {
                    DataTable modelTable = dbConnection.LoadModel(DbSelect.SelectReg, id);
                    if (W.task == null) W.task = new DataPlavka(dbConnection.GetPlavka(DbSelect.Columns, null)); //берем список параметров
                    if (modelTable == null)
                    {
                        if (W.modelRegress != null) W.modelRegress.buildInfo = -3;
                        return -1;
                    }
                    if (modelTable.Rows.Count == 0) return 0;
                    W.modelRegress = new RegressionModel(id);
                    W.modelRegress.date = Convert.ToDateTime(modelTable.Rows[0]["DATE_REG"]);
                    W.modelRegress.GOST = modelTable.Rows[0]["GOST_ID"].ToString();
                    W.modelRegress.mark = modelTable.Rows[0]["MARKA"].ToString() + " " + modelTable.Rows[0]["VYTYAZHKA"].ToString();
                    //W.task.GOST = W.modelRegress.GOST;
                    //W.task.mark = W.modelRegress.mark;
                    //формируем массив факторов Х
                    for (int i = 0; i < W.task.xAll.Count; i++)
                        if (modelTable.Rows[0][W.task.xAll[i].name] != System.DBNull.Value)
                            W.modelRegress.x.Add(i);
                    for (int i = 0; i < W.modelRegress.x.Count; i++) //проверяем простая или точная модель
                        if (modelTable.Rows[0][W.task.xAll[W.modelRegress.x[i]].name + "_F"] != System.DBNull.Value) { W.modelRegress.isUpgraded = true; break; }
                    if (W.modelRegress.isUpgraded)
                    {
                        W.modelRegress.typesF = new int[W.modelRegress.x.Count];
                        for (int i = 0; i < W.modelRegress.x.Count; i++)
                            W.modelRegress.typesF[i] = Convert.ToInt32(modelTable.Rows[0][W.task.xAll[W.modelRegress.x[i]].name + "_F"]);
                    }

                    for (int i = 0; i < modelTable.Rows.Count; i++) //каждая строка - модель
                    {
                        W.modelRegress.y.Add(Convert.ToInt32(modelTable.Rows[i]["Y_ID"]));
                        //W.task.y.Add(Convert.ToInt32(modelTable.Rows[i]["Y_ID"]));
                        double[] weights = new double[W.modelRegress.x.Count + 1]; //последний - свобоный ЧЛЕН
                        for (int j = 0; j < W.modelRegress.x.Count; j++)
                            weights[j] = Convert.ToDouble(modelTable.Rows[i][W.task.xAll[W.modelRegress.x[j]].name]);
                        weights[W.modelRegress.x.Count] = Convert.ToDouble(modelTable.Rows[i]["A"]);
                        RegEquation modelOne = new RegEquation(weights, Convert.ToDouble(modelTable.Rows[i]["FISHER"]),
                            Convert.ToDouble(modelTable.Rows[i]["FISHER_VAL"]), Convert.ToDouble(modelTable.Rows[i]["RMSERROR"]));
                        modelOne.descr = W.task.yAll[W.modelRegress.y[i]].description;
                        if (modelOne.F_test == 0) modelOne.F_test = 1.0e-16;
                        W.modelRegress.equation.Add(modelOne);
                    }
                }
                dbConnection.CloseConnection();
                return 1;
        }

        private int LoadDescret(int id)
        {
            DBWorker dbConnection = new DBWorker();
            if (dbConnection.isConnected)
            {
                DataTable modelTable = dbConnection.LoadModel(DbSelect.SelectDes, id);
                if (W.task == null) W.task = new DataPlavka(dbConnection.GetPlavka(DbSelect.Columns, null)); //берем список параметров
                if (modelTable == null) return -1;
                if (modelTable.Rows.Count == 0) return 0;
                W.modelDescret = new DescrModel(id);
                W.modelDescret.date = Convert.ToDateTime(modelTable.Rows[0]["DATE_DES"]);
                
                W.modelDescret.GOST = modelTable.Rows[0]["GOST_ID"].ToString(); 
                W.modelDescret.mark = modelTable.Rows[0]["MARKA"].ToString() + " " + modelTable.Rows[0]["VYTYAZHKA"].ToString();
                //W.task.GOST = W.modelDescret.GOST;
                //W.task.mark = W.modelDescret.mark;
                W.modelDescret.alpha = Convert.ToDouble(modelTable.Rows[0]["ALPHA"]);
                W.modelDescret.beta = Convert.ToDouble(modelTable.Rows[0]["BETA"]);
                W.modelDescret.criteria = Convert.ToDouble(modelTable.Rows[0]["U"]);
                string ys = modelTable.Rows[0]["YS"].ToString();
                string[] ys2 = ys.Split('@');
                for (int i = 0; i < ys2.Length; i++) W.modelDescret.y.Add(Convert.ToInt32(ys2[i])); //установили У
                //формируем массив факторов Х
                for (int i = 0; i < W.task.xAll.Count; i++)
                    if (modelTable.Rows[0][W.task.xAll[i].name] != System.DBNull.Value)
                        W.modelDescret.x.Add(i);

                for (int i = 0; i < W.modelDescret.x.Count; i++)
                {
                    double lower = Convert.ToDouble(modelTable.Rows[0][W.task.xAll[W.modelDescret.x[i]].name + "_MIN"]);
                    double upper = Convert.ToDouble(modelTable.Rows[0][W.task.xAll[W.modelDescret.x[i]].name]);
                    string descr = W.task.xAll[W.modelDescret.x[i]].description;
                    Conditions c = new Conditions(lower, upper, descr);
                    W.modelDescret.xBounds.Add(c);
                }
            }
            dbConnection.CloseConnection();
            return 1;
        }

        private int LoadTechnology(int id)
        {
             DBWorker dbConnection = new DBWorker();
             if (dbConnection.isConnected)
             {
                 DataTable modelTable = dbConnection.LoadModel(DbSelect.SelectTech, id);
                 if (W.task == null) W.task = new DataPlavka(dbConnection.GetPlavka(DbSelect.Columns, null)); //берем список параметров
                 if (modelTable == null) return -1;
                 if (modelTable.Rows.Count == 0) return 0;

                 W.technology = new Technology(id);
                 W.technology.date = Convert.ToDateTime(modelTable.Rows[0]["DATE_TECH"]);
                 string[] markInfo = dbConnection.GetMark(Convert.ToInt32(modelTable.Rows[0]["STE_ID"]));
                 W.technology.GOST = markInfo[0];
                 W.technology.mark = markInfo[1] + " " + markInfo[2];
                 W.technology.rmserror = Convert.ToDouble(modelTable.Rows[0]["RMSERROR"]);
                 if (W.technology.rmserror == 0) W.technology.rmserror = 1.0e-16;
                 W.technology.descretID = Convert.ToInt32(modelTable.Rows[0]["DES_ID"]);
                 W.technology.regressID = Convert.ToInt32(modelTable.Rows[0]["REG_ID"]);
                 W.technology.baseTechnologyID = Convert.ToInt32(modelTable.Rows[0]["BASE_ID"]);
                 if (W.technology.baseTechnologyID != -1) W.technology.isCorrected = true;
                 //формируем массив факторов Х
                 for (int i = 0; i < W.task.xAll.Count; i++)
                     if (modelTable.Rows[0][W.task.xAll[i].name] != System.DBNull.Value)
                         W.technology.x.Add(i);
                 //формируем массив факторов Y
                 for (int i = 0; i < W.task.yAll.Count; i++)
                     if (modelTable.Rows[0][W.task.yAll[i].name] != System.DBNull.Value)
                         W.technology.y.Add(i);
                 for (int i = 0; i < W.technology.x.Count; i++)
                     W.technology.xOpt.Add(Convert.ToDouble(modelTable.Rows[0][W.task.xAll[W.technology.x[i]].name]));
                 for (int i = 0; i < W.technology.y.Count; i++)
                     W.technology.yOpt.Add(Convert.ToDouble(modelTable.Rows[0][W.task.yAll[W.technology.y[i]].name]));

             }
             dbConnection.CloseConnection();
             return 1;
        }

        private void view_Click(object sender, RoutedEventArgs e)
        {
            DBWorker dbConnection = new DBWorker();
            DataTable res = new DataTable();
                if (dbConnection.isConnected)
                {
                    if (radioLoadReg.IsChecked == true) res = dbConnection.ViewAvaliableModels(DbSelect.SelectReg);
                    if (radioLoadDes.IsChecked == true) res = dbConnection.ViewAvaliableModels(DbSelect.SelectDes);
                    if (radioLoadOpt.IsChecked == true) res = dbConnection.ViewAvaliableModels(DbSelect.SelectTech);
                }
                Window viewer = new Window();
                viewer.MaxHeight = 600;
                viewer.MaxWidth = 900;
                DataGrid dg = new DataGrid();
                dg.ItemsSource = res.AsDataView();
                viewer.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                viewer.Content = dg;
                viewer.Show();
        }
    }
}
