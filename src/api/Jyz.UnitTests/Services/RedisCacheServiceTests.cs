using JfYu.Redis.Interface;
using Jyz.Model.RedisCache;
using Jyz.Services;
using StackExchange.Redis;

namespace Jyz.UnitTests.Services
{
    public class RedisCacheServiceTests
    {
        private static (RedisCacheService svc, Mock<IRedisService> redisMock, Mock<IDatabase> dbMock, Mock<IConnectionMultiplexer> connMock)
            CreateService(string[]? keys = null, Dictionary<string, (string value, TimeSpan? ttl)>? data = null)
        {
            var redisMock = new Mock<IRedisService>();
            var dbMock = new Mock<IDatabase>();
            var connMock = new Mock<IConnectionMultiplexer>();
            var serverMock = new Mock<IServer>();

            // Setup endpoints
            var endpoint = new System.Net.DnsEndPoint("localhost", 6379);
            connMock.Setup(c => c.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);
            connMock.Setup(c => c.GetServer(endpoint, null)).Returns(serverMock.Object);

            redisMock.Setup(r => r.Database).Returns(dbMock.Object);
            redisMock.Setup(r => r.Client).Returns(connMock.Object);

            // Setup key scan
            var keyList = keys ?? [];
            var redisKeys = keyList.Select(k => (RedisKey)k).ToAsyncEnumerable();
            serverMock.Setup(s => s.KeysAsync(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(redisKeys);

            // Setup TTL and string get
            foreach (var (key, (value, ttl)) in data ?? [])
            {
                var rk = (RedisKey)key;
                dbMock.Setup(db => db.KeyTimeToLiveAsync(rk, It.IsAny<CommandFlags>()))
                    .ReturnsAsync(ttl);
                dbMock.Setup(db => db.StringGetAsync(rk, It.IsAny<CommandFlags>()))
                    .ReturnsAsync((RedisValue)value);
            }

            var svc = new RedisCacheService(redisMock.Object);
            return (svc, redisMock, dbMock, connMock);
        }

        [Fact]
        public async Task GetAllAsync_NoKeys_ReturnsEmptyList()
        {
            var (svc, _, _, _) = CreateService(keys: []);

            var result = await svc.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_WithKeys_ReturnsList()
        {
            var key = "app:permissions:1";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = ("{\"a\":1}", TimeSpan.FromSeconds(300)) });

            var result = await svc.GetAllAsync();

            result.Should().HaveCount(1);
            result[0].Key.Should().Be(key);
        }

        [Fact]
        public async Task GetAllAsync_WithTtl_SetsTtlField()
        {
            var key = "app:x";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = ("val", TimeSpan.FromSeconds(120)) });

            var result = await svc.GetAllAsync();

            result[0].Ttl.Should().Be(120);
        }

        [Fact]
        public async Task GetAllAsync_NoTtl_SetsTtlMinusOne()
        {
            var key = "app:x";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = ("val", null) });

            var result = await svc.GetAllAsync();

            result[0].Ttl.Should().Be(-1);
        }

        [Fact]
        public async Task GetAllAsync_JsonValue_SetsIsJsonTrue()
        {
            var key = "app:x";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = ("{\"key\":\"value\"}", null) });

            var result = await svc.GetAllAsync();

            result[0].IsJson.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllAsync_NonJsonValue_SetsIsJsonFalse()
        {
            var key = "app:x";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = ("plain text", null) });

            var result = await svc.GetAllAsync();

            result[0].IsJson.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_JsonArrayValue_SetsIsJsonTrue()
        {
            var key = "app:x";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = ("[1,2,3]", null) });

            var result = await svc.GetAllAsync();

            result[0].IsJson.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllAsync_LongValue_TruncatesAt500Chars()
        {
            var key = "app:x";
            var longValue = new string('x', 600);
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = (longValue, null) });

            var result = await svc.GetAllAsync();

            result[0].Value.Should().EndWith("...");
            result[0].Value!.Length.Should().Be(503); // 500 + "..."
        }

        [Fact]
        public async Task GetAllAsync_LongJsonValue_IsJsonCheckedBeforeTruncation()
        {
            // JSON that is longer than 500 chars should still be detected as JSON
            var key = "app:x";
            var longJson = "{\"data\":\"" + new string('a', 500) + "\"}";
            longJson.Length.Should().BeGreaterThan(500);
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = (longJson, null) });

            var result = await svc.GetAllAsync();

            result[0].IsJson.Should().BeTrue();
            result[0].Value.Should().EndWith("...");
        }

        [Fact]
        public async Task DeleteAsync_CallsKeyDeleteWithAllKeys()
        {
            var (svc, _, dbMock, _) = CreateService();
            var keys = new List<string> { "app:a", "app:b" };

            await svc.DeleteAsync(keys);

            dbMock.Verify(db => db.KeyDeleteAsync(
                It.Is<RedisKey[]>(k => k.Length == 2),
                It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_EmptyList_CallsKeyDeleteWithEmptyArray()
        {
            var (svc, _, dbMock, _) = CreateService();

            await svc.DeleteAsync([]);

            dbMock.Verify(db => db.KeyDeleteAsync(
                It.Is<RedisKey[]>(k => k.Length == 0),
                It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_StringGetThrows_ReturnsNonStringTypePlaceholder()
        {
            var key = "app:binary";
            var redisMock = new Mock<IRedisService>();
            var dbMock = new Mock<IDatabase>();
            var connMock = new Mock<IConnectionMultiplexer>();
            var serverMock = new Mock<IServer>();

            var endpoint = new System.Net.DnsEndPoint("localhost", 6379);
            connMock.Setup(c => c.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);
            connMock.Setup(c => c.GetServer(endpoint, null)).Returns(serverMock.Object);
            redisMock.Setup(r => r.Database).Returns(dbMock.Object);
            redisMock.Setup(r => r.Client).Returns(connMock.Object);

            var redisKeys = new[] { (RedisKey)key }.ToAsyncEnumerable();
            serverMock.Setup(s => s.KeysAsync(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(redisKeys);
            dbMock.Setup(db => db.KeyTimeToLiveAsync((RedisKey)key, It.IsAny<CommandFlags>()))
                .ReturnsAsync((TimeSpan?)null);
            dbMock.Setup(db => db.StringGetAsync((RedisKey)key, It.IsAny<CommandFlags>()))
                .ThrowsAsync(new InvalidOperationException("WRONGTYPE Operation against a key holding the wrong kind of value"));

            var svc = new RedisCacheService(redisMock.Object);

            var result = await svc.GetAllAsync();

            result.Should().HaveCount(1);
            result[0].Value.Should().Be("(non-string type)");
            result[0].IsJson.Should().BeFalse();
        }

        // ─── IsValidJson (indirect via GetAllAsync) ───────────────────────────

        [Theory]
        [InlineData("{}", true)]
        [InlineData("[]", true)]
        [InlineData("{\"a\":1}", true)]
        [InlineData("[1,2]", true)]
        [InlineData("plain", false)]
        [InlineData("123", false)]
        [InlineData("null", false)]
        [InlineData("{bad json", false)]
        public async Task GetAllAsync_IsJsonFlag_IsCorrect(string value, bool expectedIsJson)
        {
            var key = "app:x";
            var (svc, _, _, _) = CreateService(
                keys: [key],
                data: new() { [key] = (value, null) });

            var result = await svc.GetAllAsync();

            result[0].IsJson.Should().Be(expectedIsJson);
        }
    }
}
