using Microsoft.EntityFrameworkCore;
using KitchenManagementSystem.API.Models;

namespace KitchenManagementSystem.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SalesItems => Set<SaleItem>();
    public DbSet<User> Users => Set<User>();

    // Added for Inventory Controller
    public DbSet<StockLedger> StockLedger => Set<StockLedger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tell EF Core the exact table names in SQL Server
        modelBuilder.Entity<Outlet>().ToTable("Outlets");
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<RawMaterial>().ToTable("RawMaterials");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");
        modelBuilder.Entity<MenuItem>().ToTable("MenuItems");
        modelBuilder.Entity<RecipeIngredient>().ToTable("RecipeIngredients");
        modelBuilder.Entity<Purchase>().ToTable("Purchases");
        modelBuilder.Entity<PurchaseItem>().ToTable("PurchaseItems");
        modelBuilder.Entity<Sale>().ToTable("Sales");
        modelBuilder.Entity<SaleItem>().ToTable("SalesItems");

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");               // already exists in DB

            b.HasKey(u => u.Id);

            b.Property(u => u.Email)
             .IsRequired()
             .HasMaxLength(256);

            b.HasIndex(u => u.Email)
             .IsUnique();                     // one account per email address

            b.Property(u => u.PasswordHash)
             .IsRequired();

            b.Property(u => u.FullName)
             .HasMaxLength(200);

            b.Property(u => u.Role)
             .IsRequired()
             .HasMaxLength(50);

            b.Property(u => u.RefreshToken)
             .HasMaxLength(512);

            // FK to Outlets (nullable — SuperAdmin has no outlet)
            b.HasOne(u => u.Outlet)
             .WithMany()
             .HasForeignKey(u => u.OutletId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Added for Inventory Controller
        modelBuilder.Entity<StockLedger>().ToTable("StockLedger");
    }
}