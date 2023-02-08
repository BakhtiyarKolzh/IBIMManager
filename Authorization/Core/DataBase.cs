using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace Authorization
{
    public class DataBase
    {
        SqlConnection sqlConnection = new SqlConnection(@"Data Source=IASP-158\SQLEXPRESS; 
                                                            Initial Catalog=AuthorizationDB; User Id = Administrator; password = 123qweQWE");
        /// Method OpenConncetion()
        public void OpenConncetion()
        {
            if (sqlConnection.State == System.Data.ConnectionState.Closed)
            {
                sqlConnection.Open();
            }
        }
        /// Method CloseConncetion()
        public void CloseConncetion()
        {
            if (sqlConnection.State == System.Data.ConnectionState.Open)
            {
                sqlConnection.Close();
            }
        }

        /// Method GetConnection()
        public SqlConnection GetConnection()
        {
            return sqlConnection;
        }

        /// Method GetDataTable
        internal void GetDataTable(string queryExpression)
        {
            throw new NotImplementedException();
        }


        /// Method GetDataTable
        public static DataTable UpdateDataTable(string queryExpression)
        {
            DataTable table = null;
            DataBase database = new DataBase();
            using (SqlConnection connection = database.GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    try
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand(queryExpression, connection);
                        table = new DataTable();
                        adapter.SelectCommand = command;
                        adapter.Fill(table);
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Error: " + ex.Message);
                    }
                }
            }
            return table;
        }


        public static object GetActivationData(string queryExpression)
        {
            object resultTask = null;

            string connectionString = "";
            //string connectionString = Properties.Settings.Default.DataConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(queryExpression, connection))
            {
                SqlCommand command = new SqlCommand(queryExpression, connection);

                // Part 3: Use DataAdapter to fill DataTable.
                DataTable tableUsers = new DataTable();
                adapter.Fill(tableUsers);

                // Part 4: render data onto the screen.
                // dataGridView.DataSource = tableUsers;
            }

            return resultTask;
        }



    }



}

