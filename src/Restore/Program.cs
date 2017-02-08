using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restore
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Missing arguments:");
                Console.WriteLine("restorescript \"server=.;database=test_db;user=sa;password=password\" \"c:\\temp\\script.sql\"");
                Environment.Exit(1);
            }
            Console.WriteLine("Connecting to database");
            ServerConnection serverConnection = new ServerConnection(new SqlConnection(args[0]));
            Server server = new Server(serverConnection);
            Database database = server.Databases[server.ConnectionContext.DatabaseName];
            Console.WriteLine("Dropping foreign keys");
            List<Table> list = database.Tables.Cast<Table>().ToList<Table>();
            foreach (ForeignKey foreignKey in list.Cast<Table>().SelectMany<Table, ForeignKey, ForeignKey>((Func<Table, IEnumerable<ForeignKey>>)(table => table.ForeignKeys.Cast<ForeignKey>().ToList<ForeignKey>().Cast<ForeignKey>()), (Func<Table, ForeignKey, ForeignKey>)((table, fk) => fk)))
                foreignKey.Drop();
            Console.WriteLine("Dropping indexes");
            foreach (Index index in list.Cast<Table>().SelectMany<Table, Index, Index>((Func<Table, IEnumerable<Index>>)(table => table.Indexes.Cast<Index>().ToList<Index>().Cast<Index>()), (Func<Table, Index, Index>)((table, index) => index)))
                index.Drop();
            Console.WriteLine("Dropping stored procedures");
            foreach (StoredProcedure storedProcedure in database.StoredProcedures.Cast<StoredProcedure>().Where<StoredProcedure>((Func<StoredProcedure, bool>)(sp => !sp.Schema.Equals("sys"))).ToList<StoredProcedure>())
                storedProcedure.Drop();
            Console.WriteLine("Dropping views");
            foreach (View view in database.Views.Cast<View>().Where<View>((Func<View, bool>)(v =>
            {
                if (!v.Schema.Equals("sys"))
                    return !v.Schema.Equals("INFORMATION_SCHEMA");
                return false;
            })).ToList<View>())
                view.Drop();
            Console.WriteLine("Dropping tables");
            foreach (Table table in database.Tables.Cast<Table>().ToList<Table>())
                table.Drop();
            Console.WriteLine("Restoring database from script file");
            string[] strArray = new StreamReader(args[1]).ReadToEnd().Split(new string[1]
            {
        "\r\nGO\r\n"
            }, StringSplitOptions.RemoveEmptyEntries);
            using (SqlConnection connection = new SqlConnection(args[0]))
            {
                connection.Open();
                foreach (string cmdText in strArray)
                    new SqlCommand(cmdText, connection).ExecuteNonQuery();
                connection.Close();
            }
            serverConnection.Disconnect();
            Console.WriteLine("Finished");

        }
    }
}
