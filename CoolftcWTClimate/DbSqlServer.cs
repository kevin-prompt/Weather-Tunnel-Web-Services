using System;
using System.Data.SqlClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Data;

namespace WSHelpers
{
    /// <summary>
    /// This is the general utility class for accessing SQL. It creates a connection and then allows
    /// stored procedures to be called and their data returned.
    /// 
    /// NOTE: With Azure SQL, these methods should always be placed within a try/catch higher up in the code.  It is common for 
    /// the low level socket connection to be lost and for a cached connection to reply with an error - "The semaphore timeout period has expired".
    /// This just requires a 2 try so a the connection can be properly created on a new socket.
    /// </summary>
    public class DbSqlServer
    {
        private SqlConnectionStringBuilder csBuilder = null;

        public DbSqlServer()
        {
            string region = RoleEnvironment.GetConfigurationSettingValue("Region");

            csBuilder = new SqlConnectionStringBuilder();
            csBuilder.DataSource = region.Equals("PROD") ? RoleEnvironment.GetConfigurationSettingValue("SQL_Room") : ".\\SQLEXPRESS";
            csBuilder.InitialCatalog = "wtunneldb";
            if (region.Equals("PROD"))
            {
                csBuilder.Encrypt = true;
                csBuilder.TrustServerCertificate = false;
                csBuilder.UserID = RoleEnvironment.GetConfigurationSettingValue("SQL_Chair");
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
                    try { command.ExecuteNonQuery(); }
                    catch { command.Parameters.Clear(); connection.Close(); throw; }
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
                    object rtn;
                    try { rtn = command.ExecuteScalar(); }
                    catch { command.Parameters.Clear(); connection.Close(); throw; }
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
                        try { sqlDA.Fill(rtn); }
                        catch { command.Parameters.Clear(); connection.Close(); throw; }
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
                        try { sqlDA.Fill(rtn); }
                        catch { command.Parameters.Clear(); connection.Close(); throw; }
                        connection.Close();
                        return rtn;
                    }
                }
            }
        }
    }
}