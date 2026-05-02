using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Aparesk.Eskineria.Persistence;
using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Domain.Enums;

var services = new ServiceCollection();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=aparesk;Username=postgres;Password=postgres")); // Assuming standard connection, wait, I don't know the connection string. Let's look at appsettings.json
