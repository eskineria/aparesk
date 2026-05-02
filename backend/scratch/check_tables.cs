using Microsoft.EntityFrameworkCore;
using Aparesk.Eskineria.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("Server=localhost,1433;Database=Aparesk.Eskineria;User Id=sa;Password=Password123!;TrustServerCertificate=True;"));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

var tables = db.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();
Console.WriteLine("Tables in DB:");
foreach (var table in tables) {
    Console.WriteLine("- " + table);
}
