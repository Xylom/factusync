using System.Data.OleDb;
using FactuSync.Shared;

namespace FactuSync.Api.Services;

public interface IFactusolService
{
    Task<List<Cliente>> GetClientesAsync(string busqueda);
    Task<List<Proveedor>> GetProveedoresAsync();
    Task<List<Articulo>> GetArticulosAsync(string busqueda, int tarifa = 1, string? familia = null);
    Task<List<Familia>> GetFamiliasAsync();
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
    GlobalConfig GetGlobalConfig();
    Task<bool> UpdateGlobalConfigAsync(GlobalConfig newConfig);
    Task<(bool Success, string Message)> EliminarPedidoAsync(string serie, double numero);
    Task<List<Agente>> GetAgentesAsync();
    Task<Pedido?> GetPedidoAsync(string serie, double numero);
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

    public async Task<Pedido?> GetPedidoAsync(string serie, double numero)
    {
        try
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();
            string query = "SELECT TIPPCL, CODPCL, FECPCL, CLIPCL, CNOPCL, NETPCL, TOTPCL, ALMPCL, FOPPCL, ESTPCL FROM F_PCL WHERE TIPPCL = ? AND CODPCL = ?";
            using var command = new OleDbCommand(query, connection);
            command.Parameters.AddWithValue("?", serie);
            command.Parameters.AddWithValue("?", numero);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var p = new Pedido
                {
                    TIPPCL = reader["TIPPCL"].ToString() ?? "",
                    CODPCL = Convert.ToDouble(reader["CODPCL"]),
                    Fecha = Convert.ToDateTime(reader["FECPCL"]),
                    CodigoCliente = reader["CLIPCL"].ToString() ?? "",
                    CNOPCL = reader["CNOPCL"].ToString() ?? "",
                    ALMPCL = reader["ALMPCL"].ToString() ?? "",
                    FOPPCL = reader["FOPPCL"].ToString() ?? "",
                    ESTPCL = reader["ESTPCL"] != DBNull.Value ? Convert.ToInt32(reader["ESTPCL"]) : 0,
                    Total = reader["TOTPCL"] != DBNull.Value ? Convert.ToDecimal(reader["TOTPCL"]) : 0
                };
                p.Lineas = await GetPedidoLineasAsync(serie, numero);
                return p;
            }
        }
        catch (Exception ex) { Console.WriteLine($"Error GetPedido: {ex.Message}"); }
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

    public async Task<List<Cliente>> GetClientesAsync(string busqueda)
    {
        var clientes = new List<Cliente>();
        try 
        {
            using var connection = new OleDbConnection(GetConnectionString());
            await connection.OpenAsync();

            string query = "SELECT CODCLI, NOFCLI, NOCCLI, NIFCLI, TELCLI, EMACLI, DOMCLI, REQCLI, TARCLI, FPACLI, DT1CLI FROM F_CLI";
            bool esNumero = double.TryParse(busqueda, out double codigoNum);

            if (!string.IsNullOrEmpty(busqueda))
            {
                if (esNumero)
                    query += " WHERE CODCLI = ? OR NOCCLI LIKE ? OR NOFCLI LIKE ?";
                else
                    query += " WHERE NOCCLI LIKE ? OR NOFCLI LIKE ?";
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
                    DT1CLI = reader["DT1CLI"] != DBNull.Value ? Convert.ToDecimal(reader["DT1CLI"]) : 0
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
            
            // SOLUCIÓN DUPLICADOS: F_STO tiene una fila por almacén, el LEFT JOIN multiplicaba artículos.
            // Se usan subconsultas para STOCK y PRELTA_TAR para garantizar UNA fila por artículo.
            // IMPORTANTE: OLEDB/Access NO soporta '?' dentro de subconsultas correlacionadas.
            // Al intentarlo, Access trata columnas de F_ART como parámetros desconocidos.
            // Solución: insertar 'tarifa' (int controlado) directamente en el SQL para esa subconsulta.
            // Los filtros LIKE siguen usando '?' de forma segura en la cláusula WHERE principal.
            string query = $@"SELECT 
                F_ART.CODART   AS CODART,
                F_ART.DESART   AS DESART,
                F_ART.FAMART   AS FAMART,
                F_ART.IMGART   AS IMGART,
                F_ART.PCOART   AS PCOART,
                F_ART.DT0ART   AS DT0ART,
                F_ART.TIVART   AS TIVART,
                (SELECT SUM(ACTSTO) FROM F_STO WHERE F_STO.ARTSTO = F_ART.CODART) AS STOCK_ACT,
                (SELECT TOP 1 PRELTA FROM F_LTA WHERE F_LTA.ARTLTA = F_ART.CODART AND F_LTA.TARLTA = {tarifa}) AS PRELTA_TAR
                FROM F_ART";

            if (!string.IsNullOrEmpty(busqueda))
            {
                query += " WHERE (F_ART.DESART LIKE ? OR F_ART.CODART LIKE ?)";
                if (!string.IsNullOrEmpty(familia))
                    query += " AND F_ART.FAMART = ?";
            }
            else if (!string.IsNullOrEmpty(familia))
            {
                query += " WHERE F_ART.FAMART = ?";
            }

            using var command = new OleDbCommand(query, connection);
            // Los únicos parámetros '?' que quedan son los filtros LIKE de búsqueda y FAMART
            if (!string.IsNullOrEmpty(busqueda))
            {
                command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
                command.Parameters.Add("?", OleDbType.VarWChar).Value = $"%{busqueda}%";
            }
            
            if (!string.IsNullOrEmpty(familia))
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
                    string codart = reader["CODART"].ToString()?.Trim() ?? "?";
                    string desart = reader["DESART"].ToString()?.Trim() ?? "?";

                    bool  tarifaEsNull = reader["PRELTA_TAR"] == DBNull.Value;
                    decimal pTar = tarifaEsNull ? 0 : Convert.ToDecimal(reader["PRELTA_TAR"]);
                    decimal pco  = reader["PCOART"] != DBNull.Value ? Convert.ToDecimal(reader["PCOART"]) : 0;

                    // LOG: explicar por qué la tarifa no cargó precio
                    if (tarifaEsNull)
                        Console.WriteLine($"[TARIFA] Art {codart} '{desart}': PRELTA_TAR es NULL => no existe fila en F_LTA para tarifa {tarifa}. PrecioVenta = 0.");
                    else if (pTar == 0)
                        Console.WriteLine($"[TARIFA] Art {codart} '{desart}': PRELTA_TAR = 0 en F_LTA (precio registrado en cero para tarifa {tarifa}). PrecioVenta = 0.");
                    else
                        Console.WriteLine($"[TARIFA] Art {codart} '{desart}': Tarifa {tarifa} OK => PRELTA_TAR = {pTar}.");

                    // Si no hay precio de tarifa se muestra 0, NO se usa el precio de costo
                    decimal precioFinal = pTar;

                    articulos.Add(new Articulo
                    {
                        CODART         = codart,
                        DESART         = desart,
                        FAMART         = reader["FAMART"].ToString()?.Trim() ?? "",
                        IMGART         = reader["IMGART"] != DBNull.Value ? reader["IMGART"].ToString()?.Trim() ?? "" : "",
                        PrecioVenta    = precioFinal,
                        PRELTA_TAR_RAW = pTar,
                        PrecioConIva   = conIva,
                        PCOART         = pco,
                        DT0ART         = reader["DT0ART"]    != DBNull.Value ? Convert.ToDecimal(reader["DT0ART"])   : 0,
                        STOART         = reader["STOCK_ACT"] != DBNull.Value ? Convert.ToDouble(reader["STOCK_ACT"]) : 0,
                        IvaIndex       = reader["TIVART"]    != DBNull.Value ? Convert.ToInt32(reader["TIVART"])     : 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error DB: {ex.Message}");
            // Fallback extremadamente básico si la query compleja falla
            Console.WriteLine($"[TARIFA] FALLBACK activado por error en consulta principal. Los artículos mostrarán PrecioVenta = 0 (sin tarifa).");
            try {
                using var conn2 = new OleDbConnection(GetConnectionString());
                await conn2.OpenAsync();
                string qBasic = "SELECT CODART, DESART, PCOART FROM F_ART";
                if (!string.IsNullOrEmpty(busqueda)) qBasic += " WHERE DESART LIKE ? OR CODART LIKE ?";
                using var cmdBasic = new OleDbCommand(qBasic, conn2);
                if (!string.IsNullOrEmpty(busqueda)) {
                    cmdBasic.Parameters.Add("?", OleDbType.VarWChar).Value = "%" + busqueda + "%";
                    cmdBasic.Parameters.Add("?", OleDbType.VarWChar).Value = "%" + busqueda + "%";
                }
                using var rd2 = await cmdBasic.ExecuteReaderAsync();
                while (await rd2.ReadAsync()) {
                    string cod = rd2["CODART"].ToString()?.Trim() ?? "";
                    decimal pco = rd2["PCOART"] != DBNull.Value ? Convert.ToDecimal(rd2["PCOART"]) : 0;
                    Console.WriteLine($"[TARIFA] FALLBACK Art {cod}: PrecioVenta = 0 (no se pudo consultar tarifa {tarifa}). Costo en BD = {pco}.");
                    articulos.Add(new Articulo {
                        CODART = cod,
                        DESART = rd2["DESART"].ToString()?.Trim() ?? "",
                        PrecioVenta = 0,   // Sin tarifa => precio 0, no se expone el costo
                        PCOART = pco
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
            
            // Traemos los pedidos recientes para sincronización y estadísticas
            string query = "SELECT TIPPCL, CODPCL, FECPCL, CNOPCL, TOTPCL, CLIPCL FROM F_PCL ORDER BY FECPCL DESC, CODPCL DESC";
            
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
                    TOTPCL = reader["TOTPCL"] != DBNull.Value ? Convert.ToDecimal(reader["TOTPCL"]) : 0,
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
                    pedido.CODPCL = await GetSiguientePedidoAsync(pedido.TIPPCL ?? "1");

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
                                  REQPCL, ESTPCL, CDOPCL, CPOPCL, CCPPCL, CPRPCL, CNIPCL, FOPPCL) 
                                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

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
                    // 2. Verificar restricciones adicionales en la configuración global
                    var config = GetGlobalConfig();
                    if (config.OrderSettings.RestringirPedidosAAgentes)
                    {
                        var key = agente.CODAGE?.ToString() ?? "";
                        if (config.OrderSettings.PermisosAgentes.TryGetValue(key, out var perm))
                        {
                            if (!perm.AccesoMovil) return null; // Acceso denegado por config
                        }
                        else
                        {
                            // Si no está en el diccionario y hay restricción, denegar
                            return null;
                        }
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
}
