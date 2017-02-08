using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;

namespace Backup
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Missing arguments:");
                Console.WriteLine("generatescript \"server=.;database=test_db;user=sa;password=password\" \"c:\\temp\\script.sql\"");
                System.Environment.Exit(1);
            }
            Console.WriteLine("Connecting to database");
            ServerConnection serverConnection = new ServerConnection(new SqlConnection(args[0]));
            Server svr = new Server(serverConnection);
            Database database = svr.Databases[svr.ConnectionContext.DatabaseName];
            Console.WriteLine("Setting script options");
            Scripter scripter1 = new Scripter(svr);
            scripter1.Options.ScriptSchema = true;
            scripter1.Options.ScriptData = true;
            scripter1.Options.TargetServerVersion = SqlServerVersion.Version110;
            scripter1.Options.Default = true;
            scripter1.Options.Indexes = true;
            scripter1.Options.ClusteredIndexes = true;
            scripter1.Options.FullTextIndexes = true;
            scripter1.Options.NonClusteredIndexes = true;
            scripter1.Options.DriAll = true;
            scripter1.Options.IncludeDatabaseContext = false;
            scripter1.Options.NoFileGroup = true;
            scripter1.Options.NoTablePartitioningSchemes = true;
            scripter1.Options.NoIndexPartitioningSchemes = true;
            int num = 1;
            scripter1.PrefetchObjects = num != 0;
            Scripter scripter2 = scripter1;
            Console.WriteLine("Getting tables");
            List<Urn> list = database.Tables.Cast<Table>().Where<Table>((Func<Table, bool>)(tb => !tb.IsSystemObject)).Select<Table, Urn>((Func<Table, Urn>)(tb => tb.Urn)).ToList<Urn>();
            Console.WriteLine("Getting views");
            list.AddRange(database.Views.Cast<View>().Where<View>((Func<View, bool>)(view => !view.IsSystemObject)).Select<View, Urn>((Func<View, Urn>)(view => view.Urn)));
            Console.WriteLine("Getting stored procedures");
            list.AddRange(database.StoredProcedures.Cast<StoredProcedure>().Where<StoredProcedure>((Func<StoredProcedure, bool>)(sp => !sp.IsSystemObject)).Select<StoredProcedure, Urn>((Func<StoredProcedure, Urn>)(sp => sp.Urn)));
            Console.WriteLine("Getting indexes");
            foreach (Table table in (SmoCollectionBase)database.Tables)
            {
                if (!table.Name.Equals("sysdiagrams", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (Index index in (SmoCollectionBase)table.Indexes)
                    {
                        if (index.IndexedColumns.Count > 0)
                            list.Add(index.Urn);
                        else
                            Console.WriteLine(string.Format("Failed to add index for Table {0}, Index {1}", (object)table.Name, (object)index.Name));
                    }
                }
            }
            Console.WriteLine("Getting foreign keys");
            foreach (Table table in (SmoCollectionBase)database.Tables)
            {
                if (!table.Name.Equals("sysdiagrams", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (ForeignKey foreignKey in (SmoCollectionBase)table.ForeignKeys)
                    {
                        if (foreignKey.Columns.Count > 0)
                            list.Add(foreignKey.Urn);
                        else
                            Console.WriteLine(string.Format("Failed to add Foreign Key for Table {0}, Foreign Key {1}", (object)table.Name, (object)foreignKey.Name));
                    }
                }
            }
            Console.WriteLine("Building script");
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string str in scripter2.EnumScript(list.ToArray()))
            {
                stringBuilder.AppendLine(str);
                stringBuilder.AppendLine("GO");
            }
            Console.WriteLine("Writing script to disk");
            using (BinaryWriter binaryWriter = new BinaryWriter((Stream)new FileStream(args[1], FileMode.Create)))
            {
                binaryWriter.Write(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
                binaryWriter.Close();
            }
            serverConnection.Disconnect();
            Console.WriteLine("Finished");
        }

    }
    
}
