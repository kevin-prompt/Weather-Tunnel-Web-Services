using System;
using System.Data.SqlClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Data;

namespace WSHelpers
{
    public class DbSqlServer
    {
        private SqlConnectionStringBuilder csBuilder = null;

        public DbSqlServer()
        {
            string region = RoleEnvironment.GetConfigurationSettingValue("Region");

            csBuilder = new SqlConnectionStringBuilder();
            csBuilder.DataSource = region.Equals("PROD") ? "weathertech.database.windows.net" : ".\\SQLEXPRESS";
            csBuilder.InitialCatalog = "weathertechdb";
            if (region.Equals("PROD"))
            {
                csBuilder.Encrypt = true;
                csBuilder.TrustServerCertificate = false;
                csBuilder.UserID = "weathertechsql@weathertech";
                csBuilder.Password = RoleEnvironment.GetConfigurationSettingValue("SQL_Table");
            }
            else
            {
                csBuilder.IntegratedSecurity = true;
            }
        }

        public void ExecuteNonQuery(string storedProcName) { ExecuteNonQuery(storedProcName, new SqlParameter[0]); }
        public void ExecuteNonQuery(string storedProcName, SqlParameter[] parameters)
        {
            if (csBuilder == null) throw new System.NullReferenceException("connection");
            if (storedProcName == null || storedProcName.Length == 0) throw new System.NullReferenceException("storedProcName");

            // Create a connection
            using (SqlConnection connection = new SqlConnection(csBuilder.ToString()))
            {
                // Create a command
                using (SqlCommand command = new SqlCommand(storedProcName, connection))
                {
                    connection.Open();
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (SqlParameter parameter in parameters) command.Parameters.Add(parameter);
                    // Execute a command
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public object ExecuteScalar(string storedProcName) { return ExecuteScalar(storedProcName, new SqlParameter[0]); }
        public object ExecuteScalar(string storedProcName, SqlParameter[] parameters)
        {
            if (csBuilder == null) throw new System.NullReferenceException("connection");
            if (storedProcName == null || storedProcName.Length == 0) throw new System.NullReferenceException("storedProcName");

            // Create a connection
            using (SqlConnection connection = new SqlConnection(csBuilder.ToString()))
            {
                // Create a command
                using (SqlCommand command = new SqlCommand(storedProcName, connection))
                {
                    connection.Open();
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (SqlParameter parameter in parameters) command.Parameters.Add(parameter);
                    // Execute a command
                    object rtn = command.ExecuteScalar();
                    connection.Close();
                    return rtn;
                }
            }
        }

        public DataRow ExecuteDataRow(string storedProcName) { return ExecuteDataRow(storedProcName, new SqlParameter[0]); }
        public DataRow ExecuteDataRow(string storedProcName, SqlParameter[] parameters)
        {
            if (csBuilder == null) throw new System.NullReferenceException("connection");
            if (storedProcName == null || storedProcName.Length == 0) throw new System.NullReferenceException("storedProcName");

            // Create a connection
            using (SqlConnection connection = new SqlConnection(csBuilder.ToString()))
            {
                // Create a command
                using (SqlCommand command = new SqlCommand(storedProcName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (SqlParameter parameter in parameters) command.Parameters.Add(parameter);
                    DataSet rtn = new DataSet();
                    DataRow row = null;
                    // Execute a command
                    using (SqlDataAdapter sqlDA = new SqlDataAdapter())
                    {
                        connection.Open();
                        sqlDA.SelectCommand = command;
                        sqlDA.Fill(rtn);
                        if (rtn.Tables[0].Rows.Count > 0) row = rtn.Tables[0].Rows[0];
                        connection.Close();
                        return row;
                    }
                }
            }
        }

        public DataSet ExecuteDataset(string storedProcName) { return ExecuteDataset(storedProcName, new SqlParameter[0]); }
        public DataSet ExecuteDataset(string storedProcName, SqlParameter[] parameters)
        {
            if (csBuilder == null) throw new System.NullReferenceException("connection");
            if (storedProcName == null || storedProcName.Length == 0) throw new System.NullReferenceException("storedProcName");

            // Create a connection
            using (SqlConnection connection = new SqlConnection(csBuilder.ToString()))
            {
                // Create a command
                using (SqlCommand command = new SqlCommand(storedProcName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (SqlParameter parameter in parameters) command.Parameters.Add(parameter);
                    DataSet rtn = new DataSet();
                    // Execute a command
                    using (SqlDataAdapter sqlDA = new SqlDataAdapter())
                    {
                        connection.Open();
                        sqlDA.SelectCommand = command;
                        sqlDA.Fill(rtn);
                        connection.Close();
                        return rtn;
                    }
                }
            }
        }
    }
}