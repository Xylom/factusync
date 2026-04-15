using System;
using System.Data.OleDb;

class Program
{
    static void Main()
    {
        string dbPath = @"C:\Software DELSOL\FACTUSOL\Datos\FS\XD12026.accdb"; 
        string connStr = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
        
        try
        {
            using var conn = new OleDbConnection(connStr);
            conn.Open();
            
            Console.WriteLine("--- CLIENTE 1 ---");
            using (var cmd = new OleDbCommand("SELECT TARCLI FROM F_CLI WHERE CODCLI = 1", conn))
            {
                var tar = cmd.ExecuteScalar();
                Console.WriteLine($"Tarifa del cliente 1: {tar}");
            }

            Console.WriteLine("\n--- ARTICULO 4574 ---");
            using (var cmd = new OleDbCommand("SELECT DESART, PCOART, PCMART FROM F_ART WHERE CODART = '4574'", conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    Console.WriteLine($"Descripcion: {reader[0]}");
                    Console.WriteLine($"PCO (Costo): {reader[1]}");
                    Console.WriteLine($"PCM (Costo Medio): {reader[2]}");
                }
            }

            Console.WriteLine("\n--- PRECIOS TARIFA (F_LTA) ---");
            using (var cmd = new OleDbCommand("SELECT TARLTA, PRELTA FROM F_LTA WHERE ARTLTA = '4574'", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Tarifa {reader[0]}: {reader[1]}");
                }
            }
            
            Console.WriteLine("\n--- CONFIG TARIFA (F_TAR) ---");
            using (var cmd = new OleDbCommand("SELECT CODTAR, IVATAR FROM F_TAR", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Tarifa {reader[0]} - IVATAR: {reader[1]}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
