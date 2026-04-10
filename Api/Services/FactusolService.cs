using System.Data.OleDb;
using FactuSync.Shared;

namespace FactuSync.Api.Services;

public interface IFactusolService
{
    Task<List<Cliente>> GetClientesAsync(string busqueda);
    Task<List<Proveedor>> GetProveedoresAsync();
    Task<List<Articulo>> GetArticulosAsync(string busqueda, int tarifa = 1);
    Task<List<Almacen>> GetAlmacenesAsync();
    Task<(bool Success, string Message)> CrearPedidoAsync(Pedido pedido);
    Task<Agente?> LoginAsync(string usuario, string clave);
    Task<bool> UpdateDbPathAsync(string newPath, string masterPassword);
    bool ValidateMasterPassword(string password);
    string GetDbPath();
    Task<List<Pedido>> GetPedidosAsync();
    Task<List<PedidoLinea>> GetPedidoLineasAsync(string tip, double cod);
    Task<(bool Success, string Message)> TestConnectionAsync();
    Task<List<string>> GetSeriesAsync();
    Task<double> GetSiguientePedidoAsync(string serie);
}

public class FactusolService : IFactusolService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public FactusolService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    private string GetConnectionString()
    {
        var dbPath = _config["FactusolConfig:DbPath"] ?? "Factusol.accdb";
        return $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";
    }

    public string GetDbPath() => _config["FactusolConfig:DbPath"] ?? "";

    public async Task<List<Cliente>> GetClientesAsync(string busqueda)
    {
        var clientes = new List<Cliente>();
        try 
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();

            string query = "SELECT CODCLI, NOFCLI, NOCCLI, NIFCLI, TELCLI, EMACLI, DOMCLI, REQCLI, TARCLI FROM F_CLI";
            if (!string.IsNullOrEmpty(busqueda))
            {
                query += " WHERE NOCCLI LIKE ? OR NOFCLI LIKE ?";
            }

            using var command = new OleDbCommand(query, connection);
            if (!string.IsNullOrEmpty(busqueda))
            {
                command.Parameters.AddWithValue("?", $"%{busqueda}%");
                command.Parameters.AddWithValue("?", $"%{busqueda}%");
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                clientes.Add(new Cliente
                {
                    CODCLI = reader["CODCLI"] != DBNull.Value ? Convert.ToDouble(reader["CODCLI"]) : null,
                    NOFCLI = reader["NOFCLI"].ToString() ?? "",
                    NOCCLI = reader["NOCCLI"].ToString() ?? "",
                    NIFCLI = reader["NIFCLI"].ToString() ?? "",
                    TELCLI = reader["TELCLI"].ToString() ?? "",
                    EMACLI = reader["EMACLI"].ToString() ?? "",
                    DOMCLI = reader["DOMCLI"].ToString() ?? "",
                    REQCLI = reader["REQCLI"] != DBNull.Value ? Convert.ToDouble(reader["REQCLI"]) : 0,
                    TARCLI = reader["TARCLI"] != DBNull.Value ? Convert.ToDouble(reader["TARCLI"]) : 1
                });
            }
        } 
        catch (Exception ex)
        {
            // Log error y devolver lista vacia o mock para ejemeplo
            Console.WriteLine($"Error obteniendo clientes: {ex.Message}");
            // Para el DEMO, devolver mock data si la BD falla
            return GetMockClientes(busqueda);
        }
        return clientes;
    }

    public async Task<List<Proveedor>> GetProveedoresAsync()
    {
        var provs = new List<Proveedor>();
        try 
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT CODPRO, NOFPRO, NOCPRO, NIFPRO, TELPRO FROM F_PRO";
            using var command = new OleDbCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                provs.Add(new Proveedor
                {
                    CODPRO = reader["CODPRO"] != DBNull.Value ? Convert.ToDouble(reader["CODPRO"]) : null,
                    NOFPRO = reader["NOFPRO"].ToString() ?? "",
                    NOCPRO = reader["NOCPRO"].ToString() ?? "",
                    NIFPRO = reader["NIFPRO"].ToString() ?? "",
                    TELPRO = reader["TELPRO"].ToString() ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo proveedores: {ex.Message}");
        }
        return provs;
    }

    public async Task<List<Articulo>> GetArticulosAsync(string busqueda, int tarifa = 1)
    {
        var articulos = new List<Articulo>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Simplificamos la consulta para máxima compatibilidad con OLEDB
            string query = $@"SELECT ART.*, LTA.PRELTA, 
                (SELECT SUM(ACTSTO) FROM F_STO WHERE ARTSTO = ART.CODART) AS STOCK_ACT
                FROM F_ART AS ART 
                LEFT JOIN F_LTA AS LTA ON (LTA.ARTLTA = ART.CODART AND LTA.TARLTA = {tarifa})";
            
            if (!string.IsNullOrEmpty(busqueda))
            {
                query += " WHERE (ART.DESART LIKE ? OR ART.CODART LIKE ?)";
            }

            using var command = new OleDbCommand(query, connection);
            if (!string.IsNullOrEmpty(busqueda))
            {
                command.Parameters.Add("?", OleDbType.VarWChar).Value = "%" + busqueda + "%";
                command.Parameters.Add("?", OleDbType.VarWChar).Value = "%" + busqueda + "%";
            }

            using var reader = await command.ExecuteReaderAsync();
            var schema = reader.GetSchemaTable();
            string imgColumnName = "";
            
            foreach (System.Data.DataRow row in schema.Rows) {
                string colName = row["ColumnName"].ToString() ?? "";
                if (colName.Contains("IMG", StringComparison.OrdinalIgnoreCase)) {
                    imgColumnName = colName;
                    break;
                }
            }

            while (await reader.ReadAsync())
            {
                string rImg = !string.IsNullOrEmpty(imgColumnName) ? reader[imgColumnName].ToString()?.Trim() ?? "" : "";

                // Lógica de precio: 1. Tarifa, 2. Costo
                decimal precio = 0;
                if (reader["PRELTA"] != DBNull.Value && Convert.ToDecimal(reader["PRELTA"]) > 0) 
                {
                    precio = Convert.ToDecimal(reader["PRELTA"]);
                }
                else if (reader["PCMART"] != DBNull.Value && Convert.ToDecimal(reader["PCMART"]) > 0)
                {
                    precio = Convert.ToDecimal(reader["PCMART"]);
                }
                else if (reader["PCOART"] != DBNull.Value && Convert.ToDecimal(reader["PCOART"]) > 0)
                {
                    precio = Convert.ToDecimal(reader["PCOART"]);
                }

                articulos.Add(new Articulo
                {
                    CODART = reader["CODART"].ToString()?.Trim() ?? "",
                    DESART = reader["DESART"].ToString()?.Trim() ?? "",
                    FAMART = reader["FAMART"].ToString()?.Trim() ?? "",
                    IMGART = rImg,
                    PrecioVenta = precio,
                    STOART = reader["STOCK_ACT"] != DBNull.Value ? Convert.ToDouble(reader["STOCK_ACT"]) : 0,
                    // Conservamos el índice original (1, 2, 3) para mapeo exacto
                    IvaIndex = reader["IVAART"] != DBNull.Value ? Convert.ToInt32(reader["IVAART"]) : 1
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error DB: {ex.Message}");
            // Fallback: Si falla la consulta compleja, intentamos una básica pero filtrada
            try {
                articulos.Clear();
                using var conn2 = new OleDbConnection(GetConnectionString());
                await conn2.OpenAsync();
                
                string qBasic = "SELECT * FROM F_ART";
                if (!string.IsNullOrEmpty(busqueda)) qBasic += " WHERE DESART LIKE ? OR CODART LIKE ?";
                
                using var cmdBasic = new OleDbCommand(qBasic, conn2);
                if (!string.IsNullOrEmpty(busqueda)) {
                    cmdBasic.Parameters.Add("?", OleDbType.VarWChar).Value = "%" + busqueda + "%";
                    cmdBasic.Parameters.Add("?", OleDbType.VarWChar).Value = "%" + busqueda + "%";
                }
                
                using var rd2 = await cmdBasic.ExecuteReaderAsync();
                while (await rd2.ReadAsync()) {
                    articulos.Add(new Articulo {
                        CODART = rd2["CODART"].ToString()?.Trim() ?? "",
                        DESART = rd2["DESART"].ToString()?.Trim() ?? "",
                        PrecioVenta = rd2["PCOART"] != DBNull.Value ? Convert.ToDecimal(rd2["PCOART"]) : 0
                    });
                }
            } catch { /* Ignorar errores en el fallback */ }
        }

        return articulos;
    }
    public async Task<List<Pedido>> GetPedidosAsync()
    {
        var pedidos = new List<Pedido>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Traemos los ultimos 50 pedidos para el historial
            string query = "SELECT TOP 50 TIPPCL, CODPCL, FECPCL, CNOPCL, TOTPCL FROM F_PCL ORDER BY FECPCL DESC, CODPCL DESC";
            
            using var command = new OleDbCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pedidos.Add(new Pedido
                {
                    TIPPCL = reader["TIPPCL"].ToString() ?? "1",
                    CODPCL = Convert.ToDouble(reader["CODPCL"]),
                    FECPCL = reader["FECPCL"] != DBNull.Value ? Convert.ToDateTime(reader["FECPCL"]) : DateTime.Now,
                    CNOPCL = reader["CNOPCL"].ToString() ?? "",
                    TOTPCL = reader["TOTPCL"] != DBNull.Value ? Convert.ToDecimal(reader["TOTPCL"]) : 0
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo pedidos: {ex.Message}");
        }
        return pedidos;
    }

    public async Task<List<PedidoLinea>> GetPedidoLineasAsync(string tip, double cod)
    {
        var lineas = new List<PedidoLinea>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            string query = "SELECT ARTLPC, DESLPC, CANLPC, PRELPC, TOTLPC FROM F_LPC WHERE TIPLPC = ? AND CODLPC = ? ORDER BY POSLPC ASC";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.AddWithValue("?", tip);
            command.Parameters.AddWithValue("?", cod);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lineas.Add(new PedidoLinea
                {
                    ARTLPC = reader["ARTLPC"].ToString() ?? "",
                    DESLPC = reader["DESLPC"].ToString() ?? "",
                    CANLPC = reader["CANLPC"] != DBNull.Value ? Convert.ToDecimal(reader["CANLPC"]) : 0,
                    PRELPC = reader["PRELPC"] != DBNull.Value ? Convert.ToDecimal(reader["PRELPC"]) : 0,
                    TOTLPC = reader["TOTLPC"] != DBNull.Value ? Convert.ToDecimal(reader["TOTLPC"]) : 0
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo lineas de pedido: {ex.Message}");
        }
        return lineas;
    }
    public async Task<List<string>> GetSeriesAsync()
    {
        var series = new List<string>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            // Usamos CStr para normalizar el tipo de dato de la Serie
            string query = "SELECT DISTINCT CStr(TIPPCL) FROM F_PCL WHERE TIPPCL IS NOT NULL ORDER BY CStr(TIPPCL) ASC";
            using var command = new OleDbCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var s = reader[0].ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(s)) series.Add(s);
            }
            if (series.Count == 0) series.Add("1");
        }
        catch { series.Add("1"); }
        return series;
    }

    public async Task<double> GetSiguientePedidoAsync(string serie)
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            // Forzamos comparación de texto con CStr
            string query = "SELECT MAX(CODPCL) FROM F_PCL WHERE CStr(TIPPCL) = ?";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.Add("?", OleDbType.VarWChar).Value = serie;
            var max = await command.ExecuteScalarAsync();
            return (max == DBNull.Value) ? 1 : Convert.ToDouble(max) + 1;
        }
        catch { return 1; }
    }

    public async Task<List<Almacen>> GetAlmacenesAsync()
    {
        using var connection = new OleDbConnection(GetConnectionString());
        await connection.OpenAsync();
        string query = "SELECT CODALM, NOMALM FROM F_ALM";
        using var cmd = new OleDbCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        var lista = new List<Almacen>();
        while (await reader.ReadAsync())
        {
            lista.Add(new Almacen { Codigo = reader["CODALM"].ToString() ?? "", Nombre = reader["NOMALM"].ToString() ?? "" });
        }
        return lista;
    }
    public async Task<(bool Success, string Message)> CrearPedidoAsync(Pedido pedido)
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Número consecutivo si no existe
                if (pedido.CODPCL == 0 || pedido.CODPCL == null)
                    pedido.CODPCL = await GetSiguientePedidoAsync(pedido.TIPPCL ?? "1");

                // 2. Agrupar Totales por IVA desde Configuración
                decimal net1 = 0, net2 = 0, net3 = 0;
                decimal iva1 = 0, iva2 = 0, iva3 = 0;
                decimal re1 = 0, re2 = 0, re3 = 0;
                decimal totalDoc = 0;

                foreach (var lin in pedido.Lineas)
                {
                    // Leer IVA/RE desde IvaConfig:{Index}
                    var config = _config.GetSection($"IvaConfig:{lin.IvaIndex}");
                    decimal pctIVA = config.GetValue<decimal>("IVA", lin.IvaIndex == 2 ? 10 : (lin.IvaIndex == 3 ? 4 : 21));
                    decimal pctRE = config.GetValue<decimal>("RE", lin.IvaIndex == 2 ? 1.4m : (lin.IvaIndex == 3 ? 0.5m : 5.2m));
                    
                    lin.IVALPC = (double)pctIVA;
                    decimal baseLin = (decimal)lin.TOTLPC;
                    decimal cuotaIVA = Math.Round(baseLin * (pctIVA / 100), 2);
                    decimal cuotaRE = (pedido.REQCLI == 1) ? Math.Round(baseLin * (pctRE / 100), 2) : 0;

                    if (lin.IvaIndex == 1) { net1 += baseLin; iva1 += cuotaIVA; re1 += cuotaRE; }
                    else if (lin.IvaIndex == 2) { net2 += baseLin; iva2 += cuotaIVA; re2 += cuotaRE; }
                    else if (lin.IvaIndex == 3) { net3 += baseLin; iva3 += cuotaIVA; re3 += cuotaRE; }

                    totalDoc += baseLin + cuotaIVA + cuotaRE;
                }

                // 3. Insertar Cabecera (F_PCL) - Eliminado TOTALPCL (campo inexistente)
                string sqlCab = @"INSERT INTO F_PCL (TIPPCL, CODPCL, FECPCL, CLIPCL, ALMPCL, TOTPCL, 
                                 NET1PCL, NET2PCL, NET3PCL, IIVA1PCL, IIVA2PCL, IIVA3PCL, 
                                 IREC1PCL, IREC2PCL, IREC3PCL, REQPCL) 
                                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                using var cmdCab = new OleDbCommand(sqlCab, connection, transaction);
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.TIPPCL ?? "1";
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = pedido.CODPCL;
                cmdCab.Parameters.Add("?", OleDbType.Date).Value = pedido.FECPCL ?? DateTime.Now;
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = pedido.CLIPCL ?? 0;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = !string.IsNullOrEmpty(pedido.ALMPCL) ? pedido.ALMPCL : "GENERAL";
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = totalDoc;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net1;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net3;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = iva1;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = iva2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = iva3;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = re1;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = re2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = re3;
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pedido.REQCLI;

                await cmdCab.ExecuteNonQueryAsync();

                // 4. Insertar Líneas (F_LCL)
                int pos = 1;
                foreach (var lin in pedido.Lineas)
                {
                    string sqlLin = @"INSERT INTO F_LCL (TIPLCL, CODLCL, POSLCL, ARTLCL, DESLCL, CANLCL, PRELCL, TOTLCL, IVALPC) 
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using var cmdLin = new OleDbCommand(sqlLin, connection, transaction);
                    cmdLin.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.TIPPCL ?? "1";
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = pedido.CODPCL;
                    cmdLin.Parameters.Add("?", OleDbType.Integer).Value = pos++;
                    cmdLin.Parameters.Add("?", OleDbType.VarWChar).Value = lin.ARTLPC ?? "";
                    cmdLin.Parameters.Add("?", OleDbType.VarWChar).Value = lin.DESLPC ?? "";
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = (double)(lin.CANLPC ?? 0);
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = (double)(lin.PRELPC ?? 0);
                    cmdLin.Parameters.Add("?", OleDbType.Currency).Value = lin.TOTLPC ?? 0;
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = lin.IVALPC ?? 0;

                    await cmdLin.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return (true, $"Pedido {pedido.TIPPCL}-{pedido.CODPCL} guardado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error grave: {ex.Message}");
            return (false, ex.Message);
        }
    }


    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Intentar una consulta simple para verificar permisos y tablas
            string query = "SELECT COUNT(*) FROM F_AGE";
            using var command = new OleDbCommand(query, connection);
            var count = await command.ExecuteScalarAsync();
            
            return (true, $"Conexión exitosa. Se encontraron {count} agentes.");
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
        }
    }
    
    public async Task<Agente?> LoginAsync(string usuario, string clave)
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();

            // Usamos TRIM para limpiar espacios que Access suele meter al final de los campos de texto
            string query = "SELECT CODAGE, NOMAGE, CUWAGE, CAWAGE, SUWAGE FROM F_AGE WHERE TRIM(CUWAGE) = ? AND TRIM(CAWAGE) = ?";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.AddWithValue("?", usuario.Trim());
            command.Parameters.AddWithValue("?", clave.Trim());

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var agente = new Agente
                {
                    CODAGE = reader["CODAGE"] != DBNull.Value ? Convert.ToDouble(reader["CODAGE"]) : null,
                    NOMAGE = reader["NOMAGE"].ToString()?.Trim() ?? "",
                    CUWAGE = reader["CUWAGE"].ToString()?.Trim() ?? "",
                    CAWAGE = reader["CAWAGE"].ToString()?.Trim() ?? "",
                    SUWAGE = reader["SUWAGE"] != DBNull.Value ? Convert.ToDouble(reader["SUWAGE"]) : 0
                };
                
                // Verificar si tiene el acceso web activado (SUWAGE = 1)
                if (agente.TieneAccesoWeb)
                {
                    return agente;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en login: {ex.Message}");
            // Fallback for mock demo
            if(usuario == "admin" && clave == "admin") 
            {
                return new Agente { CODAGE = 999, NOMAGE = "Agente Mock Demo", CUWAGE = "admin", CAWAGE = "admin", SUWAGE = 1 };
            }
        }
        return null;
    }

    public async Task<bool> UpdateDbPathAsync(string newPath, string masterPassword)
    {
        var configuredMaster = (_config["FactusolConfig:MasterPassword"] ?? "admin").Trim();
        if (masterPassword.Trim() != configuredMaster) return false;

        // Intentar actualizar tanto el archivo general como el de desarrollo si existe
        string[] filesToUpdate = { "appsettings.json", "appsettings.Development.json" };
        bool totalSuccess = false;

        foreach (var fileName in filesToUpdate)
        {
            string configPath = Path.Combine(_env.ContentRootPath, fileName);
            if (!File.Exists(configPath)) continue;

            try
            {
                var jsonText = await File.ReadAllTextAsync(configPath);
                var json = System.Text.Json.Nodes.JsonNode.Parse(jsonText);
                
                if (json?["FactusolConfig"] != null)
                {
                    json["FactusolConfig"]!["DbPath"] = newPath;
                    await File.WriteAllTextAsync(configPath, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    totalSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando {fileName}: {ex.Message}");
            }
        }

        return totalSuccess;
    }

    public bool ValidateMasterPassword(string password)
    {
        var configuredMaster = (_config["FactusolConfig:MasterPassword"] ?? "admin").Trim();
        var inputPassword = (password ?? "").Trim();
        return inputPassword == configuredMaster;
    }

    private List<Cliente> GetMockClientes(string busqueda)
    {
        var list = new List<Cliente>
        {
            new Cliente { CODCLI = 1, NOCCLI = "Empresa Ejemplo S.A.", NIFCLI = "A12345678", TELCLI = "912345678", EMACLI = "contacto@ejemplo.com", DOMCLI = "Calle Principal 1" },
            new Cliente { CODCLI = 2, NOFCLI = "Juan Perez Limitada", NOCCLI = "Juan Perez", NIFCLI = "12345678Z", TELCLI = "600123456", EMACLI = "juan@mail.com", DOMCLI = "Avenida 2" }
        };
        if(!string.IsNullOrEmpty(busqueda))
            return list.Where(c => c.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();
        return list;
    }

    private List<Articulo> GetMockArticulos()
    {
        return new List<Articulo>
        {
            new Articulo { CODART = "A01", DESART = "Laptop Lenovo ThinkPad", PrecioVenta = 850.00m, FAMART = "INF", CANART = 10 },
            new Articulo { CODART = "A02", DESART = "Monitor Dell 24", PrecioVenta = 150.00m, FAMART = "INF", CANART = 20 },
            new Articulo { CODART = "A03", DESART = "Teclado Mecánico", PrecioVenta = 80.00m, FAMART = "INF", CANART = 50 },
        };
    }
}
