using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QualitySystem
{

    public class DescrModel : Model
    {
        public List<Conditions> xBounds;
        public List<Conditions> yBounds;

        public double alpha;
        public double beta;
        public double criteria;

        public DescrModel(List<int> _x, List<int> _y, double _alpha, double _beta)
        {
            id = -1;
            x = new List<int>();
            y = new List<int>();
            for (int i = 0; i < _x.Count; i++) x.Add(_x[i]);
            for (int i = 0; i < _y.Count; i++) y.Add(_y[i]);
            xBounds = new List<Conditions>();
            yBounds = new List<Conditions>();
            alpha = _alpha;
            beta = _beta;
        }

        public DescrModel(List<int> _x, List<int> _y)
        {
            id = -1;
            x = new List<int>();
            y = new List<int>();
            for (int i = 0; i < _x.Count; i++) x.Add(_x[i]);
            for (int i = 0; i < _y.Count; i++) y.Add(_y[i]);
            xBounds = new List<Conditions>();
            yBounds = new List<Conditions>();
        }

        public DescrModel(int _id)
        {
            id = _id;
            x = new List<int>();
            y = new List<int>();
            xBounds = new List<Conditions>();
            yBounds = new List<Conditions>();
        }

        private Conditions InteravalBounds(int numParam, int numIntervals, int curInterval) //получаем границы подинтервала, считаем их с нуля
        {
            double intervalStep = (xBounds[numParam].upper-xBounds[numParam].lower) / numIntervals;
            double newLower = xBounds[numParam].lower + curInterval * intervalStep;
            double newUpper =  newLower + intervalStep;
            return new Conditions(newLower, newUpper, xBounds[numParam].descr);
        }

        public List<Conditions> IntervalsBounds(int numIntervals, int[] curIntervals)
        {
            List<Conditions> res = new List<Conditions>();
            for (int i = 0; i < x.Count; i++)
                res.Add(InteravalBounds(i, numIntervals, curIntervals[i]));
            return res;
        }

        public double CalcCriteria(int num, int Tplus, int Splus, int TplusSplus)
        {
            double res = -fLnF((double)Tplus / num) - fLnF(1 - (double)Tplus / num) - fLnF((double)Splus / num) - fLnF(1 - (double)Splus / num);
            res += alpha * Math.Log((double)TplusSplus / num) - beta * ((double)Tplus / num - (double)TplusSplus / num);
            res += fLnF((double)Splus / num - (double)TplusSplus / num) + fLnF(1 - (double)Tplus / num - (double)Splus / num + (double)TplusSplus / num);
            return res;
        }

        private double fLnF(double val)
        {
            if (val == 0) val = 0.00001;
            return  val* Math.Log(val);
        }

        public double[] Bounds(bool isX, bool isLowerBound)
        {
            double[] res = new double[x.Count];
            for (int i = 0; i < x.Count; i++)
                if (isX)
                    if (isLowerBound) res[i] = xBounds[i].lower;
                    else res[i] = xBounds[i].upper;
                else
                    if (isLowerBound) res[i] = yBounds[i].lower;
                    else res[i] = yBounds[i].upper;
            return res;
        }

        public double[] Bounds(bool isX, bool isLowerBound, int position)
        {
            double[] res = new double[x.Count - position];
            for (int i = 0; i < x.Count - position; i++)
                if (isX)
                    if (isLowerBound) res[i] = xBounds[i + position].lower;
                    else res[i] = xBounds[i + position].upper;
                else
                    if (isLowerBound) res[i] = yBounds[i + position].lower;
                    else res[i] = yBounds[i + position].upper;
            return res;
        }

        public void SetBounds(List<Conditions> _Bounds)
        {
            xBounds.Clear();
            for (int i = 0; i < _Bounds.Count; i++)
                xBounds.Add(new Conditions(_Bounds[i]));
        }

        public void SetBounds(List<itemGrid> _BoundsX, List<itemGrid> _BoundsY)
        {
            double val1, val2;
            xBounds.Clear();
            for (int i = 0; i < _BoundsX.Count; i++)
            {
                if (_BoundsX[i].val1 == -1) val1 = 0; else val1 = _BoundsX[i].val1;
                if (_BoundsX[i].val2 == -1) val2 = 10 * val1; else val2 = _BoundsX[i].val2;
                xBounds.Add(new Conditions(val1, val2, _BoundsX[i].header));
            }
            yBounds.Clear();
            for (int i = 0; i < _BoundsY.Count; i++)
            {
                if (_BoundsY[i].val1 == -1) val1 = 0; else val1 = _BoundsY[i].val1;
                if (_BoundsY[i].val2 == -1) val2 = 10 * val1; else val2 = _BoundsY[i].val2;
                yBounds.Add(new Conditions(val1, val2, _BoundsY[i].header));
            }
        }

    }

    public class Conditions
    {
        public double lower { get; set; }
        public double upper { get; set; }
        public string descr { get; set; } //для отображения в датаГрид

        public Conditions(double l, double u, string s)
        {
            lower = l;
            upper = u;
            descr = s;
        }

        public Conditions(Conditions c)
        {
            lower = c.lower;
            upper = c.upper;
            descr = c.descr;
        }
    }

    public class CartesianProduct<T>
    {
        int[] lengths;
        T[][] arrays;
        public CartesianProduct(params  T[][] arrays)
        {
            lengths = arrays.Select(k => k.Length).ToArray();
            if (lengths.Any(l => l == 0))
                throw new ArgumentException("Zero lenght array unhandled.");
            this.arrays = arrays;
        }
        public IEnumerable<T[]> Get()
        {
            int[] walk = new int[arrays.Length];
            int x = 0;
            yield return walk.Select(k => arrays[x++][k]).ToArray();
            while (Next(walk))
            {
                x = 0;
                yield return walk.Select(k => arrays[x++][k]).ToArray();
            }

        }
        private bool Next(int[] walk)
        {
            int whoIncrement = 0;
            while (whoIncrement < walk.Length)
            {
                if (walk[whoIncrement] < lengths[whoIncrement] - 1)
                {
                    walk[whoIncrement]++;
                    return true;
                }
                else
                {
                    walk[whoIncrement] = 0;
                    whoIncrement++;
                }
            }
            return false;
        }
    }
     
}
