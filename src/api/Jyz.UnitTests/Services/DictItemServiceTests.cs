using Jyz.Entity;
using Jyz.Model.DictItem;
using Jyz.Services;
using Jyz.UnitTests.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Jyz.UnitTests.Services
{
    public class DictItemServiceTests
    {
        // ─── DictType navigation ────────────────────────────────────────────

        [Fact]
        public async Task DictType_NavigationProperty_LoadsViaInclude()
        {
            var (ctx, _) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "gender", Name = "Gender" });
            ctx.DictItems.Add(new DictItem { DictTypeId = 1, Code = "m", Label = "Male", Sort = 0 });
            await ctx.SaveChangesAsync();

            var item = await ctx.DictItems.Include(i => i.DictType).FirstAsync(i => i.Code == "m");

            item.DictType.Should().NotBeNull();
            item.DictType!.Code.Should().Be("gender");
        }

        // ─── GetPagedAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_NoFilter_ReturnsAll()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            var dt = new DictType { Id = 1, Code = "t", Name = "T" };
            ctx.DictTypes.Add(dt);
            ctx.DictItems.AddRange(
                new DictItem { DictTypeId = 1, Code = "a", Label = "A", Sort = 0 },
                new DictItem { DictTypeId = 1, Code = "b", Label = "B", Sort = 1 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictItemRequest { PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterByDictTypeId_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Id = 1, Code = "t1", Name = "T1" },
                new DictType { Id = 2, Code = "t2", Name = "T2" });
            ctx.DictItems.AddRange(
                new DictItem { DictTypeId = 1, Code = "a", Label = "A", Sort = 0 },
                new DictItem { DictTypeId = 2, Code = "x", Label = "X", Sort = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictItemRequest { DictTypeId = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().DictTypeId.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyCode_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.AddRange(
                new DictItem { DictTypeId = 1, Code = "male", Label = "Male", Sort = 0 },
                new DictItem { DictTypeId = 1, Code = "female", Label = "Female", Sort = 1 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictItemRequest { SearchKey = "male", PageIndex = 1, PageSize = 10 });

            // "male" matches code "male" and code "female"
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_FilterBySearchKeyLabel_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.AddRange(
                new DictItem { DictTypeId = 1, Code = "a", Label = "Alpha", Sort = 0 },
                new DictItem { DictTypeId = 1, Code = "b", Label = "Beta", Sort = 1 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictItemRequest { SearchKey = "Bet", PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
            result.Data!.First().Label.Should().Be("Beta");
        }

        [Fact]
        public async Task GetPagedAsync_FilterByStatus_ReturnsMatches()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.AddRange(
                new DictItem { DictTypeId = 1, Code = "on", Label = "On", Sort = 0, Status = 1 },
                new DictItem { DictTypeId = 1, Code = "off", Label = "Off", Sort = 1, Status = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictItemRequest { Status = 1, PageIndex = 1, PageSize = 10 });

            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_OrderedBySortThenCreatedTime()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.AddRange(
                new DictItem { DictTypeId = 1, Code = "z", Label = "Z", Sort = 10 },
                new DictItem { DictTypeId = 1, Code = "a", Label = "A", Sort = 1 },
                new DictItem { DictTypeId = 1, Code = "m", Label = "M", Sort = 5 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.GetPagedAsync(new QueryDictItemRequest { PageIndex = 1, PageSize = 10 });

            result.Data![0].Code.Should().Be("a");
            result.Data[1].Code.Should().Be("m");
            result.Data[2].Code.Should().Be("z");
        }

        // ─── IsCodeDuplicateAsync ─────────────────────────────────────────────

        [Fact]
        public async Task IsCodeDuplicateAsync_NoDuplicate_ReturnsFalse()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.Add(new DictItem { DictTypeId = 1, Code = "existing", Label = "Existing", Sort = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.IsCodeDuplicateAsync(1, "newcode");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsCodeDuplicateAsync_Duplicate_ReturnsTrue()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.Add(new DictItem { DictTypeId = 1, Code = "dup", Label = "Dup", Sort = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.IsCodeDuplicateAsync(1, "dup");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsCodeDuplicateAsync_DuplicateInDifferentType_ReturnsFalse()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.AddRange(
                new DictType { Id = 1, Code = "t1", Name = "T1" },
                new DictType { Id = 2, Code = "t2", Name = "T2" });
            ctx.DictItems.Add(new DictItem { DictTypeId = 1, Code = "dup", Label = "Dup", Sort = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            // Same code but different DictTypeId → not a duplicate for type 2
            var result = await svc.IsCodeDuplicateAsync(2, "dup");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsCodeDuplicateAsync_WithExcludeId_ReturnsFalse()
        {
            var (ctx, ro) = DbContextFactory.CreateInMemory();
            ctx.DictTypes.Add(new DictType { Id = 1, Code = "t", Name = "T" });
            ctx.DictItems.Add(new DictItem { Id = 10, DictTypeId = 1, Code = "dup", Label = "Dup", Sort = 0 });
            await ctx.SaveChangesAsync();
            var svc = new DictItemService(ctx, ro);

            var result = await svc.IsCodeDuplicateAsync(1, "dup", excludeId: 10);

            result.Should().BeFalse();
        }
    }
}
