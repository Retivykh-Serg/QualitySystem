using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QualitySystem
{

    public class RegEquation
    {
        public double[] weights;
        //public int[] typesF; //виды функций если улучшенная модель

        private double f;
        public double F { get { return Math.Round(f, 5); } set { f = value; } }//критерий фишера
        public double F_test { get; set; } //значимость критерия
        private double rms;
        public double rmserror { get { return Math.Round(rms, 5); } set { rms = value; } } //стандартная ошибка
        public string descr { get; set; } //для отображения в датаГрид

        public RegEquation(double[] m, double _F, double _F_t, double _rms)
        {
            weights = new double[m.Length];
            for (int i = 0; i < m.Length; i++) weights[i] = m[i];
            F = _F;
            F_test = _F_t;
            rmserror = _rms;
        }

        public double Calculate(double[] x)
        {
            double result = 0;
            for (int i = 0; i < x.Length; i++)
                result += x[i] * weights[i];
            result += weights[x.Length]; //прибавляем свободный член
            return result;
        }

        public double Calculate(double[] x, int[] typesF)
        {
            double result = 0;
            for (int i = 0; i < x.Length; i++)
                result += FunctionTypeConverter.CalcFx(typesF[i], x[i]) * weights[i];
            result += weights[x.Length]; //прибавляем свободный член
            return result;
        }
    }

    public class RegressionModel : Model
    {
        public int buildInfo; //1 - успех, -1 - мало данных, -255 хз что
        public bool isUpgraded; //тип модели - улучшенная или нет
        public List<RegEquation> equation; //уравнения частных моделей по у
        public int[] typesF;

        public RegressionModel(List<int> _x, List<int> _y, bool _isUpgraded)
        {
            id = -1;
            x = new List<int>();
            y = new List<int>();
            for (int i = 0; i < _x.Count; i++) x.Add(_x[i]);
            for (int i = 0; i < _y.Count; i++) y.Add(_y[i]);
            equation = new List<RegEquation>();
            isUpgraded = _isUpgraded;
            if (_isUpgraded)
                typesF = new int[x.Count];
        }

        public RegressionModel(int _id)
        {
            id = _id;
            x = new List<int>();
            y = new List<int>();
            equation = new List<RegEquation>();
        }

    }

    public static class FunctionTypeConverter
    {
        //степени если отрицательные, то обязательно со знаком минус!!!
        private static string[] headers = { "x", "x^2", "x^3", "x^(-1)", "x^(-2)", "x^(-3)", "x^(1/2)", "x^(1/3)", "x^(-1/2)", "x^(-1/3)", "Ln(x)" };
        public static int funcNum = 11;

        public static double CalcFx(int type, double x)
        {
            switch (type)
            {
                case 1: return Math.Pow(x, 2);
                case 2: return Math.Pow(x, 3);
                case 3: return Math.Pow(x, -1);
                case 4: return Math.Pow(x, -2);
                case 5: return Math.Pow(x, -3);
                case 6: return Math.Pow(x, 1.0 / 2);
                case 7: return Math.Pow(x, 1.0 / 3);
                case 8: return Math.Pow(x, -1.0 / 2);
                case 9: return Math.Pow(x, -1.0 / 3);
                case 10: return Math.Log(x);
                default: return x;
            }
        }

        public static double CalcFxInv(int type, double x)
        {
            double res = 0;
            switch (type)
            {
                case 1: res = Math.Pow(x, 1.0 / 2); return res;
                case 2: res = Math.Pow(x, 1.0 / 3); return res;
                case 3: res = Math.Pow(x, -1); return res;
                case 4: res = Math.Pow(x, -1.0 / 2); return res;
                case 5: res = Math.Pow(x, -1.0 / 3); return res;
                case 6: res = Math.Pow(x, 2); return res;
                case 7: res = Math.Pow(x, 3); return res;
                case 8: res = Math.Pow(x, -2); return res;
                case 9: res = Math.Pow(x, -3); return res;
                case 10: res = Math.Exp(x); return res;
                default: return x;
            }
        }

        public static string ViewFx(int type)
        {
            return headers[type];
        }
    }
}
