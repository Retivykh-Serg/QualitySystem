using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QualitySystem
{
    class CondOptModel
    {
        private RegressionModel regModel;
        private DescrModel desModel;
        public Technology technology;

        public double[] yModel; //смоделированные значения свойств
        public double[] xDone;
        public int numDone; //число введенных завершенных переделов
        
        double epsg = 0.000001; //The  subroutine  finishes  its  work   if   the  condition |v|<EpsG is satisfied
        double epsx = 0.00001; //>0, stopping condition on outer iteration step length
        double epsi = 0.00001; //stopping condition on infeasibility
        int iter;

        public CondOptModel(RegressionModel regr, DescrModel descr, double[] _y, double _epsg, double _epsi, int _iter)
        {
            regModel = regr;
            desModel = descr;
            epsg = _epsg;
            epsx = _epsi;
            epsi = _epsi;
            iter = _iter;
            yModel = new double[regModel.y.Count];

            technology = new Technology(regr.x, regr.y, descr.id, regr.id, _y);
        }

        public CondOptModel(RegressionModel regr, DescrModel descr, Technology tech, double _epsg, double _epsi, int _iter, double[] _xCur)
        {
            regModel = regr;
            desModel = descr;
            epsg = _epsg;
            epsx = _epsi;
            epsi = _epsi;
            iter = _iter;
            yModel = new double[regModel.y.Count];
            technology = tech;
            technology.baseTechnologyID = tech.id;
            technology.id = -1;
            xDone = _xCur;
            for (int i = 0; i < xDone.Length; i++)
                if (xDone[i] == -1)
                {
                    numDone = i;
                    break;
                }
        }

        public void functionOpt(double[] x, ref double func, object obj)
        {
            func = 0;
            for (int i = 0; i < regModel.y.Count; i++)
                if (regModel.isUpgraded) func += Math.Pow(regModel.equation[i].Calculate(x, regModel.typesF) - technology.yOpt[i], 2);
                else func += Math.Pow(regModel.equation[i].Calculate(x) - technology.yOpt[i], 2);
        }

        public void functionCor(double[] x, ref double func, object obj)
        {
            for (int i = 0; i < regModel.x.Count - numDone; i++)
                xDone[numDone + i] = x[i];
            func = 0;
            for (int i = 0; i < regModel.y.Count; i++)
                if (regModel.isUpgraded) func += Math.Pow(regModel.equation[i].Calculate(xDone, regModel.typesF) - technology.yOpt[i], 2);
                else func += Math.Pow(regModel.equation[i].Calculate(xDone) - technology.yOpt[i], 2);
        }

        public Technology StartOptimize()
        {            
            alglib.minbleicstate state;
            alglib.minbleicreport rep;
            double[] xSolved = new double[regModel.x.Count];
            for (int i = 0; i < xSolved.Length; i++) xSolved[i] = 0.01;
            double diffstep = 1.0e-6;
            alglib.minbleiccreatef(xSolved, diffstep, out state);
            
            //else alglib.minbleiccreate(xOpt, out state);
            // if (!regModel.isUpgraded) 
                alglib.minbleicsetbc(state, desModel.Bounds(true, true), desModel.Bounds(true, false)); //границы из дискретной модели

            alglib.minbleicsetinnercond(state, epsg, 0, 0);
            alglib.minbleicsetoutercond(state, epsx, epsi);
            alglib.minbleicsetmaxits(state, iter);
           
            alglib.minbleicoptimize(state, functionOpt, null, null);
            alglib.minbleicresults(state, out xSolved, out rep);

            for (int i = 0; i < xSolved.Length; i++) technology.xOpt.Add(xSolved[i]);
            
            for (int i = 0; i < regModel.y.Count; i++)
            {
                if (regModel.isUpgraded) yModel[i] = regModel.equation[i].Calculate(xSolved,regModel.typesF);
                else yModel[i] = regModel.equation[i].Calculate(xSolved);
                technology.error[i] = Math.Abs(yModel[i] - technology.yOpt[i]);
                technology.rmserror += Math.Pow(technology.error[i], 2);
            }
            return technology;
        }

        public Technology StartCorrect()
        {           
            int numParams = regModel.x.Count - numDone;

            alglib.minbleicstate state;
            alglib.minbleicreport rep;
            double[] xSolved = new double[numParams];
            for (int i = 0; i < xSolved.Length; i++) xSolved[i] = 0.01;
            double diffstep = 1.0e-9;
            alglib.minbleiccreatef(xSolved, diffstep, out state);

            alglib.minbleicsetbc(state, desModel.Bounds(true, true, numDone), desModel.Bounds(true, false, numDone)); //границы из дискретной модели

            alglib.minbleicsetinnercond(state, epsg, 0, 0);
            alglib.minbleicsetoutercond(state, epsx, epsi);
            alglib.minbleicsetmaxits(state, iter);

            alglib.minbleicoptimize(state, functionCor, null, null);
            alglib.minbleicresults(state, out xSolved, out rep);
            technology.xOpt.Clear();
            for (int i = 0; i < numDone; i++) technology.xOpt.Add(xDone[i]);
            for (int i = 0; i < xSolved.Length; i++)
            {
                technology.xOpt.Add(xSolved[i]);
                xDone[i + numDone] = xSolved[i];
            }

            for (int i = 0; i < regModel.y.Count; i++)
            {
                if (regModel.isUpgraded) yModel[i] = regModel.equation[i].Calculate(xDone, regModel.typesF);
                else yModel[i] = regModel.equation[i].Calculate(xDone);
                technology.error[i] = Math.Abs(yModel[i] - technology.yOpt[i]);
                technology.rmserror += Math.Pow(technology.error[i], 2);
            }
            return technology;
        }

    }

    public class Technology : Model
    {
        public int descretID;
        public int regressID;
        public bool isCorrected;
        public int baseTechnologyID;
        public List<double> xOpt;
        public List<double> yOpt;
        public double[] error; //невязка по каждому свойству
        public double rmserror;

        public Technology(List<int> _x, List<int> _y, int dId, int rId, double[] _yOpt)
        {
            id = -1;
            baseTechnologyID = -1;
            x = new List<int>();
            y = new List<int>();
            for (int i = 0; i < _x.Count; i++) x.Add(_x[i]);
            for (int i = 0; i < _y.Count; i++) y.Add(_y[i]);
            xOpt = new List<double>();
            yOpt = new List<double>();
            for (int i = 0; i < y.Count; i++) yOpt.Add(_yOpt[i]);
            error = new double[y.Count];
            rmserror = 0;
            descretID = dId;
            regressID = rId;
            isCorrected = false;
        }

        public Technology(int _id)
        {
            id = _id;
            baseTechnologyID = -1;
            x = new List<int>();
            y = new List<int>();
            xOpt = new List<double>();
            yOpt = new List<double>();
            isCorrected = false;
        }
    }
    
}
