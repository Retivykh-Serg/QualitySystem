using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace QualitySystem
{

    public class DataPlavka : Model
    {
        public DataTable plavka; //таблица со всеми исходными данными
        public double[,] data;
        public List<DataParametr> xAll; //список доступных параметров с описаниями
        public List<DataParametr> yAll; // список доступных свойств с описаниями
        //public List<int> x; //список индексов выбранных параметров из списка xAll
        //public List<int> y;
        //public string mark;
        //public string gost;
        //НЕ ЗАБЫТЬ СОРТИРОВАТЬ ИКС! TODO
        public DataPlavka(DataTable _paramsTable)
        {
            xAll = new List<DataParametr>();
            yAll = new List<DataParametr>();
            x = new List<int>();
            y = new List<int>();
            for (int i = 0; i < _paramsTable.Rows.Count; i++)
            {
                if (_paramsTable.Rows[i].ItemArray[1].ToString().Contains('y'))
                    yAll.Add(new DataParametr(_paramsTable.Rows[i]));
                if (_paramsTable.Rows[i].ItemArray[1].ToString().Contains('x'))
                    xAll.Add(new DataParametr(_paramsTable.Rows[i]));
            }
            plavka = new DataTable();
        }

        public DataPlavka(DataPlavka _fromDB) //копируем, но создаем массив даюл значений вместо таблицы
        {
            mark = _fromDB.mark;
            GOST = _fromDB.GOST;
            xAll = new List<DataParametr>();
            yAll = new List<DataParametr>();
            x = new List<int>();
            y = new List<int>();
            for (int i = 0; i < _fromDB.x.Count; i++) x.Add(_fromDB.x[i]);
            for (int i = 0; i < _fromDB.y.Count; i++) y.Add(_fromDB.y[i]);
            for (int i = 0; i < _fromDB.xAll.Count; i++) xAll.Add(_fromDB.xAll[i]);
            for (int i = 0; i < _fromDB.yAll.Count; i++) yAll.Add(_fromDB.yAll[i]);
            data = new double[_fromDB.plavka.Rows.Count, _fromDB.plavka.Columns.Count];
            for (int i = 0; i < _fromDB.plavka.Rows.Count; i++)
                for (int j = 0; j < _fromDB.plavka.Columns.Count; j++)
                    data[i, j] = Convert.ToDouble(_fromDB.plavka.Rows[i].ItemArray[j]);
        }

        private int[] UpgradeFuncModel() //находим функции для Х, при которых ковариация с У максимальна
        { 
            int[] funcTypes = new int[x.Count];
            int numNotes = data.GetLength(0);
            int numParams = x.Count;

            for (int i = 0; i < numParams; i++) //для каждого параметра 
            {
                double[] Y = new double[numNotes], Xbase = new double[numNotes], X = new double[numNotes];
                for (int w = 0; w < numNotes; w++) Xbase[w] = data[w, y.Count+i]; //начальный массив X
                double max_sum = -1;
                for (int k = 0; k < FunctionTypeConverter.funcNum; k++)
                {
                    double temp_sum = 0;
                    for (int w = 0; w < numNotes; w++) X[w] = FunctionTypeConverter.CalcFx(k, Xbase[w]);
                    for (int j = 0; j < y.Count; j++) //суммируем корреляции со всеми У
                    {
                        for (int w = 0; w < numNotes; w++) Y[w] = data[w, j];  //Convert.ToDouble(plavka.Rows[w].ItemArray[j]); //заполнили массив свойства
                        temp_sum += Math.Pow(alglib.pearsoncorr2(X, Y), 2);
                    }
                    if (temp_sum > max_sum)
                    {
                        max_sum = temp_sum;
                        funcTypes[i] = k;
                    }
                }
            }

            return funcTypes;
        }

        private RegEquation BuildOneModel(double[,] xy, int numParams, int numNotes, ref int info) //строим модель, в инфо - успех или нет
        {
            double[] weights = new double[numParams + 1];
            double Ysr = 0;
            alglib.linreg.linearmodel LM = new alglib.linreg.linearmodel();
            alglib.linreg.lrreport AR = new alglib.linreg.lrreport();
            alglib.linreg.lrbuild(xy, numNotes, numParams, ref info, LM, AR);
            if (info < 0) return null;
            alglib.linreg.lrunpack(LM, ref weights, ref numParams); //получили веса моделей, свободный член последний
            
            //проверим на значимость по Фишеру
            for (int i = 0; i < numNotes; i++)
                Ysr += xy[i, numParams];
            Ysr = Ysr / numNotes;

            double SSreg = 0, SSresid = 0;
            for (int j = 0; j < numNotes; j++)
            {
                double Ymodel = 0;
                for (int k = 0; k < numParams; k++)
                    Ymodel += xy[j, k] * weights[k];
                Ymodel += weights[numParams]; //прибавили свободный ЧЛЕН
                SSreg += Math.Pow(Ymodel - Ysr, 2);
                SSresid += Math.Pow(Ymodel - xy[j, numParams], 2);
            }
            SSreg = SSreg / numParams;
            SSresid = SSresid / (numNotes - numParams - 1);
            double F = SSreg / SSresid;
            double F_test = alglib.fcdistribution(numParams, numNotes - numParams - 1, F);

            
            return new RegEquation(weights, F, F_test, AR.rmserror);
        }

        public RegressionModel LinReg(bool isUpgraded) //строим линейную регрессию для одного У
        {
            RegressionModel models = new RegressionModel(x, y, isUpgraded);
            models.GOST = GOST;
            models.mark = mark;
            int[] typesF = new int[x.Count];
            int numNotes = data.GetLength(0);
            int numParams = x.Count;
            int info = 100; //результат выполнения подпрограммы alglib
            double[,] xy = new double[numNotes, numParams + 1]; //таблица из всех иксов и одного у 
            //подготавливаем массив данных
            if (!isUpgraded) //для простой модели просто переносим все данные
            {
                for (int i = 0; i < numNotes; i++)
                    for (int j = 0; j < numParams; j++)
                        xy[i, j] = data[i, y.Count + j];  //Convert.ToDouble(plavka.Rows[i].ItemArray[y.Count + j]); //поскольку сначала все y
            }
            else typesF = UpgradeFuncModel();

            for (int i = 0; i < y.Count; i++) //для каждого свойства y 
            {
                if (isUpgraded) //для улучшенной модели применяем функции
                {
                    for (int k = 0; k < numNotes; k++)
                        for (int j = 0; j < numParams; j++)
                            xy[k, j] = FunctionTypeConverter.CalcFx(typesF[j], data[k, y.Count + j]); // Convert.ToDouble(plavka.Rows[k].ItemArray[y.Count + j]));
                    models.typesF = typesF;
                }

                for (int j = 0; j < numNotes; j++)
                    xy[j, numParams] = data[j,i];// Convert.ToDouble(plavka.Rows[j].ItemArray[i]); //заполняем последний столбец для нужного свойства
                
                //строим 1 модель для конкретного свойства
                RegEquation model = BuildOneModel(xy, numParams, numNotes, ref info);
                if (info < 0) //ошибка произошла, возвращаем ее в классе модели
                {
                    models.buildInfo = info;
                    return models;
                }
                models.equation.Add(model);
            }
            return models;
        }

        public DescrModel GetPlavkaBounds()
        {
            DescrModel model = new DescrModel(x, y, 0, 0);
            double lower, upper;
            for (int i = 0; i < x.Count; i++) //границы для Х
            {
                lower = data[0, y.Count + i];// Convert.ToDouble(plavka.Rows[0].ItemArray[y.Count + i]);
                upper = data[0, y.Count + i]; // Convert.ToDouble(plavka.Rows[0].ItemArray[y.Count + i]); 
                for (int j = 1; j < data.GetLength(0); j++) //0 вверху 2 строки
                {
                    if (data[j, y.Count + i] < lower) lower = data[j, y.Count + i];
                    if (data[j, y.Count + i] > upper) upper = data[j, y.Count + i];
                }
                model.xBounds.Add(new Conditions(lower, upper, xAll[x[i]].description));
            }
            for (int i = 0; i < y.Count; i++) //границы для Y
            {
                lower = data[0, i]; //Convert.ToDouble(plavka.Rows[0].ItemArray[i]);
                upper = data[0, i]; // Convert.ToDouble(plavka.Rows[0].ItemArray[i]);
                for (int j = 1; j <data.GetLength(0); j++) //0 вверху 2 строки
                {
                    if (data[j, i] < lower) lower = data[j, i];
                    if (data[j, i] > upper) upper = data[j, i];
                }
                model.yBounds.Add(new Conditions(lower, upper, yAll[y[i]].description));
            }
            model.criteria = 0;
            return model;
        }

        public void CorrectBoundsY(DescrModel model) //если не задана верхняя граница - берем максимум из выборки
        {
            double upper;
            for (int i = 0; i < x.Count; i++) //границы для Х
                if (model.xBounds[i].upper == 0)
                {
                    upper = data[0, y.Count + i]; // Convert.ToDouble(plavka.Rows[0].ItemArray[y.Count + i]); 
                    for (int j = 1; j < data.GetLength(0); j++) //0 вверху 2 строки
                        if (data[j, y.Count + i] > upper) upper = data[j, y.Count + i];
                    model.xBounds[i].upper = upper;
                }
            for (int i = 0; i < y.Count; i++) //границы для Y
            {
                if (model.yBounds[i].upper == 0)
                {
                    upper = data[0, i]; // Convert.ToDouble(plavka.Rows[0].ItemArray[i]);
                    for (int j = 1; j < data.GetLength(0); j++) //0 вверху 2 строки
                        if (data[j, i] > upper) upper = data[j, i];
                    model.yBounds[i].upper = upper;
                }
            }
        }

        public double StartBoundsOptimize(int numIntervals, DescrModel model, int degree, int boundsShift, double eps, 
            System.ComponentModel.BackgroundWorker backgroundWorker)
        {
            int[] intervals = new int[numIntervals];
            for(int i=0; i<numIntervals; i++) intervals[i] = i; // берем все нулевые интервалы
            int[][] vars = new int[x.Count][];
            for (int i = 0; i < x.Count; i++) vars[i] = intervals;
            var cross = new CartesianProduct<int>(vars);
            IEnumerable<int[]> intervalVars = cross.Get();

            double maxCriteria = -1000000;
            List<Conditions> bestBounds = new List<Conditions>();
            foreach (int[] variant in intervalVars) //выбор начального подпространства
            {
                List<Conditions> bounds = model.IntervalsBounds(numIntervals, variant);
                 double res = OneIntervalCriteria(bounds, model);
                 
                if (res > maxCriteria) //здесь оцениваем интервал, просто сравниваем критерий
                {
                    backgroundWorker.ReportProgress(1);
                    maxCriteria = res;
                    bestBounds.Clear();
                    for (int i = 0; i < bounds.Count; i++) bestBounds.Add(new Conditions(bounds[i]));
                }          
            }
            //поехали двигать границы
            backgroundWorker.ReportProgress(2);
            DescrModel modelMaxBounds = GetPlavkaBounds();
            bool gettingGlobalBetter = true;
            while (gettingGlobalBetter)
            {
                backgroundWorker.ReportProgress(3);
                gettingGlobalBetter = false;
                for (int i = 0; i < x.Count; i++) //для каждого параметра смотрим
                {
                    bool gettingBetter = true; //пока изменяются границы хотя бы одного интервала - пересматриваем заново
                    while (gettingBetter) //повторяем пока сдвиги границ дают результат
                    {
                        gettingBetter = false;//пока изменяются границы внутри интервала - пересматриваем заново
                        //double size = bestBounds[i].upper - bestBounds[i].lower;
                        List<Conditions> newBestBounds = new List<Conditions>();
                        
                        for (int v = 0; v < 4; v++)
                            for (int j = 0; j < degree + 1; j++)
                            {
                                List<Conditions> newBounds = new List<Conditions>();
                                foreach (Conditions c in bestBounds) newBounds.Add(new Conditions(c));
                                if (v == 0) //увеличиваем интервал верхней границы
                                    newBounds[i].upper += Math.Abs(modelMaxBounds.xBounds[i].upper - bestBounds[i].upper) / Math.Pow(boundsShift, i);
                                if (v == 1) //уменьшаем интервал
                                    newBounds[i].upper -= ( bestBounds[i].upper - bestBounds[i].lower) / Math.Pow(boundsShift, i + 1); //уменьшаем интервал
                                if (v == 2) //увеличиваем интервал нижней границы
                                    newBounds[i].lower += Math.Abs(modelMaxBounds.xBounds[i].lower - bestBounds[i].lower) / Math.Pow(boundsShift, i); 
                                if (v == 3) //уменьшаем интервал
                                    newBounds[i].lower -= (bestBounds[i].upper - bestBounds[i].lower) / Math.Pow(boundsShift, i + 1); 
                                double res = OneIntervalCriteria(newBounds, model);
                                if (backgroundWorker.WorkerReportsProgress) backgroundWorker.ReportProgress(1);
                                if (res > maxCriteria) //здесь оцениваем интервал, просто сравниваем критерий
                                {
                                    backgroundWorker.ReportProgress(1);
                                    gettingBetter = true;
                                    if( (Math.Abs(res) - Math.Abs(maxCriteria)) > eps) gettingGlobalBetter = true;
                                    maxCriteria = res;
                                    newBestBounds.Clear();
                                    for (int w = 0; w < newBounds.Count; w++) newBestBounds.Add(new Conditions(newBounds[w])); //новые улучшенные границы
                                }
                            }
                        if (gettingBetter)
                        {
                            bestBounds.Clear();
                            for (int w = 0; w < newBestBounds.Count; w++) bestBounds.Add(new Conditions(newBestBounds[w]));
                        }
                    }
                }
            }
            double beforeCriteria = OneIntervalCriteria(model.xBounds, model);
            model.criteria = maxCriteria;
            model.SetBounds(bestBounds);

            return beforeCriteria;
        }

        public double OneIntervalCriteria(List<Conditions> bounds, DescrModel model)
        {
            int Tplus = 0, Splus = 0, TSplus = 0;
            for (int i = 0; i < data.GetLength(0); i++)
            {
                bool flagOk = false;
                for (int q = 0; q < y.Count; q++)
                    if (!(data[i, q] >= model.yBounds[q].lower && data[i, q] <= model.yBounds[q].upper)) break;//не попыл - выходим сразу
                    else if (q == y.Count - 1)
                    {
                        flagOk = true;
                        Splus++;
                    }
                for (int q = 0; q < x.Count; q++)
                    if (!(data[i, q + y.Count] >= bounds[q].lower && data[i, q + y.Count] <= bounds[q].upper)) break;
                    else if (q == x.Count - 1)
                    {
                        Tplus++;
                        if (flagOk) TSplus++;
                    }
            }
            if (Tplus > 0 && Splus > 0)
                return model.CalcCriteria(data.GetLength(0), Tplus, Splus, TSplus);
            else return -1000;
        }
    }


    public class DataParametr
    {
        public string name;
        public string description;

        public DataParametr(DataRow dr)
        {
            name = dr.ItemArray[0].ToString();
            description = dr.ItemArray[2].ToString();
        }
    }

    public class Model
    {
        public int id;
        public DateTime date;
        public string mark;
        public string GOST;
        public List<int> x; //список индексов исследуемых параметров
        public List<int> y; //список индексов свойств
    }

}
