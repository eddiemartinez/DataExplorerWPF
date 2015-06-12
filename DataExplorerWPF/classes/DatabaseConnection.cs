using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataExplorerWPF
{
    class DatabaseConnection
    {
        private string connstr;
        private string FileName = "";
        private string dbpassword = "";

        public string connectionString
        {
            set
            {
                connstr = value;
            }
        }
        public OleDbConnection GetDb
        {
            get
            {
                return GetConnection(FileName, dbpassword);
            }
        }
        //Open DBConnection Method
        public static OleDbConnection GetConnection(string FileName, string dbpassword)
        {
            String connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + FileName + ";Jet OLEDB:Database Password= " + dbpassword + "";
            OleDbConnection conn = new OleDbConnection(connectionString);
            try
            {
                conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not open db connection ! " + ex.Message);
            }
            return conn;
        }
        //Close DBConnection Method
        public static void CloseConnection(OleDbConnection conn)
        {
            try
            {
                conn.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
