using AbarrotesPOS.App.Components;
using AbarrotesPOS.Data;
using AbarrotesPOS.Data.Repositories;
using AbarrotesPOS.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor Server puede ejecutar varios componentes al mismo tiempo.
// Por eso el DbContext no debe compartirse como Scoped en todo el circuito.
// Transient evita el error: "A second operation was started on this context instance".
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("AbarrotesPOS.Data")
    ),
    contextLifetime: ServiceLifetime.Transient,
    optionsLifetime: ServiceLifetime.Singleton);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("AbarrotesPOS.Data")
    ),
    lifetime: ServiceLifetime.Singleton);

builder.Services.AddScoped<UserSessionService>();

// ?? Nuevos servicios ??????????????????????????????????????????
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
// ?????????????????????????????????????????????????????????????

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();