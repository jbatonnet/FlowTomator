using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public enum SqlServerType
    {
        SQLite,
        MySQL,
    }

    public enum SQLiteMode
    {
        Memory,
        File
    }

    [Node(nameof(SqlQuery), "Data", "Queries a database to get a dataset")]
    public class SqlQuery : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return serverType;

                foreach (Variable variable in GetDatabaseVariables())
                    yield return variable;

                yield return query;
            }
        }
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return result;
            }
        }

        private Variable<SqlServerType> serverType = new Variable<SqlServerType>("Server", SqlServerType.MySQL, "The type of sql server to connect to");
        private Variable<string> query = new Variable<string>("Query", "SELECT 1 + 1", "The query to evaluate");

        private Variable<string> host = new Variable<string>("Host", "127.0.0.1", "The host to connect to");
        private Variable<ushort> port = new Variable<ushort>("Port", 0, "The host to connect to");
        private Variable<string> user = new Variable<string>("User", null, "The user used to connect to the database");
        private Variable<string> password = new Variable<string>("Password", null, "The password used to connect to the database");
        private Variable<string> database = new Variable<string>("Database", null, "The default database to run the specified query");

        private Variable<SQLiteMode> sqliteMode = new Variable<SQLiteMode>("Mode", SQLiteMode.File, "The SQLite mode to use");
        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file containing the database");

        private Variable<DataSet> result = new Variable<DataSet>("Result");

        public override NodeResult Run()
        {
            using (DbConnection connection = BuildConnection())
            {
                if (connection == null)
                    return NodeResult.Fail;

                try
                {
                    connection.Open();

                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query.Value;

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            int columnCount = reader.FieldCount;
                            string[] columnNames = Enumerable.Range(0, columnCount)
                                                             .Select(i => reader.GetName(i))
                                                             .ToArray();

                            DataSet dataSet = new DataSet("Data", columnNames);

                            while (reader.Read())
                            {
                                object[] values = new object[columnCount];
                                reader.GetValues(values);
                                dataSet.Rows.Add(values);
                            }

                            result.Value = dataSet;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Unable to open database connexion: " + e.Message);
                    return NodeResult.Fail;
                }

                return NodeResult.Success;
            }
        }

        public IEnumerable<Variable> GetDatabaseVariables()
        {
            switch (serverType.Value)
            {
                case SqlServerType.SQLite:
                    yield return sqliteMode;
                    if (sqliteMode.Value == SQLiteMode.File)
                        yield return file;
                    break;

                case SqlServerType.MySQL:
                    port.Value = 3306;
                    yield return host;
                    yield return port;
                    yield return user;
                    yield return password;
                    yield return database;
                    break;
            }
        }
        public DbConnection BuildConnection()
        {
            DbConnection connection = null;

            switch (serverType.Value)
            {
                case SqlServerType.SQLite:
                    //connection = new System.Data.SQLite.SQLiteConnection(string.Format("Data Source={0};", sqliteMode.Value == SQLiteMode.Memory ? ":memory:" : file.Value.FullName));
                    break;

                case SqlServerType.MySQL:
                    connection = new MySql.Data.MySqlClient.MySqlConnection(string.Format("Server={0};Port={1};Database={4};Uid={2};Pwd={3};", host.Value, port.Value, user.Value, password.Value, database.Value));
                    break;
            }

            return connection;
        }
    }
}