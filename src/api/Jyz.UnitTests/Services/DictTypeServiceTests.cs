using Jyz.Entity;
using Jyz.Model.DictType;
using Jyz.Services;
using Jyz.UnitTests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Jyz.UnitTests.Services
{
    public class DictTypeServiceTests
    {
        // ─── GetPagedAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAll()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Code = "gender", Name = "Gender" },
                new DictType { Code = "status", Name = "Status" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictTypeRequest { PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyCode_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Code = "gender", Name = "Gender" },
                new DictType { Code = "status", Name = "Status" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictTypeRequest { SearchKey = "gen", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Code.Should().Be("gender");
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyName_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Code = "a", Name = "Alpha" },
                new DictType { Code = "b", Name = "Beta" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictTypeRequest { SearchKey = "Bet", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Name.Should().Be("Beta");
        }

        [Fact]
        public async Task GetPagedAsync_FilterByStatus_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Code = "a", Name = "A", Status = 1 },
                new DictType { Code = "b", Name = "B", Status = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictTypeRequest { Status = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_OrderedByCreatedTimeDesc()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var now = DateTime.UtcNow;
            var t1 = new DictType { Code = "old", Name = "Old" };
            var t2 = new DictType { Code = "new", Name = "New" };
            ctx.DictTypes.AddRange(t1, t2);
            await ctx.SaveChangesAsync();
            t1.CreatedTime = now.AddDays(-10);
            t2.CreatedTime = now.AddDays(-1);
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictTypeRequest { PageIndex = 1, PageSize = 10 });

            result.Data!.First().Code.Should().Be("new");
        }

        // ─── IsCodeDuplicateAsync ─────────────────────────────────────────────

        [Fact]
        public async Task IsCodeDuplicateAsync_NoDuplicate_ReturnsFalse()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Code = "existing", Name = "Existing" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.IsCodeDuplicateAsync("newcode");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsCodeDuplicateAsync_Duplicate_ReturnsTrue()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Code = "dup", Name = "Dup" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.IsCodeDuplicateAsync("dup");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsCodeDuplicateAsync_SameCodeWithExcludeId_ReturnsFalse()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "dup", Name = "Dup" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            // Same code but excluding the own ID → not a duplicate
            var result = await svc.IsCodeDuplicateAsync("dup", excludeId: 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsCodeDuplicateAsync_DifferentIdSameCode_ReturnsTrue()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Id = 1, Code = "dup", Name = "Dup1" },
                new DictType { Id = 2, Code = "dup", Name = "Dup2" });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            // Exclude id=1 but id=2 still has same code
            var result = await svc.IsCodeDuplicateAsync("dup", excludeId: 1);

            result.Should().BeTrue();
        }

        // ─── GetByIdWithItemsAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetByIdWithItemsAsync_Exists_ReturnsWithItems()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var dt = new DictType { Id = 1, Code = "t", Name = "T" };
            dt.Items.Add(new DictItem { Code = "i1", Label = "Item 1", Sort = 0 });
            ctx.DictTypes.Add(dt);
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetByIdWithItemsAsync(1);

            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdWithItemsAsync_NotExists_ReturnsNull()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetByIdWithItemsAsync(999);

            result.Should().BeNull();
        }

        // ─── GetAllWithItemsAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetAllWithItemsAsync_ReturnsOnlyActiveTypes()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Code = "a", Name = "A", Status = 1 },
                new DictType { Code = "b", Name = "B", Status = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetAllWithItemsAsync();

            result.Should().HaveCount(1);
            result[0].Code.Should().Be("a");
        }

        [Fact]
        public async Task GetAllWithItemsAsync_ItemsFilteredByActiveStatus()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var dt = new DictType { Code = "t", Name = "T", Status = 1 };
            dt.Items.Add(new DictItem { Code = "active", Label = "Active", Sort = 0, Status = 1 });
            dt.Items.Add(new DictItem { Code = "inactive", Label = "Inactive", Sort = 1, Status = 0 });
            ctx.DictTypes.Add(dt);
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetAllWithItemsAsync();

            result[0].Items.Should().HaveCount(1);
            result[0].Items.First().Code.Should().Be("active");
        }

        [Fact]
        public async Task GetAllWithItemsAsync_OrderedByCode()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Code = "z", Name = "Z", Status = 1 },
                new DictType { Code = "a", Name = "A", Status = 1 });
            await ctx.SaveChangesAsync();
            var svc = new DictTypeService(ctx, ro);

            var result = await svc.GetAllWithItemsAsync();

            result[0].Code.Should().Be("a");
            result[1].Code.Should().Be("z");
        }
    }
}
