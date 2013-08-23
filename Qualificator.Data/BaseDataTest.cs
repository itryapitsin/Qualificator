using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;

namespace Qualificator.Data
{
    public abstract class BaseDataTest<TContext, TConfig> 
        where TContext: DbContext, new() 
        where TConfig: DbMigrationsConfiguration<TContext>, new()
    {
        protected SqlConnectionStringBuilder ConnectionStringBuilder = new SqlConnectionStringBuilder();
        protected TContext DataContext;

        public virtual void TestInitialize()
        {
            KillOpenedDatabaseConnections(ConnectionStringBuilder.DataSource, ConnectionStringBuilder.InitialCatalog);

            DataContext = (TContext) Activator.CreateInstance(typeof(TContext), new [] { ConnectionStringBuilder.ToString() });
            Database.SetInitializer(new CreateDbWithMigrationHistoryIfNotExists<TContext, TConfig>());
        }

        protected void KillOpenedDatabaseConnections(string dataSource, string databaseName)
        {
            var connStrBuilder = new SqlConnectionStringBuilder { DataSource = dataSource, IntegratedSecurity = true };

            using (var connection = new SqlConnection(connStrBuilder.ConnectionString))
            {
                connection.Open();

                foreach (var processId in GetCurrentConnectedProcessesIDs(connection, databaseName))
                {
                    using (var killCommand = new SqlCommand("KILL " + processId, connection))
                    {
                        killCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private IEnumerable<int> GetCurrentConnectedProcessesIDs(SqlConnection connection, string databaseName)
        {
            var resultado = new List<int>();
            var sql = string.Format("SELECT spid as Process_ID FROM  MASTER..SysProcesses WHERE DBId = DB_ID('{0}') AND SPId <> @@SPId", databaseName);
            using (var dr = new SqlCommand(sql, connection).ExecuteReader())
            {
                while (dr.Read())
                {
                    var processID = int.Parse(dr["Process_ID"].ToString());
                    resultado.Add(processID);
                }
            }
            return resultado;
        }

        protected Byte[] GenerateByteArray(int length)
        {
            var result = new byte[length];
            var random = new Random();
            random.NextBytes(result);
            return result;
        }
    }
}
