using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpecLog.GraphPlugin.Server.HtmlGraphGenerators;
using TechTalk.SpecLog.DataAccess;
using TechTalk.SpecLog.DataAccess.Boundaries;
using TechTalk.SpecLog.DataAccess.Repositories;

namespace SpecLog.GraphPlugin.Server
{
    public interface IGraphPluginRepositoryAccess
    {
        Guid GetRepositoryId();

        void EnsureDatabase();

        void StoreCommand(TechTalk.SpecLog.Commands.Command command);
        void RenameRepository(string newName);
    }

    public class GraphPluginRepositoryAccess : IGraphPluginRepositoryAccess, IGraphDataRepositoryAccess
    {
        private const string insertCommand = "INSERT INTO Commands (Id, CommandName, CreatedAt, CreatedBy, SynchronizedAt, SynchronizedBy)"
            + " VALUES (@Id, @CommandName, @CreatedAt, @CreatedBy, @SynchronizedAt, @SynchronizedBy)";

        private const string punchcardDataQuery = "SELECT strftime('%w', date(CreatedAt, '+30 minutes')), strftime('%H', time(CreatedAt, '+30 minutes')), count(*)"
            + " FROM Commands WHERE CreatedAt IS NOT NULL GROUP BY strftime('%w', date(CreatedAt, '+30 minutes')), strftime('%H', time(CreatedAt, '+30 minutes'))";

        private const string frequencySubquery = "SELECT date(CreatedAt) AS CreatedAt, count(*) AS CommandCount"
            + " FROM Commands WHERE CreatedAt >= @since GROUP BY date(CreatedAt), coalesce(CreatedBy, '')";
        private const string frequencyDataQuery = "SELECT CreatedAt, count(*), sum(CommandCount) FROM (" + frequencySubquery + ") GROUP BY CreatedAt";

        private static readonly CultureInfo locale = CultureInfo.InvariantCulture;
        private const DateTimeStyles dateFlags = DateTimeStyles.AllowInnerWhite | DateTimeStyles.AssumeLocal;

        private readonly string connectionString;
        private readonly IUpdateBarrier updateBarrier;
        private readonly ISqlClientBehaviour sqlClientBehaviour;
        public GraphPluginRepositoryAccess(IUpdateBarrier updateBarrier, ISqlClientBehaviour sqlClientBehaviour, IRepositoryStorage storage)
        {
            this.updateBarrier = updateBarrier;
            this.sqlClientBehaviour = sqlClientBehaviour;
            this.connectionString = storage.DataDomain.ConnectionString;
        }

        public void EnsureDatabase()
        {
            using (updateBarrier.BeginUpdate("EnsureDatabase"))
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();

                var tableNames = new List<string>();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand("SELECT name FROM sqlite_master WHERE type='table'", sqlConnection))
                {
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            tableNames.Add(sqlReader.GetValue(0) as string);
                        }
                    }
                }

                foreach (var table in tableNames.Except(new[] { "RepositoryInfo", "Commands" }))
                {
                    var commandText = string.Format("DROP TABLE IF EXISTS [{0}]", table);
                    using (var sqlCommand = sqlClientBehaviour.CreateCommand(commandText, sqlConnection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                }

                if (!tableNames.Contains("Commands"))
                {
                    var createText = "CREATE TABLE Commands (Id NONE, CommandName INTEGER, CreatedAt NONE, CreatedBy TEXT, SynchronizedAt NONE, SynchronizedBy TEXT)";
                    using (var sqlCommand = sqlClientBehaviour.CreateCommand(createText, sqlConnection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }

                    using (var sqlCommand = sqlClientBehaviour.CreateCommand("UPDATE RepositoryInfo set BaseLineVersion = 0", sqlConnection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }

                    using (var sqlCommand = sqlClientBehaviour.CreateCommand("VACUUM", sqlConnection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void StoreCommand(TechTalk.SpecLog.Commands.Command command)
        {
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand(insertCommand, sqlConnection))
                {
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@Id", command.CommandId));
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@CommandName", (int)command.CommandName));
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@CreatedAt", command.CreatedAt));
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@CreatedBy", command.CreatedBy));
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@SynchronizedAt", command.SynchronizedAt));
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@SynchronizedBy", command.SynchronizedBy));
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public void RenameRepository(string newName)
        {
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand("UPDATE RepositoryInfo SET Name = @name", sqlConnection))
                {
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@name", newName));

                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public DateTime? GetStartDate()
        {
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand("SELECT min(CreatedAt) FROM Commands WHERE CreatedAt IS NOT NULL", sqlConnection))
                {
                    var result = DateTime.MinValue;
                    var value = sqlCommand.ExecuteScalar() as string;
                    if (DateTime.TryParse(value, locale, dateFlags, out result))
                        return result;
                    return null;
                }
            }
        }

        public Guid GetRepositoryId()
        {
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand("SELECT Id FROM RepositoryInfo", sqlConnection))
                {
                    return new Guid(sqlCommand.ExecuteScalar() as byte[]);
                }
            }
        }

        public string GetRepositoryName()
        {
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand("SELECT Name FROM RepositoryInfo", sqlConnection))
                {
                    return sqlCommand.ExecuteScalar() as string;
                }
            }
        }

        public IEnumerable<PunchcardGraphData> GetPunchcardData()
        {
            var result = new List<PunchcardGraphData>();
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand(punchcardDataQuery, sqlConnection))
                {
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            result.Add(new PunchcardGraphData
                            {
                                Day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), (string)sqlReader.GetValue(0)),
                                Hour = int.Parse((string)sqlReader.GetValue(1)),
                                Count = (int)(long)sqlReader.GetValue(2)
                            });
                        }
                    }
                }
            }
            result.Sort((a, b) => (a.Day == b.Day) ? a.Hour.CompareTo(b.Hour) : a.Day.CompareTo(b.Day));
            return result;
        }

        public IEnumerable<FrequencyGraphData> GetFrequencyData(DateTime since)
        {
            var result = new List<FrequencyGraphData>();
            using (var sqlConnection = sqlClientBehaviour.CreateConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlClientBehaviour.CreateCommand(frequencyDataQuery, sqlConnection))
                {
                    sqlCommand.Parameters.Add(sqlClientBehaviour.CreateParameter("@since", since.Date));
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            result.Add(new FrequencyGraphData
                            {
                                Date = DateTime.Parse((string)sqlReader.GetValue(0), locale, dateFlags),
                                UserCount = (int)(long)sqlReader.GetValue(1),
                                CommandCount = (int)(long)sqlReader.GetValue(2)
                            });
                        }
                    }
                }
            }
            result.Sort((a, b) => a.Date.CompareTo(b.Date));
            return result;
        }
    }
}
