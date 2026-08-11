using BinTool.Domain.Entities;
using BinTool.Application.Abstractions;
using BinTool.Infrastructure.Data;
using BinTool.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BinTool.Tests;

// An isolated SQLite database wired to real Identity managers, for exercising the role and user
// admin services against genuine RoleManager / UserManager behaviour rather than mocks. The Admin and Viewer roles arrive from the model's seed data.
public abstract class IdentityTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    protected readonly AppDbContext Db;
    protected readonly RoleManager<ApplicationRole> Roles;
    protected readonly UserManager<ApplicationUser> Users;
    protected readonly MutableCurrentUser CurrentUser = new();

    protected IdentityTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // The seed passwords are strong; relax the rules so test fixtures stay terse.
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        var scoped = _scope.ServiceProvider;

        Db = scoped.GetRequiredService<AppDbContext>();
        Db.Database.EnsureCreated(); // schema plus the seeded Admin and Viewer roles

        Roles = scoped.GetRequiredService<RoleManager<ApplicationRole>>();
        Users = scoped.GetRequiredService<UserManager<ApplicationUser>>();
    }

    protected IAuditLog Audit() => new AuditLog(Db, CurrentUser);

    protected async Task<ApplicationUser> CreateUserAsync(string userName, params string[] roles)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@bintool.local",
            FullName = userName,
            IsActive = true
        };

        await Users.CreateAsync(user, "Passw0rd!");

        if (roles.Length > 0)
        {
            await Users.AddToRolesAsync(user, roles);
        }

        return user;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    protected sealed class MutableCurrentUser : ICurrentUser
    {
        public string? UserId { get; set; }

        public string Name { get; set; } = ICurrentUser.SystemName;
    }
}
