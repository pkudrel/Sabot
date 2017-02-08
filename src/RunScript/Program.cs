using System;
using System.Data.SqlClient;
using System.IO;

namespace RunScript
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Missing arguments:");
                Console.WriteLine(
                    "runscript \"server=.;database=test_db;user=sa;password=password\" \"c:\\temp\\script.sql\"");
                Environment.Exit(1);
            }
            var strArray = new StreamReader(args[1]).ReadToEnd().Split(new string[1]
            {
                "\r\nGO\r\n"
            }, StringSplitOptions.RemoveEmptyEntries);
            using (var connection = new SqlConnection(args[0]))
            {
                Console.WriteLine("Connecting to database");
                connection.Open();
                foreach (var cmdText in strArray)
                    new SqlCommand(cmdText, connection).ExecuteNonQuery();
                connection.Close();
            }
            Console.WriteLine("Finished");
        }
    }
}