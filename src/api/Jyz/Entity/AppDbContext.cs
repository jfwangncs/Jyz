using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jyz.Entity
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, IEnumerable<IInterceptor> interceptors) : DbContext(options)
    {
        private readonly IEnumerable<IInterceptor> _interceptors = interceptors;

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<LoginLog> LoginLogs { get; set; }

        public DbSet<DictType> DictTypes { get; set; }

        public DbSet<DictItem> DictItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            foreach (var interceptor in _interceptors)
                optionsBuilder.AddInterceptors(interceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Model-level seed data: regenerated automatically by `dotnet ef migrations add`
            var seedTime = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc);

            const string adminSeedHash = "$2a$11$hVSma9UitEHvj5AeXLBxIOnsKbs93hhFiK77hJtViDzR2uAa91u3i";

            modelBuilder.Entity<Role>().HasData(new Role
            {
                Id = 1,
                Name = "Admin",
                Status = 1,
                CreatedTime = seedTime,
                UpdatedTime = seedTime
            });

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                UserName = "admin",
                Password = adminSeedHash,
                RealName = "小鱼",
                Status = 1,
                LastLoginTime = seedTime,
                CreatedTime = seedTime,
                UpdatedTime = seedTime
            });

            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.HasData(new { RolesId = 1, UsersId = 1 }));
        }
    }
}
