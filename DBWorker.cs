using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
//using System.Data.SqlClient;
using Oracle.DataAccess.Client;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Threading;

namespace QualitySystem
{
    enum DbSelect
    {
        Columns,
        Marks,
        GOSTS,
        SteelTypes,
        Data,
        DataY, 
        RegID,
        DesID,
        TechID,
        SelectReg, 
        SelectDes,
        SelectTech,
    };

    class DBWorker
    {
        private OracleConnection thisConnection;
        public bool isConnected = false;

        public DBWorker()
        {
            try
            {
                thisConnection = new OracleConnection("Data Source=(DESCRIPTION=(CID=GTU_APP)" + 
                   "(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)" + 
                   "))(CONNECT_DATA=(SID=xe)(SERVER=DEDICATED))); User id=QualitySystem; Password=As_Serg_1312");
                thisConnection.Open();
                isConnected = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Невозможно подключится к базе данных!\n" + ex.Message, "Ошибка");
            }
            
        }

        public DataTable GetPlavka(DbSelect cmdType, object condition)
        {
            OracleCommand cmd = thisConnection.CreateCommand();        
            if (cmdType == DbSelect.Columns) cmd.CommandText = "SELECT * FROM params";
            if (cmdType == DbSelect.Marks) cmd.CommandText = "SELECT DISTINCT marka FROM steel_marks ORDER BY marka";
            if (cmdType == DbSelect.GOSTS) cmd.CommandText = "SELECT DISTINCT gost_id FROM steel_marks WHERE marka = '" + (string)condition +"'";
            if (cmdType == DbSelect.SteelTypes)
            {
                string[] conds = (string[])condition;
                cmd.CommandText = "SELECT DISTINCT vytyazhka FROM steel_marks WHERE marka = '" + conds[0] +"' AND gost_id = '" + conds[1] + "'";
            }
            if (cmdType == DbSelect.Data)
            {
                string[] conds = (string[])condition;
                cmd.CommandText = "SELECT " + conds[0] + " FROM plavka P INNER JOIN Steel_marks S ON P.STE_ID = S.ID WHERE " + conds[1];
            }
            if (cmdType == DbSelect.DataY)
            {
                string[] conds = (string[])condition;
                cmd.CommandText = "SELECT " + conds[0] + " FROM steel_marks WHERE marka = '" + conds[1] + 
                    "' AND gost_id = '" + conds[2] + "' AND vytyazhka = '" + conds[3] + "'";
            }
            
            DataTable result = new DataTable();
            result.Load(cmd.ExecuteReader());
            return result;
        }

        public int GetID(DbSelect cmdType)
        {
            OracleCommand cmd = thisConnection.CreateCommand();
            if (cmdType == DbSelect.RegID) cmd.CommandText = "SELECT max(id) FROM regression_models";
            if (cmdType == DbSelect.DesID) cmd.CommandText = "SELECT max(id) FROM descret_models";
            if (cmdType == DbSelect.TechID) cmd.CommandText = "SELECT max(id) FROM technology";
            DataTable result = new DataTable();
            result.Load(cmd.ExecuteReader());
            int max = Convert.ToInt32(result.Rows[0].ItemArray[0]);
            return max;
        }

        public int GetSTE_ID(string _mark, string _gost)
        {
            OracleCommand cmd = thisConnection.CreateCommand();
            string[] temp = _mark.Split(' ');
            cmd.CommandText = "SELECT id FROM steel_marks WHERE gost_id = '" + _gost + "' AND marka = '" + temp[0] + "' AND vytyazhka = '" + temp[1] + "'";
            DataTable result = new DataTable();
            result.Load(cmd.ExecuteReader());
            int id = Convert.ToInt32(result.Rows[0].ItemArray[0]);
            return id;
        }

        public string[] GetMark(int ste_id)
        {
            OracleCommand cmd = thisConnection.CreateCommand();
            cmd.CommandText = "SELECT GOST_ID, MARKA, VYTYAZHKA FROM steel_marks WHERE ID = " + ste_id.ToString();

            DataTable result = new DataTable();
            result.Load(cmd.ExecuteReader());
            string[] res = new string[3];
            res[0] = result.Rows[0][0].ToString();
            res[1] = result.Rows[0][1].ToString();
            res[2] = result.Rows[0][2].ToString();
            return res;
        }

        public DataTable LoadModel(DbSelect cmdType, int id)
        {
            try
            {
                OracleCommand cmd = thisConnection.CreateCommand();
                if (cmdType == DbSelect.SelectReg) cmd.CommandText = "SELECT * FROM regression_models R INNER JOIN steel_marks S ON R.STE_ID = S.ID WHERE R.ID = " + id.ToString() + " ORDER BY R.Y_ID";
                if (cmdType == DbSelect.SelectDes) cmd.CommandText = "SELECT * FROM descret_models R INNER JOIN steel_marks S ON R.STE_ID = S.ID WHERE R.ID = " + id.ToString();
                if (cmdType == DbSelect.SelectTech) cmd.CommandText = "SELECT * FROM technology R WHERE R.ID = " + id.ToString();

                DataTable result = new DataTable();
                result.Load(cmd.ExecuteReader());
                
                return result;
            }
            catch
            {
                return null;
            }
        }

        public DataTable ViewAvaliableModels(DbSelect cmdType)
        {
            OracleCommand cmd = thisConnection.CreateCommand();
            if (cmdType == DbSelect.SelectReg) 
                cmd.CommandText = "SELECT * FROM regression_models";
            if (cmdType == DbSelect.SelectDes) 
                cmd.CommandText = "SELECT * FROM descret_models";
            if (cmdType == DbSelect.SelectTech)
                cmd.CommandText = "SELECT * FROM technology";
            DataTable result = new DataTable();
            result.Load(cmd.ExecuteReader());
            return result;
        }

        #region insertMethods
        public int InsertModel(DataPlavka task, RegressionModel model)
        {
            
            OracleCommand cmd = thisConnection.CreateCommand();
            int ste_id = GetSTE_ID(model.mark, model.GOST);
            try
            {
                for (int j = 0; j < model.y.Count; j++)
                {
                    string names = "ID,Y_ID,STE_ID,";
                    if(model.isUpgraded) for (int i = 0; i < task.x.Count; i++) names += task.xAll[task.x[i]].name + "_F,";
                    for (int i = 0; i < task.x.Count; i++) names += task.xAll[task.x[i]].name + ",";
                    names += "A,RMSERROR,FISHER,FISHER_VAL,DATE_REG";
                    string values = model.id.ToString() + "," + model.y[j].ToString() + "," + ste_id.ToString() + ",";
                    if (model.isUpgraded) for (int i = 0; i < task.x.Count; i++) values += model.typesF[i].ToString() + ",";
                    for (int i = 0; i < model.equation[j].weights.Length; i++) values += 
                        (Math.Round(model.equation[j].weights[i],15)).ToString(CultureInfo.InvariantCulture) + ",";// и свободный ЧЛЕН
                    values += model.equation[j].rmserror.ToString(CultureInfo.InvariantCulture) + "," + model.equation[j].F.ToString(CultureInfo.InvariantCulture) + "," + Math.Round(model.equation[j].F_test,15).ToString(CultureInfo.InvariantCulture);
                    values += ", to_date('" + DateTime.Now.Date.ToShortDateString() + "', 'dd/mm/yyyy')";
                    cmd.CommandText = "INSERT INTO regression_models (" + names + ") values (" + values + ")";
                    cmd.ExecuteReader();
                    
                }
            }
            catch
            {
                return -1;
            }
            return 1;
        }
        
        public int InsertModel(DataPlavka task, DescrModel model)
        {

            OracleCommand cmd = thisConnection.CreateCommand();
            int ste_id = GetSTE_ID(model.mark, model.GOST);
            try
            {
                    string names = "ID,STE_ID,ALPHA,BETA,U,YS,DATE_DES";
                    for (int i = 0; i < model.x.Count; i++) names += "," + task.xAll[model.x[i]].name + "_MIN";
                    for (int i = 0; i < model.x.Count; i++) names += "," + task.xAll[model.x[i]].name;

                    string values = model.id.ToString() + "," + ste_id.ToString() + "," + model.alpha.ToString(CultureInfo.InvariantCulture) + ","
                        + model.beta.ToString(CultureInfo.InvariantCulture) + "," + model.criteria.ToString(CultureInfo.InvariantCulture) + ",";
                string ys = model.y[0].ToString();
                for (int w = 1; w < model.y.Count; w++) ys += "@" + model.y[w];
                values += "'" + ys + "',";
                    values += " to_date('" + DateTime.Now.Date.ToShortDateString() + "', 'dd/mm/yyyy')";

                    for (int i = 0; i < task.x.Count; i++) values += "," + model.xBounds[i].lower.ToString(CultureInfo.InvariantCulture);
                    for (int i = 0; i < task.x.Count; i++) values += "," + model.xBounds[i].upper.ToString(CultureInfo.InvariantCulture);
                   
                    cmd.CommandText = "INSERT INTO descret_models (" + names + ") values (" + values + ")";
                    cmd.ExecuteReader();
            }
            catch
            {
                return -1;
            }
            return 1;
        }

        public int InsertModel(DataPlavka task, Technology technology)
        {

            OracleCommand cmd = thisConnection.CreateCommand();
            int ste_id = GetSTE_ID(technology.mark, technology.GOST);
            try
            {
                string names = "ID,STE_ID,REG_ID,DES_ID,DATE_TECH,BASE_ID,RMSERROR";
                for (int i = 0; i < task.x.Count; i++) names += "," + task.xAll[task.x[i]].name;
                for (int i = 0; i < task.y.Count; i++) names += "," + task.yAll[task.y[i]].name;
                string values = technology.id.ToString() + "," + ste_id.ToString() + "," + technology.regressID.ToString() + "," +
                    technology.descretID.ToString() + ", to_date('" + DateTime.Now.Date.ToShortDateString() + "', 'dd/mm/yyyy')," +
                   technology.baseTechnologyID.ToString() + "," + Math.Round(technology.rmserror,15).ToString(CultureInfo.InvariantCulture);
               
                for (int i = 0; i < task.x.Count; i++) values += "," + technology.xOpt[i].ToString(CultureInfo.InvariantCulture);
                for (int i = 0; i < task.y.Count; i++) values += "," + technology.yOpt[i].ToString(CultureInfo.InvariantCulture);
                
                cmd.CommandText = "INSERT INTO technology (" + names + ") values (" + values + ")";
                cmd.ExecuteReader();
            }
            catch
            {
                return -1;
            }
            return 1;
        }

        public int InsertGOST(string names, string values)
        {
            OracleCommand cmd = thisConnection.CreateCommand();
            cmd.CommandText = "SELECT max(id) FROM steel_marks";
            double id = Convert.ToDouble(cmd.ExecuteScalar());
            names = "ID," + names;
            values = (id+1).ToString() + "," + values;
            cmd.CommandText = "INSERT INTO steel_marks (" + names + ") values (" + values + ")";
            try
            {
                cmd.ExecuteReader();
            }
            catch
            {
                return -1;
            }
            return 1;
        }
        #endregion

        public void CloseConnection()
        {
            thisConnection.Close();
        }
    }
}
