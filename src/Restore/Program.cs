

using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Restore
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Missing arguments:");
                Console.WriteLine(
                    "restore \"server=.;database=test_db;user=sa;password=password\" \"c:\\temp\\script.sql\"");
                Environment.Exit(1);
            }
            Console.WriteLine("Connecting to database");
            var serverConnection = new ServerConnection(new SqlConnection(args[0]));
            var server = new Server(serverConnection);
            var database = server.Databases[server.ConnectionContext.DatabaseName];
            Console.WriteLine("Dropping foreign keys");
            var list = database.Tables.Cast<Table>().ToList();
            foreach (
                var foreignKey in
                list.Cast<Table>()
                    .SelectMany(table => table.ForeignKeys.Cast<ForeignKey>().ToList().Cast<ForeignKey>(),
                        (table, fk) => fk))
                foreignKey.Drop();
            Console.WriteLine("Dropping indexes");
            foreach (
                var index in
                list.Cast<Table>()
                    .SelectMany(table => table.Indexes.Cast<Index>().ToList().Cast<Index>(), (table, index) => index))
                index.Drop();
            Console.WriteLine("Dropping stored procedures");
            foreach (
                var storedProcedure in
                database.StoredProcedures.Cast<StoredProcedure>().Where(sp => !sp.Schema.Equals("sys")).ToList())
                storedProcedure.Drop();
            Console.WriteLine("Dropping views");
            foreach (var view in database.Views.Cast<View>().Where(v =>
            {
                if (!v.Schema.Equals("sys"))
                    return !v.Schema.Equals("INFORMATION_SCHEMA");
                return false;
            }).ToList())
                view.Drop();
            Console.WriteLine("Dropping tables");
            foreach (var table in database.Tables.Cast<Table>().ToList())
                table.Drop();
            Console.WriteLine("Restoring database from script file");
            var strArray = new StreamReader(args[1]).ReadToEnd().Split(new string[1]
            {
                "\r\nGO\r\n"
            }, StringSplitOptions.RemoveEmptyEntries);
            using (var connection = new SqlConnection(args[0]))
            {
                connection.Open();
                foreach (var cmdText in strArray)
                    new SqlCommand(cmdText, connection).ExecuteNonQuery();
                connection.Close();
            }
            serverConnection.Disconnect();
            Console.WriteLine("Finished");
        }
    }
}