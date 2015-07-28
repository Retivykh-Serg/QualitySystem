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
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace QualitySystem
{
    /// <summary>
    /// Логика взаимодействия для TaskConfig.xaml
    /// </summary>
    public partial class TaskConfig : Window
    {
        BackgroundWorker backgroundWorker;

        public DescrModel modelDescret;
        public RegressionModel modelRegress;
        CondOptModel modelOpt;
        public Technology technology;
        Technology technologyCorrected;
        public DataPlavka task;

        DataSelect dataWindow;
        List<itemGrid> yOptData;
        List<itemGrid> xOptData;
        List<itemGrid> xBoundsData;
        List<itemGrid> yBoundsData;

        public TaskConfig()
        {
            InitializeComponent();
            yOptData = new List<itemGrid>();
            xOptData = new List<itemGrid>();
            xBoundsData = new List<itemGrid>();
            yBoundsData = new List<itemGrid>();
            backgroundWorker = ((BackgroundWorker)this.FindResource("backgroundWorker"));
            getBoundsBtn.Content = "Установить границы\n   по выборке";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (dataWindow != null) dataWindow.Close();
        }

        private void dataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (dataWindow == null) dataWindow = new DataSelect(this);
            dataWindow.ShowDialog();
            if (dataWindow.IsInitialized)
                if (dataWindow.isChanged)
                {
                    dataWindow.isChanged =false;
                    task = new DataPlavka(dataWindow.task);
                    Log.Text = "Загружено " + dataWindow.task.plavka.Rows.Count.ToString() + " записей";
                    labelDataNotes.Text = "Загружено записей: " + dataWindow.task.plavka.Rows.Count.ToString();
                    
                    dataDataGrid.ItemsSource = null;
                    dataDataGrid.ItemsSource = dataWindow.task.plavka.AsDataView();
                    if (dataDataGrid.Columns.Count < 9)
                        for (int i = 0; i < dataDataGrid.Columns.Count; i++)
                            dataDataGrid.Columns[i].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                    labelDataX.Content = "Количество исследуемых факторов - " + task.x.Count;
                    labelDataY.Content = "Количество исследуемых свойств - " + task.y.Count;
                    xBoundsData.Clear();
                    for(int i=0; i<task.x.Count; i++)
                        xBoundsData.Add(new itemGrid(task.xAll[task.x[i]].description, -1, -1));
                    xDescrGrid.ItemsSource = xBoundsData;

                    markComboBox.Items.Clear();
                    gostComboBox.Items.Clear();
                    typeComboBox.Items.Clear();
                    typeComboBox.IsEnabled = false;
                    DBWorker dbConnection = new DBWorker();
                    DataTable dt = dbConnection.GetPlavka(DbSelect.Marks, null);
                    for (int i = 0; i < dt.Rows.Count; i++)
                        markComboBox.Items.Add(dt.Rows[i].ItemArray[0]);
                    dbConnection.CloseConnection();

                    string[] temp = task.mark.Split(' ');
                    SetGOST();

                    funcGrid.Visibility = System.Windows.Visibility.Collapsed;
                    statGrid.Visibility = System.Windows.Visibility.Collapsed;
                    groupBoxBefore.Header = "Начальные границы";
                    groupBoxAfter.Header = "Дискретная модель";
                    dataExpander.IsExpanded = true;
                }
            
        }

        private void loadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadHelper loadWindow = new LoadHelper(this);
            loadWindow.ShowDialog(); //когда закроется - регрессионная модель уже загружена
            if (loadWindow.resInfo == -1)
            {
                    Log.Text = "Не удалось загрузить модели";
                    return;
            }
            if (loadWindow.type == 1 || loadWindow.type == 4)
            {
                modelView(modelRegress);
            }
            if (loadWindow.type == 2 || loadWindow.type == 4)
            {
                alphaTextBox.Text = modelDescret.alpha.ToString();
                betaTextBox.Text = modelDescret.beta.ToString();
                groupBoxAfter.Header = "Дискретная модель: U = " + modelDescret.criteria;
                xOptDescrGrid.ItemsSource = null;
                xOptDescrGrid.ItemsSource = modelDescret.xBounds; 
                SetModelInfoDescret();
                
                SetGOST();
                Log.Text = "Дискретная модель успешно загружена!";
            }
            if (loadWindow.type == 3 || loadWindow.type == 4)
            {
                SetTechnology();
            }
        }
        
        #region сохранение моделей
        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool[] local = new bool[3];
            if (modelRegress != null) if (modelRegress.id == -1) { local[0] = true; local[2] = true; }
            if (modelDescret != null) if (modelDescret.id == -1) { local[1] = true; local[2] = true; }
            if (technology != null) if (technology.id == -1) local[2] = true;
            if (technologyCorrected != null) if (technologyCorrected.id == -1) local[2] = true;
            SaveHelper sWindow = new SaveHelper(local);
            if (sWindow.IsEnabled) sWindow.ShowDialog();
            int res = sWindow.result; // 0 -regress model, 1 - descret model, 2 - all avaliable
            sWindow.Close();
            if (res == 0) saveRegModel(true);
            if (res == 1) saveDesModel(true);
            if (res == 2)
            {
                if (modelRegress != null) if (modelRegress.id == -1) saveRegModel(false);
                if (modelDescret != null) if (modelDescret.id == -1) saveDesModel(false);
                if (technology != null) if (technology.id == -1)
                    {
                        technology.descretID = modelDescret.id;
                        technology.regressID = modelRegress.id;
                        labelOptDesID.Text = "ID дескр. модели: " + modelDescret.id.ToString();
                        labelOptRegID.Text = "ID регр. модели: " + modelRegress.id.ToString();
                        saveTechnology(technology);
                    }
                if (technologyCorrected != null) if (technologyCorrected.id == -1)
                    {
                        technologyCorrected.descretID = modelDescret.id;
                        technologyCorrected.baseTechnologyID = technology.id;
                        labelCorDesID.Text = "ID дескр. модели: " + modelDescret.id.ToString();
                        labelCorOptID.Text = "ID исх. технологии: " + technology.id.ToString();
                        saveTechnology(technologyCorrected);
                    }
            }
            
        }

        private void saveRegModel(bool showBox)
        {
            DBWorker dbConnection = new DBWorker();
            if (dbConnection.isConnected)
            {
                modelRegress.id = dbConnection.GetID(DbSelect.RegID) + 1;
                if (dbConnection.InsertModel(task, modelRegress) == -1)
                {
                    MessageBox.Show("Ошибка сохранения регрессионной модели");
                    modelRegress.id = -1;
                }
                else
                {
                    if(showBox) MessageBox.Show("Модель успешно сохранена!");
                    labelRegID.Text = "ID: " + modelRegress.id.ToString();
                }
            }
        }

        private void saveDesModel(bool showBox)
        {
            DBWorker dbConnection = new DBWorker();
            if (dbConnection.isConnected)
            {
                modelDescret.id = dbConnection.GetID(DbSelect.DesID) + 1;
                if (dbConnection.InsertModel(task, modelDescret) == -1)
                {
                    MessageBox.Show("Ошибка сохранения дискретной модели");
                    modelDescret.id = -1;
                }
                else
                {
                    if (showBox) MessageBox.Show("Модель успешно сохранена!");
                    labelDesID.Text = "ID: " + modelDescret.id.ToString();
                }
            }
        }

        private void saveTechnology(Technology tech)
        {
            DBWorker dbConnection = new DBWorker();
            if (dbConnection.isConnected)
            {
                tech.id = dbConnection.GetID(DbSelect.TechID) + 1;
                if (dbConnection.InsertModel(task, tech) == -1)
                {
                    MessageBox.Show("Ошибка сохранения технологии");
                    tech.id = -1;
                }
                else
                {
                    if (tech.isCorrected != true) MessageBox.Show("Cохранено!");
                    labelOptID.Text = "ID: " + tech.id.ToString();
                }
            }
        }
        #endregion

        #region работа с регрессией
        private void modelView(RegressionModel modelRegress)
        {
            modelGrid.Columns.Clear();
            modelGrid.AutoGenerateColumns = false;
            statGrid.Visibility = Visibility.Visible;
            if (modelRegress.isUpgraded) funcGrid.Visibility = Visibility.Visible;
            else funcGrid.Visibility = Visibility.Collapsed;
            #region вывод коэффициентов модели
            //изврат с отображением в датаГрид
            List<string[]> dataViewer = new List<string[]>();
            for (int i = 0; i < modelRegress.x.Count + 1; i++) // +1 потому что еще свободный ЧЛЕН
            {
                string[] s = new string[modelRegress.y.Count + 1];
                if (i != modelRegress.x.Count) s[0] = task.xAll[modelRegress.x[i]].description;
                else s[0] = "a";
                for (int j = 0; j < modelRegress.y.Count; j++)
                    s[j + 1] = Math.Round(modelRegress.equation[j].weights[i], 6).ToString();
                dataViewer.Add(s);
            }

            for (int i = 0; i < modelRegress.y.Count + 1; i++) //формируем столбцы - это у
            {
                var col = new DataGridTextColumn();
                var binding = new Binding(String.Format("[{0}]", i));
                if (i == 0) col.Width = new DataGridLength(2, DataGridLengthUnitType.Star);
                else
                {
                    col.Header = task.yAll[modelRegress.y[i-1]].description;
                    col.Width = new DataGridLength(3, DataGridLengthUnitType.Star);
                }
                col.Binding = binding;
                
                modelGrid.Columns.Add(col);
            } //тут создали выбранные параметры столбцами
            modelGrid.ItemsSource = dataViewer;
            #endregion
            for (int i = 0; i < task.y.Count; i++)
                modelRegress.equation[i].descr = task.yAll[modelRegress.y[i]].description;
            statGrid.ItemsSource = modelRegress.equation;
            #region вывод типов функций для уточненной модели
            if (modelRegress.isUpgraded)
            {
                List<itemGrid> f = new List<itemGrid>();
                for (int i = 0; i < modelRegress.typesF.Length; i++)
                    f.Add( new itemGrid(task.xAll[modelRegress.x[i]].description, FunctionTypeConverter.ViewFx(modelRegress.typesF[i])));
                funcGrid.ItemsSource = f;
                funcGrid.Width = gridsDockPanel.ActualWidth / 3;
            }
            #endregion
            
            yOptData.Clear();
            yOptGrid.ItemsSource = null;
            xOptGrid.ItemsSource = null;
            for (int i = 0; i < modelRegress.y.Count; i++)
                yOptData.Add(new itemGrid(task.yAll[modelRegress.y[i]].description, -1));
            yOptGrid.Columns[2].Visibility = System.Windows.Visibility.Hidden;
            yOptGrid.Columns[3].Visibility = System.Windows.Visibility.Hidden;
            yOptGrid.ItemsSource = yOptData;

            if (modelRegress.isUpgraded) labelRegType.Text = "Тип: улучшенная";
            else labelRegType.Text = "Тип: простая";
            if (modelRegress.id == -1)
            {
                labelRegID.Text = "ID: local";
                modelRegress.date = DateTime.Now.Date;
                modelRegress.mark = task.mark;
                modelRegress.GOST = task.GOST;
            }
            else
                labelRegID.Text = "ID: " + modelRegress.id.ToString();
            Log.Text = "Регрессионная модель успешно построена";
            labelRegGOST.Text = modelRegress.GOST;
            labelRegMark.Text = modelRegress.mark;
            labelRegDate.Text = "Дата: " + modelRegress.date.ToShortDateString();
            expanderReg.IsExpanded = true;
        }
        
        private void startModeling_Click(object sender, RoutedEventArgs e)
        {
            if (task.data == null)
            {
                Log.Text = "Данные не загружены";
                return;
            }
            modelRegress = task.LinReg(false);
            if (modelRegress.buildInfo < 0)
            {
                // MessageBox.Show("buildInfo = " + modelRegress.buildInfo.ToString());
                Log.Text = "Ошибка построения модели: выборка имеет малое количество записей";
                return;
            }
            modelView(modelRegress);
            expanderReg.IsExpanded = true;
        }

        private void startUpgrade_Click(object sender, RoutedEventArgs e)
        {
            if (task.data == null)
            {
                Log.Text = "Данные не загружены";
                return;
            }
            modelRegress = task.LinReg(true);
            if (modelRegress.buildInfo < 0)
            {
                MessageBox.Show("buildInfo = " + modelRegress.buildInfo.ToString());
                Log.Text = "Ошибка построения модели";
                return;
            }
            modelView(modelRegress);
            expanderReg.IsExpanded = true;
        }
        #endregion

        #region работа с оптимизацией
        private void optimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (modelRegress == null)
            {
                Log.Text = "Регрессионная модель не загружена";
                return;
            }
            if (modelDescret == null)
            {
                Log.Text = "Дискретная модель не загружена";
                return;
            }
            for (int i=0; i<modelRegress.x.Count; i++) 
                if (modelRegress.x[i] != modelDescret.x[i])
                {
                    Log.Text = "Дискретная и регрессионная модель используют разные факторы!";
                    return;
                }
            for (int i = 0; i < modelRegress.y.Count; i++)
                if (modelRegress.y[i] != modelDescret.y[i])
                {
                    Log.Text = "Дискретная и регрессионная модель используют разные выходные свойства!";
                    return;
                }
            for(int i=0; i<modelRegress.y.Count; i++)
                if ( yOptData[i].val1 == -1 ) 
                {
                    Log.Text = "Не выбраны оптимальные значения свойств";
                    return;
                }
            
            double[] yOpt = new double[modelRegress.y.Count];
            for (int i = 0; i < modelRegress.y.Count; i++) yOpt[i] = yOptData[i].val1;
            modelOpt = new CondOptModel(modelRegress, modelDescret, yOpt, Convert.ToDouble(epsgTextBox.Text), 
                Convert.ToDouble(epsxTextBox.Text), Convert.ToInt32(iterTextBox.Text));
            technology = modelOpt.StartOptimize(); //численный градиент

            SetTechnology();
        } 
        
        private void correctBtn_Click(object sender, RoutedEventArgs e)
        {
            if (technology == null)
            {
                Log.Text = "Технология не загружена";
                return;
            }
            if (modelDescret == null)
            {
                Log.Text = "Дискретная модель не загружена";
                return;
            }
            if (modelRegress == null)
            {
                Log.Text = "Регрессионная модель не загружена";
                return;
            }
            for (int i = 0; i < modelRegress.x.Count; i++)
                if (modelRegress.x[i] != modelDescret.x[i])
                {
                    Log.Text = "Модели используют разные факторы!";
                    return;
                }

            double[] xDone = new double[xOptData.Count];
            for (int i = 0; i < xOptData.Count; i++) xDone[i] = xOptData[i].val2;
            CondOptModel modelCor = new CondOptModel(modelRegress, modelDescret, technology,Convert.ToDouble(epsgTextBox2.Text), 
                Convert.ToDouble(epsxTextBox2.Text), Convert.ToInt32(iterTextBox2.Text),xDone);
            technologyCorrected = modelCor.StartCorrect();
            //заполняем результаты
            for (int i = 0; i < modelRegress.x.Count; i++) xOptData[i].val2 = technologyCorrected.xOpt[i];
            XcurrentGrid.ItemsSource = null;
            XcurrentGrid.ItemsSource = xOptData;
            for (int i = 0; i < modelRegress.y.Count; i++)
            {
                yOptData[i].val2 = modelCor.yModel[i];
                yOptData[i].val3 = technologyCorrected.error[i];
            }

            YcurrentGrid.Columns[2].Visibility = System.Windows.Visibility.Visible;
            YcurrentGrid.Columns[3].Visibility = System.Windows.Visibility.Visible;
            YcurrentGrid.ItemsSource = null;
            YcurrentGrid.ItemsSource = yOptData;
            Log.Text = "Технология скорректирована!";

            List<itemGrid> YnotCorrectedData = new List<itemGrid>();
            for (int i = 0; i < yOptData.Count; i++)
                YnotCorrectedData.Add(new itemGrid(yOptData[i].header, yOptData[i].val1));
            double[] oldOpt = new double[technology.xOpt.Count];
            for (int i = 0; i < oldOpt.Length; i++) oldOpt[i] = technology.xOpt[i];
            for (int i = 0; i < yOptData.Count; i++)
            {
                if (modelRegress.isUpgraded) YnotCorrectedData[i].val2 = modelRegress.equation[i].Calculate(oldOpt, modelRegress.typesF);
                else YnotCorrectedData[i].val2 = modelRegress.equation[i].Calculate(xDone);
                YnotCorrectedData[i].val3 = Math.Abs(YnotCorrectedData[i].val2 - YnotCorrectedData[i].val1);
            }
            YolDcurrentGrid.ItemsSource = null;
            YolDcurrentGrid.ItemsSource = YnotCorrectedData;

            labelCorID.Text = "ID: local";
            labelCorDate.Text = "Дата: " + DateTime.Now.Date.ToShortDateString();
            labelCorGOST.Text = labelDesGOST.Text;
            labelCorMark.Text = labelDesMark.Text;
            labelCorOptID.Text = "ID исходной техн-ии: " + technologyCorrected.baseTechnologyID;
            labelCorDesID.Text = "ID дескр. модели: " + modelDescret.id.ToString();
            technologyCorrected.mark = modelDescret.mark;
            technologyCorrected.GOST = modelDescret.GOST;
            technologyCorrected.date = DateTime.Now.Date;
            expanderCor.IsExpanded = true;
        }

        private void SetTechnology()
        {
            xOptData.Clear();
            for (int i = 0; i < modelRegress.x.Count; i++)
                xOptData.Add(new itemGrid(task.xAll[modelRegress.x[i]].description, technology.xOpt[i]));
            xOptGrid.ItemsSource = null;
            xOptGrid.ItemsSource = xOptData;

            XcurrentGrid.ItemsSource = xOptData;
            XcurrentGrid.ItemsSource = xOptData;

            resultErrorLabel.Content = "Невязка составляет: " + technology.rmserror.ToString("e");
            
            YcurrentGrid.Columns[2].Visibility = System.Windows.Visibility.Hidden;
            YcurrentGrid.Columns[3].Visibility = System.Windows.Visibility.Hidden;
           


            if (technology.id == -1)
            {
                for (int i = 0; i < modelRegress.y.Count; i++)
                {
                    yOptData[i].val2 = modelOpt.yModel[i];
                    yOptData[i].val3 = technology.error[i];
                }
                yOptGrid.Columns[2].Visibility = System.Windows.Visibility.Visible;
                yOptGrid.Columns[3].Visibility = System.Windows.Visibility.Visible;
                labelOptID.Text = "ID: local";
                technology.date = DateTime.Now.Date;
                technology.GOST = modelDescret.GOST;
                technology.mark = modelDescret.mark;
                technology.regressID = modelRegress.id;
                technology.descretID = modelDescret.id;
            }
            else
            {
                labelOptID.Text = "ID: " + technology.id.ToString();
                for (int i = 0; i < modelRegress.y.Count; i++)
                    yOptData[i].val1 = technology.yOpt[i];
            }
            yOptGrid.ItemsSource = null;
            yOptGrid.ItemsSource = yOptData;
            YcurrentGrid.ItemsSource = yOptData;
            YcurrentGrid.ItemsSource = yOptData;

            labelOptDate.Text = technology.date.ToShortDateString();
            labelOptGOST.Text = technology.GOST;
            labelOptMark.Text = technology.mark;
            labelOptDesID.Text = "ID дескр-й модели: " + technology.descretID;
            labelOptRegID.Text = "ID регр-й модели: " + technology.regressID;

            expanderOpt.IsExpanded = true;
            Log.Text = "Технология успешно построена!";
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < xOptData.Count; i++) xOptData[i].val2 = -1;
            XcurrentGrid.ItemsSource = null;
            XcurrentGrid.ItemsSource = xOptData;
        }
        #endregion

        #region работа с дискретной моделью
        private void getBoundsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (task == null)
            {
                Log.Text = "Данные не загружены";
                return;
            }

            xBoundsData.Clear();
            xDescrGrid.ItemsSource = null;
            modelDescret = task.GetPlavkaBounds();
            foreach (Conditions c in modelDescret.xBounds)
                xBoundsData.Add(new itemGrid(c.descr, c.lower, c.upper));
            xDescrGrid.ItemsSource = xBoundsData;
            Log.Text = "Границы загружены";

            xOptDescrGrid.ItemsSource = null;
            xOptDescrGrid.ItemsSource = modelDescret.xBounds;

            SetModelInfoDescret();
        }

        private void SetModelInfoDescret()
        {
            if (modelDescret.id == -1)
            {
                labelDesID.Text = "ID: local";
                modelDescret.date = DateTime.Now.Date;
                modelDescret.mark = task.mark;
                modelDescret.GOST = task.GOST;
            }
            else labelDesID.Text = "ID: " + modelDescret.id.ToString();
            labelDesDate.Text = "Дата: "+ modelDescret.date.ToShortDateString();
            labelDesGOST.Text = modelDescret.GOST;
            labelDesMark.Text = modelDescret.mark;
            modelDescret.date = DateTime.Now.Date;
            expanderDesLeft.IsExpanded = true;
        }

        private void descrBoundsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (task.data == null)
            {
                Log.Text = "Данные не загружены";
                return;
            }
            if (yBoundsData.Count == 0)
            {
                Log.Text = "Не выбран стандарт на марку";
                return;
            }
            modelDescret = new DescrModel(task.x, task.y, Convert.ToDouble(alphaTextBox.Text), Convert.ToDouble(betaTextBox.Text));
            modelDescret.SetBounds(xBoundsData, yBoundsData); //берем границы из табличек
            task.CorrectBoundsY(modelDescret); //если верхняя не определена - берем максимум по выборке
            //List<string[]> results;
            DescretOptTask descrOptimizeBackground = new DescretOptTask(task, modelDescret, Convert.ToInt32(numIntervalsTextBox.Text),
                Convert.ToInt32(degreeTextBox.Text), Convert.ToInt32(shiftTextBox.Text), Convert.ToDouble(epsTextBox.Text));

            backgroundWorker.RunWorkerAsync(descrOptimizeBackground);
            Log.Text = "Начато построение дискретной модели . . . ";
                 //передаем ссыль на модель, в функции все меняется

            getBoundsBtn.IsEnabled = false;
            descrBoundsBtn.IsEnabled = false;
            
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DescretOptTask input = (DescretOptTask)e.Argument;

            double res = input.StartOptimize(backgroundWorker);
            e.Result = res;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Grid.SetRow(groupBoxBefore, 2);
            Grid.SetRowSpan(groupBoxBefore, 1);
            Grid.SetRowSpan(groupBoxAfter, 2);
            groupBoxBefore.Header = "Начальные границы: U = " + Math.Round((double)e.Result, 6);
            groupBoxAfter.Header = "Дискретная модель: U = " + modelDescret.criteria;

            xOptDescrGrid.ItemsSource = null;
            xOptDescrGrid.ItemsSource = modelDescret.xBounds;
            Log.Text = "Дискретная модель успешно построена!";

            SetModelInfoDescret();
            getBoundsBtn.IsEnabled = true;
            descrBoundsBtn.IsEnabled = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Log.Text += ". ";
        }

        #endregion

        private void markComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (markComboBox.SelectedIndex != -1)
            {
                DBWorker dbConnection = new DBWorker();
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
            }
        }

        private void gostComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gostComboBox.HasItems)
            {
                DBWorker dbConnection = new DBWorker();
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
            }
        }

        private void typeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (typeComboBox.SelectedIndex != -1)
            {
                if (modelDescret == null)
                {
                    Log.Text = "Невозможно задать марку стали, поскольку не выбраны свойства";
                    return;
                }
                modelDescret.GOST = gostComboBox.SelectedItem.ToString();
                modelDescret.mark = markComboBox.SelectedItem.ToString() + " " + typeComboBox.SelectedItem.ToString();
                SetGOST();
            }
                
        }

        private void SetGOST()
        {
            string[] temp;
            if(modelDescret != null) temp = modelDescret.mark.Split(' ');
            else temp = task.mark.Split(' ');
            string[] conds = new string[4];
            conds[0] = "";
            conds[1] = temp[0];
            if (modelDescret != null) conds[2] = modelDescret.GOST;
            else conds[2] = task.GOST;
            conds[3] = temp[1];
            if (modelDescret != null)
            for (int i = 0; i < modelDescret.y.Count; i++)
                conds[0] += task.yAll[modelDescret.y[i]].name + "," + task.yAll[modelDescret.y[i]].name + "_min,";
            else
                for (int i = 0; i < task.y.Count; i++)
                    conds[0] += task.yAll[task.y[i]].name + "," + task.yAll[task.y[i]].name + "_min,";
            conds[0] = conds[0].Trim(',');
            DBWorker dbConnection = new DBWorker();
            DataTable res = dbConnection.GetPlavka(DbSelect.DataY, conds); //извлекаем нужные данные полностью
            dbConnection.CloseConnection();
            yBoundsData.Clear();

            if (modelDescret != null)
                for (int i = 0; i < res.Rows[0].ItemArray.Length; i = i + 2)
                    yBoundsData.Add(new itemGrid(task.yAll[modelDescret.y[i / 2]].description,
                        Convert.ToDouble(res.Rows[0].ItemArray[i + 1]),
                        Convert.ToDouble(res.Rows[0].ItemArray[i])));
            else
                for (int i = 0; i < res.Rows[0].ItemArray.Length; i = i + 2)
                    yBoundsData.Add(new itemGrid(task.yAll[task.y[i / 2]].description,
                        Convert.ToDouble(res.Rows[0].ItemArray[i + 1]),
                        Convert.ToDouble(res.Rows[0].ItemArray[i])));
            yDescrGrid.ItemsSource = null;
            yDescrGrid.ItemsSource = yBoundsData;

            expanderDes.Header = conds[1] + " " + conds[2] + " " + conds[3];
            labelDesGOST.Text = "Стандарт: " + conds[2];
            labelDesMark.Text = "Марка: " + conds[1] + " " + conds[3];
        }

        private void gostMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GostHelper w = new GostHelper();
            w.Show();
        }       

        
    }

    public class itemGrid
    {
        public string header { get; set; }
        public string str { get; set; }
        public double val2 { get; set; }
        public double val3 { get; set; }
        public double val1 { get; set;  }

        public itemGrid(string s, double v1)
        {
            header = s;
            val1 = v1;
            val2 = -1;
        }

        public itemGrid(string s, double v1, double v2)
        {
            header = s;
            val1 = v1;
            val2 = v2;
        }

        public itemGrid(string s, string s2)
        {
            header = s;
            str = s2;
        }
    }

    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((double)value == -1) return "";
            else return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    public class DescretOptTask
    {
        DataPlavka task;
        DescrModel model;
        int numOfIntervals;
        int degree;
        int shift;
        double eps;

        public DescretOptTask(DataPlavka _task, DescrModel _model, int _numOfIntervals, int _degree, int _shift, double _eps)
        {
            task = _task;
            model = _model;
            numOfIntervals = _numOfIntervals;
                degree = _degree;
                shift = _shift;
                eps = _eps;

        }

        public double StartOptimize(BackgroundWorker bcgrwrkr)
        {
            return task.StartBoundsOptimize(numOfIntervals, model, degree, shift, eps, bcgrwrkr);
        }
    }
}
