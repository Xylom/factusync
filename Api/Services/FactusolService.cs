using System.Data.OleDb;
using System.Text;
using FactuSync.Shared;

namespace FactuSync.Api.Services;

public interface IFactusolService
{
    Task<List<Cliente>> GetClientesAsync(string busqueda, string? ruta = null);
    Task<List<Proveedor>> GetProveedoresAsync();
    Task<List<Articulo>> GetArticulosAsync(string busqueda, int tarifa = 1, string? familia = null);
    Task<List<Familia>> GetFamiliasAsync();
    Task<List<Ruta>> GetRutasAsync();
    Task<List<Almacen>> GetAlmacenesAsync();
    Task<(bool Success, string Message)> CrearPedidoAsync(Pedido pedido);
    Task<Agente?> LoginAsync(string usuario, string clave);
    Task<bool> UpdateDbPathAsync(string newPath, string masterPassword);
    bool ValidateMasterPassword(string password);
    string GetDbPath();
    Task<List<Pedido>> GetPedidosAsync(DateTime? desde = null, DateTime? hasta = null, string? serie = null, double? agentId = null);
    Task<List<PedidoLinea>> GetPedidoLineasAsync(string tip, double cod);
    Task<List<Factura>> GetFacturasAsync(DateTime? desde = null, DateTime? hasta = null, string? serie = null);
    Task<(bool Success, string Message)> TestConnectionAsync();
    Task<List<string>> GetSeriesAsync();
    Task<List<string>> TestSchemaAsync();
    Task<List<FacturaLinea>> GetFacturaLineasAsync(string serie, double numero);
    Task<List<Cobro>> GetCobrosFacturaAsync(string serie, double numero);
    Task<double> GetSiguientePedidoAsync(string serie);
    GlobalConfig GetGlobalConfig();
    Task<bool> UpdateGlobalConfigAsync(GlobalConfig newConfig);
    Task<(bool Success, string Message)> EliminarPedidoAsync(string serie, double numero);
    Task<List<Agente>> GetAgentesAsync();
    Task<Pedido?> GetPedidoAsync(string serie, double numero);
    Task RestartTunnelAsync();
    string GetConsoleLog();
    event Action<string>? OnConsoleOutput;
}

public class FactusolService : IFactusolService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private static string _currentZrokUrl = "";
    private static string _lastTunnelStatus = "Inactivo";
    private static string _lastTunnelError = "";
    private static StringBuilder _consoleLog = new StringBuilder();
    public event Action<string>? OnConsoleOutput;

    public string GetConsoleLog() => _consoleLog.ToString();

    private void AddToLog(string line) 
    {
        _consoleLog.AppendLine(line);
        OnConsoleOutput?.Invoke(line);
    }

    public static void SetZrokUrl(string url) { _currentZrokUrl = url; _lastTunnelStatus = "Conectado"; _lastTunnelError = ""; }

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

    public async Task<Pedido?> GetPedidoAsync(string serie, double numero)
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Simplificamos la consulta para evitar columnas inexistentes en algunas versiones
            string query = "SELECT TIPPCL, CODPCL, FECPCL, CLIPCL, CNOPCL, TOTPCL, ALMPCL, ESTPCL FROM F_PCL WHERE TIPPCL = ? AND CODPCL = ?";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.Add("?", OleDbType.VarWChar).Value = serie;
            command.Parameters.Add("?", OleDbType.Double).Value = numero;
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var p = new Pedido
                {
                    TIPPCL = reader["TIPPCL"].ToString() ?? "",
                    CODPCL = reader["CODPCL"] != DBNull.Value ? Convert.ToDouble(reader["CODPCL"]) : 0,
                    Fecha = reader["FECPCL"] != DBNull.Value ? Convert.ToDateTime(reader["FECPCL"]) : DateTime.Now,
                    CLIPCL = reader["CLIPCL"] != DBNull.Value ? Convert.ToDouble(reader["CLIPCL"]) : 0,
                    CNOPCL = reader["CNOPCL"].ToString() ?? "",
                    ALMPCL = reader["ALMPCL"].ToString() ?? "",
                    ESTPCL = reader["ESTPCL"] != DBNull.Value ? Convert.ToInt32(reader["ESTPCL"]) : 0,
                    TOTPCL = reader["TOTPCL"] != DBNull.Value ? Convert.ToDecimal(reader["TOTPCL"]) : 0
                };
                p.Lineas = await GetPedidoLineasAsync(serie, numero);
                return p;
            }
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"[DB ERROR] Error en GetPedidoAsync ({serie}-{numero}): {ex.Message}"); 
        }
        return null;
    }

    public async Task<List<Agente>> GetAgentesAsync()
    {
        var agentes = new List<Agente>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT * FROM F_AGE ORDER BY NOMAGE";
            using var command = new OleDbCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            // Obtener índices de columnas para ser seguros ante cambios de esquema
            var columns = new HashSet<string>(Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i).ToUpper()));

            while (await reader.ReadAsync())
            {
                var agente = new Agente
                {
                    CODAGE = reader["CODAGE"] != DBNull.Value ? Convert.ToDouble(reader["CODAGE"]) : null,
                    NOMAGE = reader["NOMAGE"].ToString()?.Trim() ?? "",
                    CUWAGE = columns.Contains("CUWAGE") ? reader["CUWAGE"].ToString()?.Trim() ?? "" : ""
                };

                // Lectura segura de SUWAGE (Acceso Internet)
                if (columns.Contains("SUWAGE") && reader["SUWAGE"] != DBNull.Value)
                    agente.SUWAGE = Convert.ToDouble(reader["SUWAGE"]);
                
                // Lectura segura de JEQAGE (Es Jefe)
                if (columns.Contains("JEQAGE") && reader["JEQAGE"] != DBNull.Value)
                    agente.JEQAGE = Convert.ToDouble(reader["JEQAGE"]);

                agentes.Add(agente);
            }
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"[DB ERROR] Error en GetAgentesAsync: {ex.Message}"); 
            Console.WriteLine($"[DB STACK] {ex.StackTrace}");
        }
        return agentes;
    }

    public async Task<List<Cliente>> GetClientesAsync(string busqueda, string? ruta = null)
    {
        var clientes = new List<Cliente>();
        try 
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();

            string query = @"SELECT F_CLI.CODCLI, F_CLI.NOFCLI, F_CLI.NOCCLI, F_CLI.NIFCLI, 
                                    F_CLI.TELCLI, F_CLI.EMACLI, F_CLI.DOMCLI, F_CLI.REQCLI, 
                                    F_CLI.TARCLI, F_CLI.FPACLI, F_CLI.DT1CLI, F_CLI.RUTCLI, 
                                    F_CLI.PROCLI, F_RUT.DESRUT 
                             FROM F_CLI 
                             LEFT JOIN F_RUT ON F_CLI.RUTCLI = F_RUT.CODRUT 
                             WHERE 1=1";
            
            bool esNumero = double.TryParse(busqueda, out double codigoNum);

            if (!string.IsNullOrEmpty(busqueda))
            {
                if (esNumero)
                    query += " AND (F_CLI.CODCLI = ? OR F_CLI.NOCCLI LIKE ? OR F_CLI.NOFCLI LIKE ?)";
                else
                    query += " AND (F_CLI.NOCCLI LIKE ? OR F_CLI.NOFCLI LIKE ?)";
            }

            if (!string.IsNullOrEmpty(ruta))
            {
                query += " AND F_CLI.RUTCLI = ?";
            }

            using var command = new OleDbCommand(query, connection);
            if (!string.IsNullOrEmpty(busqueda))
            {
                if (esNumero)
                {
                    command.Parameters.Add("?", OleDbType.Double).Value = codigoNum;
                    command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
                    command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
                }
                else
                {
                    command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
                    command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
                }
            }

            if (!string.IsNullOrEmpty(ruta))
            {
                command.Parameters.Add("?", OleDbType.VarWChar).Value = ruta;
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
                    TARCLI = reader["TARCLI"] != DBNull.Value ? Convert.ToDouble(reader["TARCLI"]) : 1,
                    FPACLI = reader["FPACLI"]?.ToString()?.Trim() ?? "",
                    DT1CLI = reader["DT1CLI"] != DBNull.Value ? Convert.ToDecimal(reader["DT1CLI"]) : 0,
                    RUTCLI = reader["RUTCLI"]?.ToString()?.Trim() ?? "",
                    PROCLI = reader["PROCLI"]?.ToString()?.Trim() ?? "",
                    NombreRuta = reader["DESRUT"] != DBNull.Value ? reader["DESRUT"].ToString() : ""
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

    public async Task<List<Ruta>> GetRutasAsync()
    {
        var rutas = new List<Ruta>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT CODRUT, DESRUT, AGERUT FROM F_RUT ORDER BY DESRUT";
            using var command = new OleDbCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rutas.Add(new Ruta
                {
                    CODRUT = reader["CODRUT"].ToString()?.Trim() ?? "",
                    DESRUT = reader["DESRUT"].ToString()?.Trim() ?? "",
                    AGERUT = reader["AGERUT"] != DBNull.Value ? Convert.ToDouble(reader["AGERUT"]) : null
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching rutas: {ex.Message}");
        }
        return rutas;
    }

    public async Task<List<Familia>> GetFamiliasAsync()
    {
        var familias = new List<Familia>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT CODFAM, DESFAM FROM F_FAM ORDER BY DESFAM";
            using var command = new OleDbCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                familias.Add(new Familia
                {
                    CODFAM = reader["CODFAM"].ToString()?.Trim() ?? "",
                    DESFAM = reader["DESFAM"].ToString()?.Trim() ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching familias: {ex.Message}");
        }
        return familias;
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

    public async Task<List<Articulo>> GetArticulosAsync(string busqueda, int tarifa = 1, string? familia = null)
    {
        var articulos = new List<Articulo>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // 1. Consultar artículos filtrados de forma ultrarrápida (sin subqueries)
            string query = @"SELECT CODART, DESART, FAMART, IMGART, PCOART, DT0ART, TIVART FROM F_ART";

            bool hasBusqueda = !string.IsNullOrEmpty(busqueda);
            bool hasFamilia = !string.IsNullOrEmpty(familia);

            if (hasBusqueda)
            {
                query += " WHERE (DESART LIKE ? OR CODART LIKE ?)";
                if (hasFamilia) query += " AND FAMART = ?";
            }
            else if (hasFamilia)
            {
                query += " WHERE FAMART = ?";
            }

            using var command = new OleDbCommand(query, connection);
            if (hasBusqueda)
            {
                command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
                command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
            }
            if (hasFamilia)
            {
                command.Parameters.Add("?", OleDbType.VarWChar).Value = familia;
            }

            // Consultar si la tarifa incluye IVA (campo IVATAR en F_TAR)
            bool conIva = false;
            try
            {
                using var cmdTar = new OleDbCommand("SELECT IVATAR FROM F_TAR WHERE CODTAR = ?", connection);
                cmdTar.Parameters.Add("?", OleDbType.Double).Value = (double)tarifa;
                var ivaTar = await cmdTar.ExecuteScalarAsync();
                if (ivaTar != null && ivaTar != DBNull.Value)
                    conIva = (Convert.ToInt32(ivaTar) != 0);
            }
            catch { /* Ignorar si no se puede leer la tarifa */ }

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    articulos.Add(new Articulo
                    {
                        CODART = reader["CODART"].ToString()?.Trim() ?? "",
                        DESART = reader["DESART"].ToString()?.Trim() ?? "",
                        FAMART = reader["FAMART"].ToString()?.Trim() ?? "",
                        IMGART = reader["IMGART"] != DBNull.Value ? reader["IMGART"].ToString()?.Trim() ?? "" : "",
                        PCOART = reader["PCOART"] != DBNull.Value ? Convert.ToDecimal(reader["PCOART"]) : 0,
                        DT0ART = reader["DT0ART"] != DBNull.Value ? Convert.ToDecimal(reader["DT0ART"]) : 0,
                        IvaIndex = reader["TIVART"] != DBNull.Value ? Convert.ToInt32(reader["TIVART"]) : 0,
                        PrecioConIva = conIva
                    });
                }
            }

            if (articulos.Count == 0) return articulos;

            // Optimización: Cargar stock y precios en diccionarios en memoria 
            // (evita miles de subqueries individuales = rendimiento extremo)
            var stockDict = new Dictionary<string, double>();
            try 
            {
                string stockQuery = "SELECT ARTSTO, SUM(ACTSTO) AS STOCK_ACT FROM F_STO GROUP BY ARTSTO";
                using var cmdStock = new OleDbCommand(stockQuery, connection);
                using var rdStock = await cmdStock.ExecuteReaderAsync();
                while (await rdStock.ReadAsync())
                {
                    string cod = rdStock["ARTSTO"].ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(cod))
                        stockDict[cod] = rdStock["STOCK_ACT"] != DBNull.Value ? Convert.ToDouble(rdStock["STOCK_ACT"]) : 0;
                }
            } 
            catch { }

            var preciosDict = new Dictionary<string, decimal>();
            try 
            {
                string precioQuery = "SELECT ARTLTA, PRELTA FROM F_LTA WHERE TARLTA = ?";
                using var cmdPrecio = new OleDbCommand(precioQuery, connection);
                cmdPrecio.Parameters.Add("?", OleDbType.Double).Value = (double)tarifa;
                using var rdPrecio = await cmdPrecio.ExecuteReaderAsync();
                while (await rdPrecio.ReadAsync())
                {
                    string cod = rdPrecio["ARTLTA"].ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(cod) && !preciosDict.ContainsKey(cod))
                    {
                        preciosDict[cod] = rdPrecio["PRELTA"] != DBNull.Value ? Convert.ToDecimal(rdPrecio["PRELTA"]) : 0;
                    }
                }
            }
            catch { }

            // Mapeo final en memoria, manteniendo la misma lógica exacta original de precios
            foreach (var art in articulos)
            {
                art.STOART = stockDict.TryGetValue(art.CODART, out double stock) ? stock : 0;
                
                bool tarifaExists = preciosDict.TryGetValue(art.CODART, out decimal pTar);
                decimal precioFinal = tarifaExists ? pTar : 0;
                
                art.PrecioVenta = precioFinal;
                art.PRELTA_TAR_RAW = pTar;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo artículos (optimizado): {ex.Message}");
        }
        return articulos;
    }

    public async Task<List<Pedido>> GetPedidosAsync(DateTime? desde = null, DateTime? hasta = null, string? serie = null, double? agentId = null)
    {
        var pedidos = new List<Pedido>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            var conditions = new List<string>();
            var parameters = new List<(string, object)>();

            string query = "SELECT TIPPCL, CODPCL, FECPCL, CLIPCL, CNOPCL, TOTPCL, ESTPCL FROM F_PCL";

            if (desde.HasValue) {
                conditions.Add("FECPCL >= ?");
                parameters.Add(("?", desde.Value.Date));
            }
            if (hasta.HasValue) {
                conditions.Add("FECPCL <= ?");
                parameters.Add(("?", hasta.Value.Date));
            }
            if (!string.IsNullOrEmpty(serie)) {
                conditions.Add("TIPPCL = ?");
                parameters.Add(("?", serie));
            }
            if (agentId.HasValue) {
                conditions.Add("AGEPCL = ?");
                parameters.Add(("?", agentId.Value));
            }

            if (conditions.Any()) {
                query += " WHERE " + string.Join(" AND ", conditions);
            }
            
            query += " ORDER BY FECPCL DESC, CODPCL DESC";

            using var command = new OleDbCommand(query, connection);
            foreach (var p in parameters) {
                var param = command.Parameters.AddWithValue(p.Item1, p.Item2);
                if (p.Item2 is DateTime) param.OleDbType = OleDbType.DBDate;
            }
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pedidos.Add(new Pedido
                {
                    TIPPCL = reader["TIPPCL"].ToString() ?? "",
                    CODPCL = reader["CODPCL"] != DBNull.Value ? Convert.ToDouble(reader["CODPCL"]) : 0,
                    Fecha = reader["FECPCL"] != DBNull.Value ? Convert.ToDateTime(reader["FECPCL"]) : DateTime.Now,
                    CNOPCL = reader["CNOPCL"].ToString() ?? "",
                    TOTPCL = reader["TOTPCL"] != DBNull.Value ? Convert.ToDecimal(reader["TOTPCL"]) : 0,
                    ESTPCL = reader["ESTPCL"] != DBNull.Value ? Convert.ToInt32(reader["ESTPCL"]) : 0,
                    CLIPCL = reader["CLIPCL"] != DBNull.Value ? Convert.ToDouble(reader["CLIPCL"]) : null
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo pedidos: {ex.Message}");
        }
        return pedidos;
    }
    public async Task<List<Factura>> GetFacturasAsync(DateTime? desde = null, DateTime? hasta = null, string? serie = null)
    {
        var facturas = new List<Factura>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            var conditions = new List<string>();
            var parameters = new List<(string, object)>();

            string query = "SELECT TIPFAC, CODFAC, REFFAC, FECFAC, HORFAC, ESTFAC, CLIFAC, CNIFAC, TELFAC, CNOFAC, CDOFAC, TOTFAC, EMAFAC FROM F_FAC";

            if (desde.HasValue) {
                conditions.Add("FECFAC >= ?");
                parameters.Add(("?", desde.Value.Date));
            }
            if (hasta.HasValue) {
                conditions.Add("FECFAC <= ?");
                parameters.Add(("?", hasta.Value.Date));
            }
            if (!string.IsNullOrEmpty(serie)) {
                conditions.Add("TIPFAC = ?");
                parameters.Add(("?", serie));
            }

            if (conditions.Any()) {
                query += " WHERE " + string.Join(" AND ", conditions);
            }
            
            query += " ORDER BY FECFAC DESC, CODFAC DESC";

            using var command = new OleDbCommand(query, connection);
            foreach (var p in parameters) {
                var param = command.Parameters.AddWithValue(p.Item1, p.Item2);
                if (p.Item2 is DateTime) param.OleDbType = OleDbType.DBDate;
            }
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    TIPFAC = reader["TIPFAC"].ToString() ?? "",
                    CODFAC = reader["CODFAC"] != DBNull.Value ? Convert.ToDouble(reader["CODFAC"]) : 0,
                    REFFAC = reader["REFFAC"].ToString() ?? "",
                    FECFAC = reader["FECFAC"] != DBNull.Value ? Convert.ToDateTime(reader["FECFAC"]) : DateTime.Now,
                    HORFAC = reader["HORFAC"] != DBNull.Value ? Convert.ToDateTime(reader["HORFAC"]) : DateTime.Now,
                    ESTFAC = reader["ESTFAC"] != DBNull.Value ? Convert.ToDouble(reader["ESTFAC"]) : 0,
                    CLIFAC = reader["CLIFAC"] != DBNull.Value ? Convert.ToDouble(reader["CLIFAC"]) : 0,
                    CNIFAC = reader["CNIFAC"].ToString() ?? "",
                    TELFAC = reader["TELFAC"].ToString() ?? "",
                    CNOFAC = reader["CNOFAC"].ToString() ?? "",
                    CDOFAC = reader["CDOFAC"].ToString() ?? "",
                    TOTFAC = reader["TOTFAC"] != DBNull.Value ? Convert.ToDecimal(reader["TOTFAC"]) : 0,
                    EMAFAC = reader["EMAFAC"] != DBNull.Value ? Convert.ToDouble(reader["EMAFAC"]) : 0
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo facturas: {ex.Message}");
        }
        return facturas;
    }


    public async Task<List<string>> TestSchemaAsync()
    {
        var cols = new List<string>();
        try {
            using var conn = new OleDbConnection(GetConnectionString());
            await conn.OpenAsync();
            using var cmd = new OleDbCommand("SELECT TOP 1 * FROM F_COB", conn);
            using var r = await cmd.ExecuteReaderAsync();
            for(int i = 0; i < r.FieldCount; i++) cols.Add(r.GetName(i));
        } catch(Exception e) { cols.Add(e.Message); }
        return cols;
    }

    public async Task<List<FacturaLinea>> GetFacturaLineasAsync(string serie, double numero)
    {
        var lineas = new List<FacturaLinea>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            string query = "SELECT ARTLFA, DESLFA, CANLFA, PRELFA, DT1LFA, IVALFA, TOTLFA FROM F_LFA WHERE TIPLFA = ? AND CODLFA = ? ORDER BY POSLFA ASC";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.Add("?", OleDbType.VarWChar).Value = serie;
            command.Parameters.Add("?", OleDbType.Double).Value = numero;
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lineas.Add(new FacturaLinea
                {
                    CodigoArticulo = reader["ARTLFA"].ToString() ?? "",
                    DescripcionArticulo = reader["DESLFA"].ToString() ?? "",
                    Cantidad = reader["CANLFA"] != DBNull.Value ? Convert.ToDecimal(reader["CANLFA"]) : 0,
                    Precio = reader["PRELFA"] != DBNull.Value ? Convert.ToDecimal(reader["PRELFA"]) : 0,
                    Descuento1 = reader["DT1LFA"] != DBNull.Value ? Convert.ToDecimal(reader["DT1LFA"]) : 0,
                    Iva = reader["IVALFA"] != DBNull.Value ? Convert.ToDecimal(reader["IVALFA"]) : 0,
                    Total = reader["TOTLFA"] != DBNull.Value ? Convert.ToDecimal(reader["TOTLFA"]) : 0
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo líneas de factura: {ex.Message}");
        }
        return lineas;
    }

    public async Task<List<Cobro>> GetCobrosFacturaAsync(string serie, double numero)
    {
        var cobros = new List<Cobro>();
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            
            // Intentamos con DOCCOB primero
            string query = "SELECT CODCOB, FECCOB, IMPCOB, CPTCOB FROM F_COB WHERE DOCCOB = ?";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.Add("?", OleDbType.Double).Value = numero;
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cobros.Add(new Cobro
                {
                    CODCOB = reader["CODCOB"] != DBNull.Value ? Convert.ToDouble(reader["CODCOB"]) : 0,
                    FECCOB = reader["FECCOB"] != DBNull.Value ? Convert.ToDateTime(reader["FECCOB"]) : null,
                    IMPCOB = reader["IMPCOB"] != DBNull.Value ? Convert.ToDecimal(reader["IMPCOB"]) : 0,
                    CPTCOB = reader["CPTCOB"].ToString() ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[F_COB] Error consultando por DOCCOB: {ex.Message}");
        }
        
        // Búsqueda por texto en el concepto si no encontró por DOCCOB
        if (cobros.Count == 0)
        {
            try {
               using var connection2 = new OleDbConnection(GetConnectionString());
               await connection2.OpenAsync();
               string numStr = numero.ToString().PadLeft(6, '0');
               string searchStr = $"%{serie} - {numStr}%"; 
               string query2 = "SELECT CODCOB, FECCOB, IMPCOB, CPTCOB FROM F_COB WHERE CPTCOB LIKE ?";
               using var command2 = new OleDbCommand(query2, connection2);
               command2.Parameters.Add("?", OleDbType.VarWChar).Value = searchStr;
               using var reader2 = await command2.ExecuteReaderAsync();
               while(await reader2.ReadAsync()) {
                   cobros.Add(new Cobro {
                       CODCOB = reader2["CODCOB"] != DBNull.Value ? Convert.ToDouble(reader2["CODCOB"]) : 0,
                       FECCOB = reader2["FECCOB"] != DBNull.Value ? Convert.ToDateTime(reader2["FECCOB"]) : null,
                       IMPCOB = reader2["IMPCOB"] != DBNull.Value ? Convert.ToDecimal(reader2["IMPCOB"]) : 0,
                       CPTCOB = reader2["CPTCOB"].ToString() ?? ""
                   });
               }
            } catch { }
        }

        return cobros;
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
            command.Parameters.Add("?", OleDbType.VarWChar).Value = tip;
            command.Parameters.Add("?", OleDbType.Double).Value = cod;
            
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
        // El usuario indica que las series son fijas del 1 al 9
        return await Task.FromResult(Enumerable.Range(1, 9).Select(i => i.ToString()).ToList());
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
            lista.Add(new Almacen { 
                Codigo = reader["CODALM"].ToString()?.Trim() ?? "", 
                Nombre = reader["NOMALM"].ToString()?.Trim() ?? "" 
            });
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
                {
                    pedido.CODPCL = await GetSiguientePedidoAsync(pedido.TIPPCL ?? "1");
                }
                else
                {
                    // Si el número ya viene dado, es probable que sea una edición.
                    // Limpiamos las líneas y cabecera previas para evitar error de duplicados.
                    string delLinSql = "DELETE FROM F_LPC WHERE TIPLPC = ? AND CODLPC = ?";
                    using var cmdDelLin = new OleDbCommand(delLinSql, connection, transaction);
                    cmdDelLin.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.TIPPCL;
                    cmdDelLin.Parameters.Add("?", OleDbType.Double).Value = pedido.CODPCL;
                    await cmdDelLin.ExecuteNonQueryAsync();

                    string delCabSql = "DELETE FROM F_PCL WHERE TIPPCL = ? AND CODPCL = ?";
                    using var cmdDelCab = new OleDbCommand(delCabSql, connection, transaction);
                    cmdDelCab.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.TIPPCL;
                    cmdDelCab.Parameters.Add("?", OleDbType.Double).Value = pedido.CODPCL;
                    await cmdDelCab.ExecuteNonQueryAsync();
                }

                // 2. Obtener datos del cliente para la cabecera
                string cdo = "", cpo = "", ccp = "", cpr = "", cni = "";
                string cno = pedido.CNOPCL ?? ""; // Nombre por defecto del modelo
                if ((pedido.CLIPCL ?? 0) > 0)
                {
                    // Se añade NOFCLI para forzar el Nombre Fiscal
                    string sqlCli = "SELECT DOMCLI, POBCLI, CPOCLI, PROCLI, NIFCLI, NOFCLI FROM F_CLI WHERE CODCLI = ?";
                    using var cmdCli = new OleDbCommand(sqlCli, connection, transaction);
                    cmdCli.Parameters.Add("?", OleDbType.Double).Value = pedido.CLIPCL;
                    using var readerCli = await cmdCli.ExecuteReaderAsync();
                    if (await readerCli.ReadAsync())
                    {
                        cdo = readerCli["DOMCLI"].ToString() ?? "";
                        cpo = readerCli["POBCLI"].ToString() ?? "";
                        ccp = readerCli["CPOCLI"].ToString() ?? "";
                        cpr = readerCli["PROCLI"].ToString() ?? "";
                        cni = readerCli["NIFCLI"].ToString() ?? "";
                        
                        // Si existe nombre fiscal, reemplazar el CNOPCL por él
                        string nofcli = readerCli["NOFCLI"].ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(nofcli)) 
                        {
                            cno = nofcli;
                            pedido.CNOPCL = cno; // Actualizar el pedido para el return log si aplica
                        }
                    }
                }

                // 3. Obtener los porcentajes globales de IVA para guardar en cabecera
                decimal pctIVA1 = _config.GetValue<decimal>("IvaConfig:0:IVA", 21);
                decimal pctIVA2 = _config.GetValue<decimal>("IvaConfig:1:IVA", 10);
                decimal pctIVA3 = _config.GetValue<decimal>("IvaConfig:2:IVA", 4);
                
                decimal pctRE1 = _config.GetValue<decimal>("IvaConfig:0:RE", 5.2m);
                decimal pctRE2 = _config.GetValue<decimal>("IvaConfig:1:RE", 1.4m);
                decimal pctRE3 = _config.GetValue<decimal>("IvaConfig:2:RE", 0.5m);

                // 4. Agrupar Totales por IVA
                decimal net1 = 0, net2 = 0, net3 = 0;
                decimal iva1 = 0, iva2 = 0, iva3 = 0;
                decimal re1 = 0, re2 = 0, re3 = 0;
                decimal totalDoc = 0;

                foreach (var lin in pedido.Lineas)
                {
                    decimal pctIVA = lin.IvaIndex == 1 ? pctIVA2 : (lin.IvaIndex == 2 ? pctIVA3 : pctIVA1);
                    decimal pctRE = lin.IvaIndex == 1 ? pctRE2 : (lin.IvaIndex == 2 ? pctRE3 : pctRE1);
                    
                    // IMPORTANTE: En F_LPC.IVALPC se guarda el CÓDIGO (0, 1, 2)
                    lin.IVALPC = (double)lin.IvaIndex;

                    decimal baseLin = (decimal?)(lin.TOTLPC) ?? (lin.Cantidad * lin.Precio);
                    decimal cuotaIVA = Math.Round(baseLin * (pctIVA / 100), 2);
                    decimal cuotaRE = (pedido.REQCLI == 1) ? Math.Round(baseLin * (pctRE / 100), 2) : 0;

                    if (lin.IvaIndex == 0) { net1 += baseLin; iva1 += cuotaIVA; re1 += cuotaRE; }
                    else if (lin.IvaIndex == 1) { net2 += baseLin; iva2 += cuotaIVA; re2 += cuotaRE; }
                    else if (lin.IvaIndex == 2) { net3 += baseLin; iva3 += cuotaIVA; re3 += cuotaRE; }

                    totalDoc += baseLin + cuotaIVA + cuotaRE;
                }

                // 5. Insertar Cabecera (F_PCL)
                string sqlCab = @"INSERT INTO F_PCL (TIPPCL, CODPCL, FECPCL, CLIPCL, CNOPCL, ALMPCL, TOTPCL, 
                                 NET1PCL, NET2PCL, NET3PCL, BAS1PCL, BAS2PCL, BAS3PCL,
                                 PIVA1PCL, PIVA2PCL, PIVA3PCL, IIVA1PCL, IIVA2PCL, IIVA3PCL, 
                                 PREC1PCL, PREC2PCL, PREC3PCL, IREC1PCL, IREC2PCL, IREC3PCL, 
                                  REQPCL, ESTPCL, AGEPCL, CDOPCL, CPOPCL, CCPPCL, CPRPCL, CNIPCL, FOPPCL) 
                                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                using var cmdCab = new OleDbCommand(sqlCab, connection, transaction);
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.TIPPCL ?? "1";
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = pedido.CODPCL;
                cmdCab.Parameters.Add("?", OleDbType.DBDate).Value = (pedido.FECPCL ?? DateTime.Now).Date;
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = pedido.CLIPCL ?? 0;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = cno; // Nombre Fiscal
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = !string.IsNullOrEmpty(pedido.ALMPCL) ? pedido.ALMPCL : "GENERAL";
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = totalDoc;
                
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net1; // NET
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net3;
                
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net1; // BAS = NET
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = net3;
                
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pctIVA1; // PIVA
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pctIVA2;
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pctIVA3;
                
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = iva1; // IIVA
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = iva2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = iva3;
                
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pctRE1; // PREC
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pctRE2;
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pctRE3;
                
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = re1; // IREC
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = re2;
                cmdCab.Parameters.Add("?", OleDbType.Currency).Value = re3;
                
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = (double)pedido.REQCLI;
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = 0; // 0 = Pendiente
                cmdCab.Parameters.Add("?", OleDbType.Double).Value = pedido.AGEPCL ?? 0; // Agente
                
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = cdo;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = cpo;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = ccp;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = cpr;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = cni;
                cmdCab.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.FOPPCL ?? ""; // Forma de Pago (FOPPCL)

                await cmdCab.ExecuteNonQueryAsync();

                // 5. Insertar Líneas (F_LPC) - Agregado PENLPC, COSLPC y DT1LPC
                int pos = 1;
                foreach (var lin in pedido.Lineas)
                {
                    string sqlLin = @"INSERT INTO F_LPC (TIPLPC, CODLPC, POSLPC, ARTLPC, DESLPC, CANLPC, PRELPC, TOTLPC, IVALPC, PENLPC, COSLPC, DT1LPC) 
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using var cmdLin = new OleDbCommand(sqlLin, connection, transaction);
                    cmdLin.Parameters.Add("?", OleDbType.VarWChar).Value = pedido.TIPPCL ?? "1";
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = pedido.CODPCL;
                    cmdLin.Parameters.Add("?", OleDbType.Integer).Value = pos++;
                    cmdLin.Parameters.Add("?", OleDbType.VarWChar).Value = lin.ARTLPC ?? "";
                    cmdLin.Parameters.Add("?", OleDbType.VarWChar).Value = lin.DESLPC ?? "";
                    
                    double canLpc = (double)(lin.CANLPC ?? 0);
                    
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = canLpc;
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = (double)(lin.PRELPC ?? 0);
                    cmdLin.Parameters.Add("?", OleDbType.Currency).Value = lin.TOTLPC ?? (lin.Cantidad * lin.Precio);
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = lin.IVALPC ?? 0;
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = canLpc; // PENLPC igual que CANLPC
                    cmdLin.Parameters.Add("?", OleDbType.Currency).Value = lin.COSLPC ?? 0;
                    cmdLin.Parameters.Add("?", OleDbType.Double).Value = (double)(lin.DT1LPC ?? 0);

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



    public async Task<(bool Success, string Message)> EliminarPedidoAsync(string serie, double numero)
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. Eliminar líneas
                string sqlLines = "DELETE FROM F_LPC WHERE TIPLPC = ? AND CODLPC = ?";
                using var cmdLines = new OleDbCommand(sqlLines, connection, transaction);
                cmdLines.Parameters.Add("?", OleDbType.VarWChar).Value = serie;
                cmdLines.Parameters.Add("?", OleDbType.Double).Value = numero;
                await cmdLines.ExecuteNonQueryAsync();

                // 2. Eliminar cabecera
                string sqlHeader = "DELETE FROM F_PCL WHERE TIPPCL = ? AND CODPCL = ?";
                using var cmdHeader = new OleDbCommand(sqlHeader, connection, transaction);
                cmdHeader.Parameters.Add("?", OleDbType.VarWChar).Value = serie;
                cmdHeader.Parameters.Add("?", OleDbType.Double).Value = numero;
                await cmdHeader.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return (true, $"Pedido {serie}-{numero} eliminado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error eliminando pedido: {ex.Message}");
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
            string query = "SELECT CODAGE, NOMAGE, CUWAGE, CAWAGE, SUWAGE, JEQAGE FROM F_AGE WHERE TRIM(CUWAGE) = ? AND TRIM(CAWAGE) = ?";
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
                    SUWAGE = reader["SUWAGE"] != DBNull.Value ? Convert.ToDouble(reader["SUWAGE"]) : 0,
                    JEQAGE = reader["JEQAGE"] != DBNull.Value ? Convert.ToDouble(reader["JEQAGE"]) : 0
                };
                
                // 1. Verificar si tiene el acceso web activado en Factusol (SUWAGE = 1)
                if (agente.TieneAccesoWeb)
                {
                    // 2. Adjuntar permisos desde la configuración global
                    var config = GetGlobalConfig();
                    var key = agente.CODAGE?.ToString() ?? "";
                    
                    if (config.OrderSettings.PermisosAgentes.TryGetValue(key, out var perm))
                    {
                        agente.Permissions = perm;
                    }
                    else if (agente.EsJefe)
                    {
                        // Los jefes tienen todos los permisos por defecto si no están en el diccionario
                        agente.Permissions = new AgentPermission { 
                            AccesoMovil = true, 
                            PermitirDescuentos = true, 
                            PermitirEliminar = true,
                            SoloVerPedidosPropios = false 
                        };
                    }

                    if (config.OrderSettings.RestringirPedidosAAgentes)
                    {
                        if (!agente.Permissions.AccesoMovil) return null; // Acceso denegado por config
                    }

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

    public GlobalConfig GetGlobalConfig()
    {
        var config = new GlobalConfig();
        try {
            // Cargar Impuestos
            var ivaSection = _config.GetSection("IvaConfig");
            if (ivaSection.Exists())
            {
                config.IvaConfig = ivaSection.Get<Dictionary<string, TaxItem>>() ?? new();
            }

            // Cargar Ajustes de Pedido
            var orderSection = _config.GetSection("OrderSettings");
            if (orderSection.Exists())
            {
                config.OrderSettings = orderSection.Get<OrderSettings>() ?? new();
            }

            // Cargar Ajustes de Túnel
            var tunnelSection = _config.GetSection("TunnelConfig");
            if (tunnelSection.Exists())
            {
                config.Tunnel = tunnelSection.Get<Shared.TunnelConfig>() ?? new();
                // Prioridad: ManualUrl > _currentZrokUrl
                config.Tunnel.CurrentUrl = !string.IsNullOrEmpty(config.Tunnel.ManualUrl) 
                                        ? config.Tunnel.ManualUrl 
                                        : _currentZrokUrl;
                config.Tunnel.Status = _lastTunnelStatus;
                config.Tunnel.LastErrorMessage = _lastTunnelError;
            }
        } catch (Exception ex) {
            Console.WriteLine($"[CONFIG] Error leyendo appsettings: {ex.Message}");
        }

        // Garantizar que IvaConfig tenga al menos los índices básicos si está vacío
        if (config.IvaConfig == null || !config.IvaConfig.Any()) {
            config.IvaConfig = new Dictionary<string, TaxItem> {
                { "0", new TaxItem { IVA = 21, RE = 5.2m } },
                { "1", new TaxItem { IVA = 10, RE = 1.4m } },
                { "2", new TaxItem { IVA = 4, RE = 0.5m } }
            };
        }

        return config;
    }

    public async Task<bool> UpdateGlobalConfigAsync(GlobalConfig newConfig)
    {
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
                
                if (json != null)
                {
                    // Update Sections
                    var ivaNode = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(newConfig.IvaConfig));
                    var orderNode = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(newConfig.OrderSettings));
                    
                    json["IvaConfig"] = ivaNode;
                    json["OrderSettings"] = orderNode;

                    // Actualizar TunnelConfig (sin CurrentUrl)
                    var tunnelToSave = new { 
                        Enabled = newConfig.Tunnel.Enabled,
                        Provider = newConfig.Tunnel.Provider,
                        AuthToken = newConfig.Tunnel.AuthToken,
                        ReservedName = newConfig.Tunnel.ReservedName,
                        LocalPort = newConfig.Tunnel.LocalPort,
                        ManualUrl = newConfig.Tunnel.ManualUrl
                    };
                    json["TunnelConfig"] = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(tunnelToSave));
                    
                    await File.WriteAllTextAsync(configPath, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    totalSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando configuración global en {fileName}: {ex.Message}");
            }
        }

        return totalSuccess;
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
    public async Task RestartTunnelAsync()
    {
        _currentZrokUrl = ""; // Limpiar URL vieja
        _consoleLog.Clear();
        AddToLog("<span style='color:#79c0ff;'>[SISTEMA] Reiniciando servicios de túnel...</span>");
        var conf = _config.GetSection("TunnelConfig");
        bool enabled = conf.GetValue<bool>("Enabled");
        string provider = conf.GetValue<string>("Provider") ?? "zrok";
        string? token = conf.GetValue<string>("AuthToken");
        string? name = conf.GetValue<string>("ReservedName");
        int port = conf.GetValue<int>("LocalPort");
        if (port == 0) port = 44373;

        if (!enabled) return;

        // 1. Matar procesos previos
        try {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("ngrok")) p.Kill();
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("zrok")) p.Kill();
        } catch { }

        if (provider.ToLower() == "ngrok")
        {
            await StartNgrokAsync(token, name, port);
        }
        else
        {
            await StartZrokAsync(token, name, port);
        }
    }

    private async Task StartNgrokAsync(string? token, string? domain, int port)
    {
        _lastTunnelStatus = "Iniciando...";
        _lastTunnelError = "";

        var ngrokPath = Path.Combine(AppContext.BaseDirectory, "ngrok", "ngrok.exe");
        if (!File.Exists(ngrokPath)) ngrokPath = Path.Combine(Directory.GetCurrentDirectory(), "ngrok", "ngrok.exe");

        if (!File.Exists(ngrokPath)) {
            _lastTunnelStatus = "Error";
            _lastTunnelError = "No se encontró ngrok.exe en la carpeta /Api/ngrok/";
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                // Configurar Token
                if (!string.IsNullOrEmpty(token)) {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = ngrokPath,
                        Arguments = $"config add-authtoken \"{token}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();
                }

                // Iniciar Túnel
                string args = $"http {port}";
                if (!string.IsNullOrEmpty(domain)) args += $" --domain=\"{domain}\"";

                var pTunnel = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = ngrokPath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true // Capturar errores
                });

                if (pTunnel == null) {
                    _lastTunnelStatus = "Error";
                    _lastTunnelError = "No se pudo arrancar el proceso ngrok.exe";
                    return;
                }

                _lastTunnelStatus = "Negociando túnel...";

                // Esperar un poco a que ngrok levante y consultar su API local
                using var client = new HttpClient();
                bool detectado = false;

                for(int i=0; i<10; i++) // 10 intentos = 20 segundos aprox
                {
                    if (pTunnel.HasExited) {
                        var error = await pTunnel.StandardError.ReadToEndAsync();
                        _lastTunnelStatus = "Error";
                        _lastTunnelError = $"ngrok se cerró inesperadamente: {error}";
                        AddToLog($"<span style='color:#f85149;'>[ERROR] {error}</span>");
                        return;
                    }

                    try {
                        AddToLog("<span style='color:#8b949e;'>[INFO] Consultando API local de ngrok...</span>");
                        var response = await client.GetFromJsonAsync<System.Text.Json.Nodes.JsonNode>("http://localhost:4040/api/tunnels");
                        var tunnels = response?["tunnels"]?.AsArray();
                        if (tunnels != null && tunnels.Count > 0) {
                            var publicUrl = tunnels[0]?["public_url"]?.ToString();
                            if (!string.IsNullOrEmpty(publicUrl)) {
                                SetZrokUrl(publicUrl);
                                Console.WriteLine($"[NGROK] URL detectada: {publicUrl}");
                                detectado = true;
                                break;
                            }
                        }
                    } catch { /* API no lista aún */ }
                    
                    await Task.Delay(2000);
                }

                if (!detectado) {
                    _lastTunnelStatus = "Error";
                    _lastTunnelError = "Tiempo de espera agotado. Verifica tu Authtoken y que el puerto sea el correcto.";
                }
            }
            catch (Exception ex) { 
                _lastTunnelStatus = "Error";
                _lastTunnelError = ex.Message; 
            }
        });
    }

    private async Task StartZrokAsync(string? token, string? name, int port)
    {
        var zrokPath = Path.Combine(AppContext.BaseDirectory, "zrok", "zrok.exe");
        if (!File.Exists(zrokPath)) zrokPath = Path.Combine(AppContext.BaseDirectory, "zrok", "zrok2.exe");
        if (!File.Exists(zrokPath)) zrokPath = Path.Combine(Directory.GetCurrentDirectory(), "zrok", "zrok.exe");
        if (!File.Exists(zrokPath)) zrokPath = Path.Combine(Directory.GetCurrentDirectory(), "zrok", "zrok2.exe");

        if (!File.Exists(zrokPath)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                // Limpiar el token de espacios accidentales y comandos pegados por error
                token = token?.Trim();
                if (!string.IsNullOrEmpty(token)) {
                    if (token.StartsWith("zrok enable ", StringComparison.OrdinalIgnoreCase)) token = token.Substring(12).Trim();
                    else if (token.StartsWith("enable ", StringComparison.OrdinalIgnoreCase)) token = token.Substring(7).Trim();
                    else if (token.StartsWith("zrok ", StringComparison.OrdinalIgnoreCase)) token = token.Substring(5).Trim();
                }

                // 1. Validar y habilitar si hay token
                if (!string.IsNullOrEmpty(token)) {
                    // Detección simple de token equivocado (Ngrok tokens suelen ser largos y con guiones bajos/puntos)
                    if (token.Length > 30 && (token.Contains("_") || token.Contains(".")))
                    {
                        AddToLog("<span style='color:#ff9800;'>[AVISO] El token parece ser de Ngrok pero el proveedor es Zrok.</span>");
                    }

                    string maskedToken = token.Length > 8 ? $"{token.Substring(0, 4)}...{token.Substring(token.Length - 4)}" : "****";
                    AddToLog($"<span style='color:#8b949e;'>[ZROK] Habilitando entorno con token {maskedToken}...</span>");
                    
                    var pEnable = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = zrokPath,
                        Arguments = $"enable \"{token}\" --headless",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    });
                    if (pEnable != null) {
                        var outEnable = await pEnable.StandardOutput.ReadToEndAsync();
                        var errEnable = await pEnable.StandardError.ReadToEndAsync();
                        await pEnable.WaitForExitAsync();
                        
                        if (errEnable.Contains("401") || outEnable.Contains("401")) {
                            AddToLog("<span style='color:#f85149;'>[ERROR] Token de Zrok no autorizado (401). Verifica tu token en la configuración.</span>");
                            return; // Detener si falla la autenticación
                        }
                        
                        if (pEnable.ExitCode != 0 && 
                            !(errEnable.Contains("already") && errEnable.Contains("enabled")) && 
                            !(outEnable.Contains("already") && outEnable.Contains("enabled"))) {
                            
                            AddToLog($"<span style='color:#f85149;'>[ERROR] No se pudo habilitar zrok (Código {pEnable.ExitCode}).</span>");
                            if (!string.IsNullOrEmpty(errEnable)) AddToLog($"[ZROK ERR] {errEnable}");
                            return; // Detener solo si es un error real
                        }
                        
                        AddToLog("<span style='color:#7ee787;'>[ZROK] Entorno verificado/habilitado.</span>");
                    }
                }

                // 2. Consultar Status y verificar si el entorno está cargado
                AddToLog("<span style='color:#8b949e;'>[ZROK] Consultando estado del entorno...</span>");
                var pStatus = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = zrokPath,
                    Arguments = "status",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                });
                if (pStatus != null) {
                    var outStatus = await pStatus.StandardOutput.ReadToEndAsync();
                    var errStatus = await pStatus.StandardError.ReadToEndAsync();
                    await pStatus.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(outStatus)) AddToLog($"[ZROK STATUS]\n{outStatus}");
                    
                    if (outStatus.Contains("unable to load environment") || errStatus.Contains("unable to load environment") || pStatus.ExitCode != 0) {
                        AddToLog("<span style='color:#f85149;'>[ERROR] No se pudo cargar el entorno de zrok. Asegúrate de haber ingresado el Token.</span>");
                        return; // No continuar al share si no hay entorno
                    }
                }

                // 3. Iniciar Share
                string args = string.IsNullOrEmpty(name) ? $"share public http://localhost:{port} --headless" : $"share reserved \"{name}\" --headless";
                AddToLog($"<span style='color:#8b949e;'>[ZROK] Iniciando share: {args}</span>");

                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = zrokPath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process != null) {
                    process.OutputDataReceived += (s, e) => {
                        if (string.IsNullOrEmpty(e.Data)) return;
                        AddToLog(e.Data);
                        
                        var match = System.Text.RegularExpressions.Regex.Match(e.Data, @"[a-zA-Z0-9.-]+\.zrok\.io");
                        if (match.Success && e.Data.Contains("access your zrok share")) {
                            string url = "https://" + match.Value;
                            SetZrokUrl(url);
                            AddToLog("<span style='color:#7ee787; font-weight:bold;'>¡TÚNEL ZROK ACTIVO!</span>");
                        }
                    };
                    process.ErrorDataReceived += (s, e) => {
                        if (string.IsNullOrEmpty(e.Data)) return;
                        AddToLog($"<span style='color:#f85149;'>[ZROK ERR] {e.Data}</span>");
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
            catch (Exception ex) { 
                Console.WriteLine($"[ZROK] Error: {ex.Message}"); 
                AddToLog($"<span style='color:#f85149;'>[ZROK ERROR CRITICO] {ex.Message}</span>");
            }
        });
    }

    private string CalcularDvDIAN(string nitString)
        {
            if (string.IsNullOrEmpty(nitString)) return "";
            string nit = System.Text.RegularExpressions.Regex.Replace(nitString, @"\D", "");
            if (string.IsNullOrEmpty(nit)) return "";

            int[] vpri = new int[] { 3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71 };
            int x = 0;
            int z = nit.Length;

            for (int i = 0; i < z; i++)
            {
                x += (nit[z - 1 - i] - '0') * vpri[i];
            }

            int y = x % 11;
            return (y > 1) ? (11 - y).ToString() : y.ToString();
        }
    }
