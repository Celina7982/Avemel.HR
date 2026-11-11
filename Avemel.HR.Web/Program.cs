using Avemel.HR.Web.Components;
using Avemel.HR.Web.Components.Account;
using Avemel.HR.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Avemel.HR.Infrastructure.Identity;
using Avemel.HR.Infrastructure.Persistence;
using Avemel.HR.Infrastructure.Services;
using Avemel.HR.Application.Employees;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<EmployeeNumberService>();
builder.Services.AddScoped<IEmployeeCreationService, EmployeeCreationService>();
builder.Services.AddScoped<IEmployeeUpdateService, EmployeeUpdateService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();


var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Existing DbContext registration used by ASP.NET Core Identity.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// DbContext factory used by Blazor components.
builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseNpgsql(connectionString),
    lifetime: ServiceLifetime.Scoped);


builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

//creates roles if they don't exist

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
    .GetRequiredService<ApplicationDbContext>();

    await DivisionSeeder.SeedAsync(db);

    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "ADMIN", "MANAGER", "STAFF" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role {role}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    var adminEmail = app.Configuration["DevelopmentAdmin:Email"];

    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            throw new InvalidOperationException(
                "Development admin account not found. Register the account first.");
        }

        if (!await userManager.IsInRoleAsync(adminUser, "ADMIN"))
        {
            var result = await userManager.AddToRoleAsync(adminUser, "ADMIN");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign ADMIN role: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run(); // Binding to 127.0.0.1:5287
