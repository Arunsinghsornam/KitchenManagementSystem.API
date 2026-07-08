
using KitchenManagementSystem.API.Data;
using KitchenManagementSystem.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.Text;

var builder = WebApplication.CreateBuilder(args);


// 1. Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// 2. JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 3. Authorization policies
// 3. Authorization policies
builder.Services.AddAuthorization(options =>
{
    // Super Admin / Power Admin only
    options.AddPolicy("SuperAdmin",
        p => p.RequireRole("super_admin", "power_admin"));

    // Super Admin + Power Admin + Store Manager
    options.AddPolicy("Manager",
        p => p.RequireRole(
            "super_admin",
            "power_admin",
            "store_manager"));

    // Dashboard (all authenticated users)
    options.AddPolicy("AnyStaff",
        p => p.RequireRole(
            "super_admin",
            "power_admin",
            "store_manager",
            "accountant",
            "kitchen_staff"));

    // Inventory
    options.AddPolicy("InventoryAccess",
        p => p.RequireRole(
            "super_admin",
            "power_admin",
            "store_manager",
            "kitchen_staff"));

    // Recipes
    options.AddPolicy("RecipeAccess",
        p => p.RequireRole(
            "super_admin",
            "power_admin",
            "store_manager",
            "kitchen_staff"));

    // Suppliers + Purchases + Sales
    options.AddPolicy("StoreOperations",
        p => p.RequireRole(
            "super_admin",
            "power_admin",
            "store_manager"));

    // Profit & Loss
    options.AddPolicy("PLAccess",
        p => p.RequireRole(
            "super_admin",
            "power_admin",
            "store_manager",
            "accountant"));
});

// 4. Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. CORS — allow Angular app to call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddScoped<IPLReportService, PLReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOutletService, OutletService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// 6. Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

// Serve static files from the uploads directory
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();