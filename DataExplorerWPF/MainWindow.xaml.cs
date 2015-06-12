using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.Reflection;

namespace DataExplorerWPF
{
    /// DataBase Explorer
    /// Author: Eddie Martinez
    /// Date: 3/9/2015

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private delegate void DelegateOpenFile(String s);
        DelegateOpenFile openFileDelegate;
        public MainWindow()
        {
            Loaded += MyWindow_Loaded;
            InitializeComponent();
            Percentage.Content = String.Empty;
            Results.Content = String.Empty;
            this.AllowDrop = true;
            openFileDelegate = new DelegateOpenFile(this.OpenFile);
            // Create the events for the Background Worker.
            if (worker.IsBusy != true)
            {
                worker.WorkerReportsProgress = true;
                worker.WorkerSupportsCancellation = true;
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            }
        }

        //Form Load
        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtSearch.Focus();
            Percentage.Content = "0%";
            Results.Content = "Results: ";
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            // Set the Green accent and dark theme
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent("Green"),
                                        ThemeManager.GetAppTheme("BaseDark"));
        }

        //Declare and Initialize Variables, Arrays, DataTables and DataSets        
        DataSet ds = new DataSet();
        DataTable userTables = new DataTable();
        List<string> dbtables = new List<string>();
        List<string> sqlquery = new List<string>();
        List<string> found = new List<string>();
        string dbpass = "";
        string _dbpath;
        string searchExpression = "";

        //Background Worker Do Work
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            ds.Tables.Clear();
            dbtables.Clear();
            sqlquery.Clear();
            found.Clear();
            var conn = DatabaseConnection.GetConnection(dbpath, dbpass);
            OleDbDataAdapter oledbAdapter;
            string[] restrictions = new string[4];
            restrictions[3] = "Table";
            userTables = conn.GetSchema("Tables", restrictions);
            for (int i = 0; i < userTables.Rows.Count; i++)
            {
                //Fill dbtable with table count and names
                dbtables.Add(userTables.Rows[i][2].ToString());

                //Fill Sql Command Array
                OleDbCommand command = new OleDbCommand(conn.ConnectionString, conn);
                command.CommandText = "SELECT * FROM " + "[" + dbtables[i].ToString() + "]";
                sqlquery.Add(command.CommandText);
                oledbAdapter = new OleDbDataAdapter(sqlquery[i].ToString(), conn);
                oledbAdapter.Fill(ds, dbtables[i].ToString());
                oledbAdapter.Dispose();
                command.Dispose();
                DatabaseConnection.CloseConnection(conn);
                for (int r = 0; r < ds.Tables[i].Rows.Count; r++)
                {
                    for (int c = 0; c < ds.Tables[i].Columns.Count; c++)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(ds.Tables[i].Rows[r][c].ToString(), searchExpression.Trim(), System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            found.Add(ds.Tables[i].TableName.ToString());
                        }
                    }
                }
            }
            for (int i = 1; i <= found.Count; i++)
            {
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                    worker.ReportProgress(Convert.ToInt32(i * 100 / found.Count));
            }
        }

        //Background Worker Progress Changed
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            Percentage.Content = e.ProgressPercentage.ToString() + "%";
        }

        //Background Worker Completed
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                Results.Foreground = Brushes.Red;
                this.Results.Content = "Search Canceled By User!";
                Percentage.Content = "0%";
            }

            else if (!(e.Error == null))
            {
                Results.Foreground = Brushes.Red;
                this.Results.Content = ("Error: " + e.Error.Message);
            }
            else
            {
                //Fill ListBox With Found Items
                var g = found.GroupBy(i => i);
                foreach (var grp in g)
                {
                    ListBox.Items.Add(grp.Key + " " + grp.Count() + " Times");
                }
                searchExpression = txtSearch.Text.ToString();
                if (found.Count != 0)
                {
                    Results.Foreground = Brushes.White;
                    Results.Content = "Results: " + '\"' + searchExpression + '\"' + " Found " + found.Count + " Times!";
                }
                else
                {
                    Results.Foreground = Brushes.Red;
                    Results.Content = "No Records Found For: " + searchExpression;
                }
            }
        }

        //Save Settings
        private void SaveSettings()
        {

        }

        //Save Settings on Closing
        private void Window_Closing(
             object sender,
             System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        //Button Cancel
        private void btnCancel(object sender, RoutedEventArgs e)
        {
            if (worker.WorkerSupportsCancellation == true)
            {
                worker.CancelAsync();
            }
        }

        //Custom Exception Method
        CustomExceptions customx = new CustomExceptions();
        private void checkFileException()
        {
            if (!dbpath.Contains(".mdb"))
            {
                throw new CustomExceptions();
            }
        }

        //Raise Event when Database Path Changes using Interface INotifyPropertyChanged
        public string dbpath
        {
            get
            {
                return this._dbpath;
            }
            set
            {
                if (_dbpath != value)
                {
                    _dbpath = value;
                    RaisePropertyChanged("dbpath");
                    ListBox.Items.Clear();
                    txtSearch.Text = string.Empty;
                    Results.Content = "Results: ";
                    progressBar.Value = 0;
                    txtSearch.Focus();
                }
            }
        }

        // Declare the OnPropertyChanged Event 
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //OpenFile Method for DragDrop Events
        private void OpenFile(string sFile)
        {
            txtDataBase.Text = System.IO.Path.GetFileName(sFile);
            if (sFile == dbpath)
            {
                MessageBox.Show(dbpath + " Already Open");
            }
            else
            {
                dbpath = sFile.ToString();
                sqlquery.Clear();
                dbtables.Clear();
                imgPath.ToolTip = "Full Path: " + dbpath;
            }
        }

        //ListBox Drop Events
        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    openFileDelegate(files[0]);
                    txtSearch.Focus();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error in DragDrop function: " + ex.Message);
            }
        }

        //Button Explorer
        private void btnExplorer_Click(object sender, RoutedEventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "Access Databases (*.mdb)|*.mdb|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == true)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        txtDataBase.Text = openFileDialog1.SafeFileName;
                        string dbfilename = openFileDialog1.FileName;
                        if (dbfilename == dbpath)
                        {
                            MessageBox.Show(dbpath + " Already Open");
                        }
                        else
                        {
                            using (myStream)
                            {
                                dbpath = openFileDialog1.FileName;
                                imgPath.ToolTip = "Full Path: " + dbpath;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        //Button Clear
        private void btnClear(object sender, RoutedEventArgs e)
        {
            progressBar.Value = 0;
            Percentage.Content = "0%";
            Results.Content = "Results: ";
            Results.Foreground = Brushes.White;
            dbtables.Clear();
            ds.Tables.Clear();
            sqlquery.Clear();
            found.Clear();
            txtSearch.Text = string.Empty;
            ListBox.Items.Clear();
            txtSearch.Focus();
        }

        //Button Search
        private void btnSearch(object sender, RoutedEventArgs e)
        {
            progressBar.Value = 0;
            Percentage.Content = "0%";
            searchExpression = txtSearch.Text.ToString();
            Percentage.Content = "";
            Results.Content = "";
            ListBox.Items.Clear();
            try
            {
                checkFileException();
                DatabaseConnection.GetConnection(dbpath, dbpass);
                //Start Worker
                worker.RunWorkerAsync(found);
                txtSearch.Focus();
            }//Custom Exception FileException()
            catch (CustomExceptions customx)
            {
                MessageBox.Show(customx.FileException());
            }//Generic Exception
            catch (Exception ex)
            {
                MessageBox.Show("Can not open db connection ! " + ex);
            }
        }


        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtDataBase_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
