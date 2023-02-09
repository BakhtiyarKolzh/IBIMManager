using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;


namespace IBIMTool.Authorization
{
    public class DataBase
    {

        SqlConnection sqlConnection = new SqlConnection(@"Data Source=IASP-158\SQLEXPRESS; Initial Catalog=AuthorizationDB; User Id = Administrator; password = 123qweQWE");

        /// Method OpenConncetion()
        public void OpenConncetion()
        {
            if (sqlConnection.State == ConnectionState.Closed)
            {
                sqlConnection.Open();
            }
        }


        /// Method CloseConncetion()
        public void CloseConncetion()
        {
            if (sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }
        }


        /// Method GetConnection()
        public SqlConnection GetConnection()
        {
            return sqlConnection;
        }


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
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Error: " + ex.Message);
                        throw new Exception(queryExpression, ex);
                    }
                }
            }
            return table;
        }


        public static bool ExecuteNonQueryHandler(string queryExpression)
        {
            bool result = false;
            DataBase database = new DataBase();
            using (SqlConnection connection = database.GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    try
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand(queryExpression, connection);
                        result = 0 < command.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Error: " + ex.Message);
                        throw new Exception(queryExpression, ex);
                    }
                }
            }
            return result;
        }


        public static object ExecuteScalarHandler(string queryExpression)
        {
            object result = null;
            DataBase database = new DataBase();
            using (SqlConnection connection = database.GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    try
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand(queryExpression, connection);
                        result = command.ExecuteScalar();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Error: " + ex.Message);
                        throw new Exception(queryExpression, ex);
                    }
                }
            }
            return result;
        }


    }

}

