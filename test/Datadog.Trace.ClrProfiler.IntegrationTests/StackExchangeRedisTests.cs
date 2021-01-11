// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class StackExchangeRedisTests : TestHelper
    {
        public StackExchangeRedisTests(ITestOutputHelper output)
            : base("StackExchange.Redis", output)
        {
        }

        public static IEnumerable<object[]> TestParameters()
        {
            foreach (var versions in PackageVersions.StackExchangeRedis)
            {
                foreach (var version in versions)
                {
                    yield return new object[] { version, "true" };
                    yield return new object[] { version, "false" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestParameters))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion, string tagCommands)
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            envVars["SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS"] = tagCommands;

            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"{TestPrefix}", packageVersion: packageVersion, envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var expected = new TupleList<string, string>
                {
                    { "SET", $"SET {TestPrefix}StackExchange.Redis.INCR" },
                    { "PING", "PING" },
                    { "INCR", $"INCR {TestPrefix}StackExchange.Redis.INCR" },
                    { "INCRBYFLOAT", $"INCRBYFLOAT {TestPrefix}StackExchange.Redis.INCR" },
                    { "GET", $"GET {TestPrefix}StackExchange.Redis.INCR" },
                    { "DDCUSTOM", "DDCUSTOM" },
                    { "ECHO", "ECHO" },
                    { "SLOWLOG", "SLOWLOG" },
                    { "TIME", "TIME" },
                };

                if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.2.2") < 0)
                {
                    expected.Remove(new Tuple<string, string>("DDCUSTOM", "DDCUSTOM"));
                    expected.Remove(new Tuple<string, string>("ECHO", "ECHO"));
                    expected.Remove(new Tuple<string, string>("SLOWLOG", "SLOWLOG"));
                    expected.Remove(new Tuple<string, string>("TIME", "TIME"));
                }

                var batchPrefix = $"{TestPrefix}StackExchange.Redis.Batch.";
                expected.AddRange(new TupleList<string, string>
                {
                    { "DEBUG", $"DEBUG {batchPrefix}DebugObjectAsync" },
                    { "DDCUSTOM", $"DDCUSTOM" }, // Only present on 1.2.2+
                    { "GEOADD", $"GEOADD {batchPrefix}GeoAddAsync" }, // Only present on 1.2.0+
                    { "GEODIST", $"GEODIST {batchPrefix}GeoDistanceAsync" }, // Only present on 1.2.0+
                    { "GEOHASH", $"GEOHASH {batchPrefix}GeoHashAsync" }, // Only present on 1.2.0+
                    { "GEOPOS", $"GEOPOS {batchPrefix}GeoPositionAsync" }, // Only present on 1.2.0+
                    { "GEORADIUSBYMEMBER", $"GEORADIUSBYMEMBER {batchPrefix}GeoRadiusAsync" }, // Only present on 1.2.0+
                    { "ZREM", $"ZREM {batchPrefix}GeoRemoveAsync" }, // Only present on 1.2.0+
                    { "HINCRBYFLOAT", $"HINCRBYFLOAT {batchPrefix}HashDecrementAsync" },
                    { "HDEL", $"HDEL {batchPrefix}HashDeleteAsync" },
                    { "HEXISTS", $"HEXISTS {batchPrefix}HashExistsAsync" },
                    { "HGETALL", $"HGETALL {batchPrefix}HashGetAllAsync" },
                    { "HINCRBYFLOAT", $"HINCRBYFLOAT {batchPrefix}HashIncrementAsync" },
                    { "HKEYS", $"HKEYS {batchPrefix}HashKeysAsync" },
                    { "HLEN", $"HLEN {batchPrefix}HashLengthAsync" },
                    { "HMSET", $"HMSET {batchPrefix}HashSetAsync" },
                    { "HVALS", $"HVALS {batchPrefix}HashValuesAsync" },
                    { "PFADD", $"PFADD {batchPrefix}HyperLogLogAddAsync" }, // Only present on 1.0.242+
                    { "PFCOUNT", $"PFCOUNT {batchPrefix}HyperLogLogLengthAsync" }, // Only present on 1.0.242+
                    { "PFMERGE", $"PFMERGE {batchPrefix}HyperLogLogMergeAsync" }, // Only present on 1.0.242+
                    { "PING", $"PING" },
                    // { "DEL", $"DEL key" },
                    { "DUMP", $"DUMP key" },
                    { "EXISTS", $"EXISTS key" },
                    { "PEXPIREAT", $"PEXPIREAT key" },
                    { "MOVE", $"MOVE key" },
                    { "PERSIST", $"PERSIST key" },
                    { "RANDOMKEY", $"RANDOMKEY" },
                    { "RENAME", "RENAME key1" },
                    { "RESTORE", "RESTORE key" },
                    { "TYPE", "TYPE key" },
                    { "LINDEX", "LINDEX listkey" },
                    { "LINSERT", "LINSERT listkey" },
                    { "LINSERT", "LINSERT listkey" },
                    { "LPOP", "LPOP listkey" },
                    { "LPUSH", "LPUSH listkey" },
                    { "LLEN", "LLEN listkey" },
                    { "LRANGE", "LRANGE listkey" },
                    { "LREM", "LREM listkey" },
                    { "RPOP", "RPOP listkey" },
                    { "RPOPLPUSH", "RPOPLPUSH listkey" },
                    { "RPUSH", "RPUSH listkey" },
                    { "LSET", "LSET listkey" },
                    { "LTRIM", "LTRIM listkey" },
                    { "GET", "GET listkey" },
                    { "SET", "SET listkey" },
                    { "PUBLISH", "PUBLISH channel" },
                    { "SADD", "SADD setkey" },
                    { "SUNIONSTORE", "SUNIONSTORE setkey" },
                    { "SUNION", "SUNION setkey1" },
                    { "SISMEMBER", "SISMEMBER setkey" },
                    { "SCARD", "SCARD setkey" },
                    { "SMEMBERS", "SMEMBERS setkey" },
                    { "SMOVE", "SMOVE setkey1" },
                    { "SPOP", "SPOP setkey1" },
                    { "SRANDMEMBER", "SRANDMEMBER setkey" },
                    { "SRANDMEMBER", "SRANDMEMBER setkey" },
                    { "SREM", "SREM setkey" },
                    { "SORT", "SORT setkey" }, // Only present on 1.0.206+
                    { "SORT", "SORT setkey" }, // Only present on 1.0.206+
                    { "ZUNIONSTORE", "ZUNIONSTORE ssetkey1" }, // Only present on 1.0.206+
                    { "ZADD", "ZADD ssetkey" },
                    { "ZINCRBY", "ZINCRBY ssetkey" },
                    { "ZINCRBY", "ZINCRBY ssetkey" },
                    { "ZCARD", "ZCARD ssetkey" },
                    { "ZRANGE", "ZRANGE ssetkey" },
                    { "ZRANGE", "ZRANGE ssetkey" },
                    { "ZRANGEBYSCORE", "ZRANGEBYSCORE ssetkey" },
                    { "ZRANGEBYSCORE", "ZRANGEBYSCORE ssetkey" },
                    { "ZRANK", "ZRANK ssetkey" },
                    { "ZREM", "ZREM ssetkey" },
                    { "ZREMRANGEBYRANK", "ZREMRANGEBYRANK ssetkey" },
                    { "ZREMRANGEBYSCORE", "ZREMRANGEBYSCORE ssetkey"  },
                    { "ZSCORE", "ZSCORE ssestkey" },
                    { "ZLEXCOUNT", "ZLEXCOUNT ssetkey" }, // Only present on 1.0.273+
                    { "ZRANGEBYLEX", "ZRANGEBYLEX ssetkey" }, // Only present on 1.0.273+
                    { "ZREMRANGEBYLEX", "ZREMRANGEBYLEX ssetkey" }, // Only present on 1.0.273+
                    { "APPEND", "APPEND ssetkey" },
                    { "BITCOUNT", "BITCOUNT ssetkey" },
                    { "BITOP", "BITOP" },
                    { "BITPOS", "BITPOS ssetkey1" },
                    { "INCRBYFLOAT", "INCRBYFLOAT key" },
                    { "GET", "GET key" },
                    { "GETBIT", "GETBIT key" },
                    { "GETRANGE", "GETRANGE key" },
                    { "GETSET", "GETSET key" },
                    { "INCR", "INCR key" },
                    { "STRLEN", "STRLEN key" },
                    { "SET", "SET key" },
                    { "SETBIT", "SETBIT key" },
                    { "SETRANGE", "SETRANGE key" },
                });

                FilterExpectedResultsByApiVersion(expected, packageVersion);

                var dbPrefix = $"{TestPrefix}StackExchange.Redis.Database.";
                expected.AddRange(new TupleList<string, string>
                {
                    { "DEBUG", $"DEBUG {dbPrefix}DebugObject" },
                    { "DDCUSTOM", $"DDCUSTOM" }, // Only present on 1.2.2+
                    { "GEOADD", $"GEOADD {dbPrefix}Geo" }, // Only present on 1.2.0+
                    { "GEODIST", $"GEODIST {dbPrefix}Geo" }, // Only present on 1.2.0+
                    { "GEOHASH", $"GEOHASH {dbPrefix}Geo" }, // Only present on 1.2.0+
                    { "GEOPOS", $"GEOPOS {dbPrefix}Geo" }, // Only present on 1.2.0+
                    { "GEORADIUSBYMEMBER", $"GEORADIUSBYMEMBER {dbPrefix}Geo" }, // Only present on 1.2.0+
                    { "ZREM", $"ZREM {dbPrefix}Geo" }, // Only present on 1.2.0+
                    { "HINCRBYFLOAT", $"HINCRBYFLOAT {dbPrefix}Hash" },
                    { "HDEL", $"HDEL {dbPrefix}Hash" },
                    { "HEXISTS", $"HEXISTS {dbPrefix}Hash" },
                    { "HGET", $"HGET {dbPrefix}Hash" },
                    { "HGETALL", $"HGETALL {dbPrefix}Hash" },
                    { "HINCRBY", $"HINCRBY {dbPrefix}Hash" },
                    { "HKEYS", $"HKEYS {dbPrefix}Hash" },
                    { "HLEN", $"HLEN {dbPrefix}Hash" },
                    // { "HSCAN", $"HSCAN {dbPrefix}Hash" },
                    { "HMSET", $"HMSET {dbPrefix}Hash" },
                    { "HVALS", $"HVALS {dbPrefix}Hash" },
                    { "PFADD", $"PFADD {dbPrefix}HyperLogLog" }, // Only present on 1.0.242+
                    { "PFCOUNT", $"PFCOUNT {dbPrefix}HyperLogLog" }, // Only present on 1.0.242+
                    { "PFMERGE", $"PFMERGE {dbPrefix}HyperLogLog2" }, // Only present on 1.0.242+
                    // { "DEL", $"DEL {dbPrefix}Key" },
                    { "DUMP", $"DUMP {dbPrefix}Key" },
                    { "EXISTS", $"EXISTS {dbPrefix}Key" },
                    { "PEXPIREAT", $"PEXPIREAT {dbPrefix}Key" },
                    { "MIGRATE", $"MIGRATE {dbPrefix}Key" }, // Only present on 1.0.297+
                    { "MOVE", $"MOVE {dbPrefix}Key" },
                    { "PERSIST", $"PERSIST {dbPrefix}Key" },
                    { "RANDOMKEY", $"RANDOMKEY" },
                    { "RENAME", $"RENAME {dbPrefix}Key" },
                    { "RESTORE", $"RESTORE {dbPrefix}Key" },
                    { "PTTL", $"PTTL {dbPrefix}Key" },
                    { "TYPE", $"TYPE {dbPrefix}Key" },
                    { "LINDEX", $"LINDEX {dbPrefix}List" },
                    { "LINSERT", $"LINSERT {dbPrefix}List" },
                    { "LINSERT", $"LINSERT {dbPrefix}List" },
                    { "LPOP", $"LPOP {dbPrefix}List" },
                    { "LPUSH", $"LPUSH {dbPrefix}List" },
                    { "LLEN", $"LLEN {dbPrefix}List" },
                    { "LRANGE", $"LRANGE {dbPrefix}List" },
                    { "LREM", $"LREM {dbPrefix}List" },
                    { "RPOP", $"RPOP {dbPrefix}List" },
                    { "RPOPLPUSH", $"RPOPLPUSH {dbPrefix}List" },
                    { "RPUSH", $"RPUSH {dbPrefix}List" },
                    { "LSET", $"LSET {dbPrefix}List" },
                    { "LTRIM", $"LTRIM {dbPrefix}List" },
                    { "GET", $"GET {dbPrefix}Lock" },
                    { "SET", $"SET {dbPrefix}Lock" },
                    { "PING", $"PING" },
                    { "PUBLISH", $"PUBLISH value" },
                    { "SADD", $"SADD {dbPrefix}Set" },
                    { "SUNION", $"SUNION {dbPrefix}Set" },
                    { "SUNIONSTORE", $"SUNIONSTORE {dbPrefix}Set" },
                    { "SISMEMBER", $"SISMEMBER {dbPrefix}Set" },
                    { "SCARD", $"SCARD {dbPrefix}Set" },
                    { "SMEMBERS", $"SMEMBERS {dbPrefix}Set" },
                    { "SMOVE", $"SMOVE {dbPrefix}Set" },
                    { "SPOP", $"SPOP {dbPrefix}Set" },
                    { "SRANDMEMBER", $"SRANDMEMBER {dbPrefix}Set" },
                    { "SRANDMEMBER", $"SRANDMEMBER {dbPrefix}Set" },
                    { "SREM", $"SREM {dbPrefix}Set" },
                    { "EXEC", $"EXEC" },
                    { "SORT", $"SORT {dbPrefix}Key" }, // Only present on 1.0.206+
                    { "SORT", $"SORT {dbPrefix}Key" }, // Only present on 1.0.206+
                    { "ZUNIONSTORE", $"ZUNIONSTORE {dbPrefix}SortedSet2" }, // Only present on 1.0.206+
                    { "ZADD", $"ZADD {dbPrefix}SortedSet" },
                    { "ZINCRBY", $"ZINCRBY {dbPrefix}SortedSet" },
                    { "ZINCRBY", $"ZINCRBY {dbPrefix}SortedSet" },
                    { "ZCARD", $"ZCARD {dbPrefix}SortedSet" },
                    { "ZRANGE", $"ZRANGE {dbPrefix}SortedSet" },
                    { "ZRANGE", $"ZRANGE {dbPrefix}SortedSet" },
                    { "ZRANGEBYSCORE", $"ZRANGEBYSCORE {dbPrefix}SortedSet" },
                    { "ZRANGEBYSCORE", $"ZRANGEBYSCORE {dbPrefix}SortedSet" },
                    { "ZRANK", $"ZRANK {dbPrefix}SortedSet" },
                    { "ZREM", $"ZREM {dbPrefix}SortedSet" },
                    { "ZREMRANGEBYRANK", $"ZREMRANGEBYRANK {dbPrefix}SortedSet" },
                    { "ZREMRANGEBYSCORE", $"ZREMRANGEBYSCORE {dbPrefix}SortedSet"  },
                    { "ZSCORE", $"ZSCORE {dbPrefix}SortedSet" },
                    { "ZLEXCOUNT", $"ZLEXCOUNT {dbPrefix}SortedSet" }, // Only present on 1.0.273+
                    { "ZRANGEBYLEX", $"ZRANGEBYLEX {dbPrefix}SortedSet" }, // Only present on 1.0.273+
                    { "ZREMRANGEBYLEX", $"ZREMRANGEBYLEX {dbPrefix}SortedSet" }, // Only present on 1.0.273+
                    { "APPEND", $"APPEND {dbPrefix}Key" },
                    { "BITCOUNT", $"BITCOUNT {dbPrefix}Key" },
                    { "BITOP", $"BITOP" },
                    { "BITPOS", $"BITPOS {dbPrefix}Key" },
                    { "INCRBYFLOAT", $"INCRBYFLOAT {dbPrefix}Key" },
                    { "GET", $"GET {dbPrefix}Key" },
                    { "GETBIT", $"GETBIT {dbPrefix}Key" },
                    { "GETRANGE", $"GETRANGE {dbPrefix}Key" },
                    { "GETSET", $"GETSET {dbPrefix}Key" },
                    { "PTTL+GET", $"PTTL+GET {dbPrefix}Key" },
                    { "STRLEN", $"STRLEN {dbPrefix}Key" },
                    { "SET", $"SET {dbPrefix}Key" },
                    { "SETBIT", $"SETBIT {dbPrefix}Key" },
                    { "SETRANGE", $"SETRANGE {dbPrefix}Key" },
                });

                FilterExpectedResultsByApiVersion(expected, packageVersion);

                // No db.statement tags should exists, so overwrite with null
                if (!tagCommands.Equals("true"))
                {
                    var replacement = new TupleList<string, string>();
                    foreach (var item in expected)
                    {
                        replacement.Add(new Tuple<string, string>(item.Item1, null));
                    }

                    expected = replacement;
                }

                var spans = agent.WaitForSpans(expected.Count).Where(s => s.Tags.GetValueOrDefault<string>("db.type") == "redis").OrderBy(s => s.Start).ToList();
                var host = Environment.GetEnvironmentVariable("STACKEXCHANGE_REDIS_HOST") ?? "localhost:6389";
                var port = host.Substring(host.IndexOf(':') + 1);
                host = host.Substring(0, host.IndexOf(':'));

                foreach (var span in spans)
                {
                    Assert.Equal("Samples.StackExchange.Redis", span.Service);
                    Assert.Equal("StackExchange.Redis", span.Tags.GetValueOrDefault<string>("component"));
                    Assert.Equal(SpanKinds.Client, span.Tags.GetValueOrDefault<string>("span.kind"));
                    Assert.Equal(host, span.Tags.GetValueOrDefault<string>("peer.hostname"));
                    Assert.Equal(port, span.Tags.GetValueOrDefault<string>("peer.port"));
                }

                var spanLookup = new Dictionary<Tuple<string, string>, int>();
                foreach (var span in spans)
                {
                    var key = new Tuple<string, string>(span.Name, span.Tags.GetValueOrDefault<string>("db.statement"));
                    if (spanLookup.ContainsKey(key))
                    {
                        spanLookup[key]++;
                    }
                    else
                    {
                        spanLookup[key] = 1;
                    }
                }

                var missing = new List<Tuple<string, string>>();

                foreach (var e in expected)
                {
                    // By default all "db.statement" are sanitized - no worries about truncation because
                    // all tag values here are smaller than the maximum recorded length default.
                    var expectedKey = new Tuple<string, string>(e.Item1, e.Item2?.SanitizeSqlStatement());
                    var found = spanLookup.ContainsKey(expectedKey);
                    if (found)
                    {
                        if (--spanLookup[expectedKey] <= 0)
                        {
                            spanLookup.Remove(e);
                        }
                    }
                    else
                    {
                        missing.Add(expectedKey);
                    }
                }

                foreach (var e in missing)
                {
                    Assert.True(false, $"no span found for `{e.Item1}`, `{e.Item2}`, remaining spans: `{string.Join(", ", spanLookup.Select(kvp => $"{kvp.Key.Item1}, {kvp.Key.Item2}").ToArray())}`");
                }
            }
        }

        private void FilterExpectedResultsByApiVersion(TupleList<string, string> expected, string packageVersion)
        {
            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.2.2") < 0)
            {
                expected.Remove(new Tuple<string, string>("DDCUSTOM", "DDCUSTOM"));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.2.0") < 0)
            {
                expected.RemoveAll(tuple => tuple.Item1.ToUpper().StartsWith("GEO") ||
                    (tuple.Item1.ToUpper().Equals("ZREM") && tuple.Item2.ToUpper().Contains("GEO")));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.0.297") < 0)
            {
                expected.RemoveAll(tuple => tuple.Item1.ToUpper().StartsWith("MIGRATE"));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.0.273") < 0)
            {
                expected.RemoveAll(tuple => tuple.Item1.ToUpper().Contains("LEX") && tuple.Item2.ToUpper().Contains("LEX"));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.0.245") < 0)
            {
                expected.RemoveAll(tuple => tuple.Item1.ToUpper().Equals("PUBLISH"));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.0.242") < 0)
            {
                expected.RemoveAll(tuple => tuple.Item1.ToUpper().StartsWith("PF"));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.0.219") < 0)
            {
                expected.Remove(new Tuple<string, string>("RANDOMKEY", "RANDOMKEY"));
            }

            if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("1.0.206") < 0)
            {
                expected.RemoveAll(tuple => tuple.Item1.ToUpper().Equals("SORT") ||
                    (tuple.Item1.ToUpper().Equals("ZUNIONSTORE")));
            }
        }
    }
}
