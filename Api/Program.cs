using FactuSync.Api.Services;
using FactuSync.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add CORS to allow the Blazor WebAssembly client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();
// Register our Service
builder.Services.AddScoped<IFactusolService, FactusolService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/clientes", async (IFactusolService service, string? busqueda) => 
    await service.GetClientesAsync(busqueda ?? ""));

api.MapGet("/proveedores", async (IFactusolService service) => 
    await service.GetProveedoresAsync());

api.MapGet("/articulos", async (IFactusolService service, string? busqueda, string? familia, int tarifa = 1) => 
    await service.GetArticulosAsync(busqueda ?? "", tarifa, familia));

api.MapGet("/familias", async (IFactusolService service) => 
    await service.GetFamiliasAsync());

api.MapPost("/auth/login", async (IFactusolService service, FactuSync.Shared.LoginRequest req) => 
{
    var agente = await service.LoginAsync(req.Usuario, req.Clave);
    if (agente != null)
        return Results.Ok(agente);
    return Results.Unauthorized();
});

api.MapPost("/settings/validate-master", (IFactusolService service, FactuSync.Shared.ValidateMasterRequest req) => 
{
    var isValid = service.ValidateMasterPassword(req.Password);
    if (isValid)
    {
        return Results.Ok(new { dbPath = service.GetDbPath() });
    }
    return Results.Unauthorized();
});

api.MapGet("/settings/test-connection", async (IFactusolService service) => 
{
    var (success, message) = await service.TestConnectionAsync();
    return success ? Results.Ok(new { message }) : Results.BadRequest(new { message });
});

api.MapPost("/settings/dbpath", async (IFactusolService service, FactuSync.Shared.UpdateDbRequest req) => 
{
    var success = await service.UpdateDbPathAsync(req.NewPath, req.MasterPassword);
    if (success)
        return Results.Ok(new { message = "Ruta actualizada correctamente" });
    return Results.BadRequest(new { message = "Contraseña maestra incorrecta o ruta inválida" });
});

api.MapGet("/pedidos/lineas", async (IFactusolService service, string tip, double cod) => 
    await service.GetPedidoLineasAsync(tip, cod));

api.MapGet("/articulos/imagen", (string path, IFactusolService service) => 
{
    if (string.IsNullOrEmpty(path)) return Results.NotFound();
    
    try 
    {
        string fullPath = path;
        if (!Path.IsPathRooted(path))
        {
            var dbDir = Path.GetDirectoryName(service.GetDbPath());
            fullPath = Path.Combine(dbDir ?? "", path);
        }

        if (File.Exists(fullPath))
        {
            var ext = Path.GetExtension(fullPath).ToLower();
            var contentType = ext == ".png" ? "image/png" : "image/jpeg";
            return Results.File(fullPath, contentType);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sirviendo imagen: {ex.Message}");
    }
    
    return Results.NotFound();
});

api.MapGet("/pedidos", async (IFactusolService service, DateTime? desde, DateTime? hasta, string? serie, double? agentId) => 
    Results.Ok(await service.GetPedidosAsync(desde, hasta, serie, agentId)));
api.MapGet("/pedidos/series", async (IFactusolService service) => Results.Ok(await service.GetSeriesAsync()));
api.MapGet("/pedidos/siguiente", async (IFactusolService service, string serie) => Results.Ok(await service.GetSiguientePedidoAsync(serie)));
api.MapGet("/pedidos/almacenes", async (IFactusolService service) => Results.Ok(await service.GetAlmacenesAsync()));
api.MapGet("/config/global", (IFactusolService service) => 
{
    var globalConfig = service.GetGlobalConfig();
    return Results.Ok(globalConfig);
});

api.MapPost("/config/global", async (IFactusolService service, GlobalConfig newConfig) => 
{
    var success = await service.UpdateGlobalConfigAsync(newConfig);
    if (success) return Results.Ok(new { message = "Configuración global actualizada correctamente" });
    return Results.BadRequest(new { message = "No se pudo actualizar la configuración" });
});
api.MapGet("/pedidos/{serie}/{numero}", async (IFactusolService service, string serie, double numero) => 
{
    var p = await service.GetPedidoAsync(serie, numero);
    return p != null ? Results.Ok(p) : Results.NotFound();
});

api.MapPost("/pedidos", async (IFactusolService service, Pedido pedido) => 
{
    var result = await service.CrearPedidoAsync(pedido);
    if (result.Success) return Results.Ok();
    return Results.BadRequest(result.Message);
});

api.MapGet("/agentes", async (IFactusolService service) => Results.Ok(await service.GetAgentesAsync()));

api.MapDelete("/pedidos", async (IFactusolService service, string serie, double numero) => 
{
    var result = await service.EliminarPedidoAsync(serie, numero);
    if (result.Success) return Results.Ok(new { message = result.Message });
    return Results.BadRequest(new { message = result.Message });
});

app.MapFallbackToFile("index.html");
app.Run();
