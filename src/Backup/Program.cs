using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Backup
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Missing arguments:");
                Console.WriteLine(
                    "backup.exe \"server=.;database=test_db;user=sa;password=password\" \"c:\\temp\\script.sql\"");
                Environment.Exit(1);
            }
            Console.WriteLine("Connecting to database");
            var serverConnection = new ServerConnection(new SqlConnection(args[0]));
            var svr = new Server(serverConnection);
            var database = svr.Databases[svr.ConnectionContext.DatabaseName];
            Console.WriteLine("Setting script options");
            var scripter1 = new Scripter(svr)
            {
                Options =
                {
                    ScriptSchema = true,
                    ScriptData = true,
                    TargetServerVersion = SqlServerVersion.Version110,
                    Default = true,
                    Indexes = true,
                    ClusteredIndexes = true,
                    FullTextIndexes = true,
                    NonClusteredIndexes = true,
                    DriAll = true,
                    IncludeDatabaseContext = false,
                    NoFileGroup = true,
                    NoTablePartitioningSchemes = true,
                    NoIndexPartitioningSchemes = true
                }
            };
            var num = 1;
            scripter1.PrefetchObjects = num != 0;
            var scripter2 = scripter1;
            Console.WriteLine("Getting tables");
            var list = database.Tables.Cast<Table>().Where(tb => !tb.IsSystemObject).Select(tb => tb.Urn).ToList();
            Console.WriteLine("Getting views");
            list.AddRange(database.Views.Cast<View>().Where(view => !view.IsSystemObject).Select(view => view.Urn));
            Console.WriteLine("Getting stored procedures");
            list.AddRange(
                database.StoredProcedures.Cast<StoredProcedure>().Where(sp => !sp.IsSystemObject).Select(sp => sp.Urn));
            Console.WriteLine("Getting indexes");
            foreach (Table table in database.Tables)
                if (!table.Name.Equals("sysdiagrams", StringComparison.CurrentCultureIgnoreCase))
                    foreach (Index index in table.Indexes)
                        if (index.IndexedColumns.Count > 0)
                            list.Add(index.Urn);
                        else
                            Console.WriteLine("Failed to add index for Table {0}, Index {1}", table.Name, index.Name);
            Console.WriteLine("Getting foreign keys");
            foreach (Table table in database.Tables)
                if (!table.Name.Equals("sysdiagrams", StringComparison.CurrentCultureIgnoreCase))
                    foreach (ForeignKey foreignKey in table.ForeignKeys)
                        if (foreignKey.Columns.Count > 0)
                            list.Add(foreignKey.Urn);
                        else
                            Console.WriteLine("Failed to add Foreign Key for Table {0}, Foreign Key {1}", table.Name,
                                foreignKey.Name);
            Console.WriteLine("Building script");
            var stringBuilder = new StringBuilder();
            foreach (var str in scripter2.EnumScript(list.ToArray()))
            {
                stringBuilder.AppendLine(str);
                stringBuilder.AppendLine("GO");
            }
            Console.WriteLine("Writing script to disk");
            using (var binaryWriter = new BinaryWriter(new FileStream(args[1], FileMode.Create)))
            {
                binaryWriter.Write(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
                binaryWriter.Close();
            }
            serverConnection.Disconnect();
            Console.WriteLine("Finished");
        }
    }
}