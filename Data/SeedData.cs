using Microsoft.AspNetCore.Identity;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Crear Rol Analista
        if (!await roleManager.RoleExistsAsync("Analista"))
        {
            await roleManager.CreateAsync(new IdentityRole("Analista"));
        }

        // Crear Rol Cliente (opcional, pero buena práctica)
        if (!await roleManager.RoleExistsAsync("Cliente"))
        {
            await roleManager.CreateAsync(new IdentityRole("Cliente"));
        }

        // Crear Usuario Analista
        var analistaEmail = "analista@plataforma.com";
        if (await userManager.FindByEmailAsync(analistaEmail) == null)
        {
            var analista = new IdentityUser { UserName = analistaEmail, Email = analistaEmail, EmailConfirmed = true };
            await userManager.CreateAsync(analista, "Password123!");
            await userManager.AddToRoleAsync(analista, "Analista");
        }

        // Crear Usuarios Clientes
        var cliente1Email = "cliente1@plataforma.com";
        var cliente1User = await userManager.FindByEmailAsync(cliente1Email);
        if (cliente1User == null)
        {
            cliente1User = new IdentityUser { UserName = cliente1Email, Email = cliente1Email, EmailConfirmed = true };
            await userManager.CreateAsync(cliente1User, "Password123!");
            await userManager.AddToRoleAsync(cliente1User, "Cliente");
        }

        var cliente2Email = "cliente2@plataforma.com";
        var cliente2User = await userManager.FindByEmailAsync(cliente2Email);
        if (cliente2User == null)
        {
            cliente2User = new IdentityUser { UserName = cliente2Email, Email = cliente2Email, EmailConfirmed = true };
            await userManager.CreateAsync(cliente2User, "Password123!");
            await userManager.AddToRoleAsync(cliente2User, "Cliente");
        }

        // Crear Clientes en la tabla Cliente
        if (!context.Clientes.Any())
        {
            var c1 = new Cliente { UsuarioId = cliente1User.Id, IngresosMensuales = 2000m, Activo = true };
            var c2 = new Cliente { UsuarioId = cliente2User.Id, IngresosMensuales = 5000m, Activo = true };
            
            context.Clientes.AddRange(c1, c2);
            await context.SaveChangesAsync();

            // Crear Solicitudes
            var s1 = new SolicitudCredito
            {
                ClienteId = c1.Id,
                MontoSolicitado = 5000m,
                FechaSolicitud = DateTime.UtcNow.AddDays(-1),
                Estado = EstadoSolicitud.Pendiente
            };

            var s2 = new SolicitudCredito
            {
                ClienteId = c2.Id,
                MontoSolicitado = 20000m,
                FechaSolicitud = DateTime.UtcNow.AddDays(-5),
                Estado = EstadoSolicitud.Aprobado
            };

            context.SolicitudesCredito.AddRange(s1, s2);
            await context.SaveChangesAsync();
        }
    }
}
