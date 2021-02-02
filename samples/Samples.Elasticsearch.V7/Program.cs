// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;

namespace Samples.Elasticsearch.V7
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = new Uri("http://" + Host());
            var settings = new ConnectionSettings(host).DefaultIndex("elastic-net-example");
            var elastic = new ElasticClient(settings);

            var commands = new List<Func<object>>()
                          .Concat(IndexCommands(elastic))
                          .Concat(IndexCommandsAsync(elastic))
                          .Concat(DocumentCommands(elastic))
                          .Concat(DocumentCommandsAsync(elastic))
                          .Concat(CatCommands(elastic))
                          .Concat(CatCommandsAsync(elastic))
                          .Concat(JobCommands(elastic))
                          .Concat(JobCommandsAsync(elastic))
                          .Concat(ClusterCommands(elastic))
                          .Concat(ClusterCommandsAsync(elastic))
                          .Concat(UserCommands(elastic))
                          .Concat(UserCommandsAsync(elastic))
                          .Concat(WatchCommands(elastic)).ToList();

            Console.WriteLine(commands.Count);

            var exceptions = new List<Exception>();

            foreach (var action in commands)
            {
                try
                {
                    var result = action();
                    if (result is Task task)
                    {
                        result = TaskResult(task);
                    }

                    Console.WriteLine($"{result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions).Flatten();
            }
        }

        private static string Host()
        {
            return Environment.GetEnvironmentVariable("ELASTICSEARCH7_HOST") ?? "localhost:9210";
        }

        private static IEnumerable<Func<object>> DocumentCommands(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Bulk(
                    new BulkRequest("test_index")
                    {
                        Operations = new List<IBulkOperation>
                        {
                            new BulkCreateOperation<Post>(new Post { Id = 1, Title = "BulkCreateOperation" })
                        }
                    }),
                () => elastic.Create(new CreateRequest<Post>(new Post { Id = 2, Title = "CreateRequest" }, "test_index")),
                () => elastic.CreateDocument(new Post { Id = 3, Title = "CreateDocument" }),
                () => elastic.Count<Post>(),
                () => elastic.Search<Post>(s => s.MatchAll()),
                () => elastic.DeleteByQuery(new DeleteByQueryRequest("test_index") { Size = 0 })
            };
        }

        private static IEnumerable<Func<object>> DocumentCommandsAsync(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.BulkAsync(
                    new BulkRequest("test_index")
                    {
                        Operations = new List<IBulkOperation>
                        {
                            new BulkCreateOperation<Post>(new Post { Id = 1, Title = "BulkCreateOperation" })
                        }
                    }),
                () => elastic.CreateAsync(new CreateRequest<Post>(new Post { Id = 2, Title = "CreateRequest" }, "test_index")),
                () => elastic.CreateDocumentAsync(new Post { Id = 3, Title = "CreateDocument" }),
                () => elastic.CountAsync<Post>(),
                () => elastic.SearchAsync<Post>(s => s.MatchAll()),
                () => elastic.DeleteByQueryAsync(new DeleteByQueryRequest("test_index") { Size = 0 })
            };
        }

        private static IEnumerable<Func<object>> IndexCommands(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Indices.Create("test_index_1"),
                () => elastic.Indices.Exists("test_index_1"),
                () => elastic.Indices.UpdateSettings(
                    new UpdateIndexSettingsRequest("test_index_1")
                    {
                        IndexSettings = new IndexSettings
                        {
                            Sorting = new SortingSettings { Fields = new Field("Title") }
                        }
                    }),
                () => elastic.Indices.BulkAlias(
                    new BulkAliasRequest
                    {
                        Actions = new List<IAliasAction>
                        {
                            new AliasAddAction
                            {
                                Add = new AliasAddOperation
                                {
                                    Index = "test_index_1", Alias = "test_index_2"
                                }
                            }
                        }
                    }),
                () => elastic.GetAliasesPointingToIndex("test_index_1"),
                () => elastic.Indices.PutAlias("test_index_1", "test_index_3"),
                () => elastic.Indices.AliasExists(new AliasExistsRequest("test_index_1")),
                () => elastic.Indices.DeleteAlias(new DeleteAliasRequest("test_index_1", "test_index_3")),
                () => elastic.Indices.DeleteAlias(new DeleteAliasRequest("test_index_1", "test_index_2")),
                () => elastic.Indices.Create("test_index_4"),
                () => elastic.Indices.Split("test_index_1", "test_index_4"),
                () => elastic.Indices.Delete("test_index_4"),
                () => elastic.Indices.Close("test_index_1"),
                () => elastic.Indices.Open("test_index_1"),
                () => elastic.Indices.PutTemplate(new PutIndexTemplateRequest("test_template_1")),
                () => elastic.Indices.TemplateExists("test_template_1"),
                () => elastic.Indices.DeleteTemplate("test_template_1"),
                () => elastic.Indices.ShardStores(),
                () => elastic.Indices.Stats("test_index_1"),
                () => elastic.Indices.Delete("test_index_1"),
                () => elastic.Indices.GetAlias(new GetAliasRequest()),
                () => elastic.ReindexOnServer(
                    new ReindexOnServerRequest
                    {
                        Source = new ReindexSource
                        {
                            Remote = new RemoteSource
                            {
                                Host = new Uri("http://" + Host()),
                                Username = "test_user_1",
                                Password = "supersecret"
                            },
                            Index = "some_index",
                            Size = 10
                        },
                        Destination = new ReindexDestination { Index = "some_index_clone" }
                    })
            };
        }

        private static IEnumerable<Func<object>> IndexCommandsAsync(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Indices.CreateAsync("test_index_1"),
                () => elastic.Indices.ExistsAsync("test_index_1"),
                () => elastic.Indices.UpdateSettingsAsync(
                    new UpdateIndexSettingsRequest("test_index_1")
                    {
                        IndexSettings = new IndexSettings
                        {
                            Sorting = new SortingSettings { Fields = new Field("Title") }
                        }
                    }),
                () => elastic.Indices.BulkAliasAsync(
                    new BulkAliasRequest
                    {
                        Actions = new List<IAliasAction>
                        {
                            new AliasAddAction
                            {
                                Add = new AliasAddOperation
                                {
                                    Index = "test_index_1", Alias = "test_index_2"
                                }
                            }
                        }
                    }),
                () => elastic.GetAliasesPointingToIndexAsync("test_index_1"),
                () => elastic.Indices.PutAliasAsync("test_index_1", "test_index_3"),
                () => elastic.Indices.AliasExistsAsync(new AliasExistsRequest("test_index_1")),
                () => elastic.Indices.DeleteAliasAsync(new DeleteAliasRequest("test_index_1", "test_index_3")),
                () => elastic.Indices.DeleteAliasAsync(new DeleteAliasRequest("test_index_1", "test_index_2")),
                () => elastic.Indices.CreateAsync("test_index_4"),
                () => elastic.Indices.Split("test_index_1", "test_index_4"),
                () => elastic.Indices.DeleteAsync("test_index_4"),
                () => elastic.Indices.CloseAsync("test_index_1"),
                () => elastic.Indices.OpenAsync("test_index_1"),
                () => elastic.Indices.PutTemplateAsync(new PutIndexTemplateRequest("test_template_1")),
                () => elastic.Indices.TemplateExistsAsync("test_template_1"),
                () => elastic.Indices.DeleteTemplateAsync("test_template_1"),
                () => elastic.Indices.ShardStoresAsync(),
                () => elastic.Indices.StatsAsync("test_index_1"),
                () => elastic.Indices.DeleteAsync("test_index_1"),
                () => elastic.Indices.GetAliasAsync(new GetAliasRequest()),
                () => elastic.ReindexOnServerAsync(
                    new ReindexOnServerRequest
                    {
                        Source = new ReindexSource
                        {
                            Remote = new RemoteSource
                            {
                                Host = new Uri("http://" + Host()),
                                Username = "test_user_1",
                                Password = "supersecret"
                            },
                            Index = "some_index",
                            Size = 10
                        },
                        Destination = new ReindexDestination { Index = "some_index_clone" }
                    })
            };
        }

        private static IEnumerable<Func<object>> CatCommands(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Cat.Aliases(new CatAliasesRequest()),
                () => elastic.Cat.Allocation(new CatAllocationRequest()),
                () => elastic.Cat.Count(new CatCountRequest()),
                () => elastic.Cat.Fielddata(new CatFielddataRequest()),
                () => elastic.Cat.Health(new CatHealthRequest()),
                () => elastic.Cat.Help(new CatHelpRequest()),
                () => elastic.Cat.Indices(new CatIndicesRequest()),
                () => elastic.Cat.Master(new CatMasterRequest()),
                () => elastic.Cat.NodeAttributes(new CatNodeAttributesRequest()),
                () => elastic.Cat.Nodes(new CatNodesRequest()),
                () => elastic.Cat.PendingTasks(new CatPendingTasksRequest()),
                () => elastic.Cat.Plugins(new CatPluginsRequest()),
                () => elastic.Cat.Recovery(new CatRecoveryRequest()),
                () => elastic.Cat.Repositories(new CatRepositoriesRequest()),
                () => elastic.Cat.Segments(new CatSegmentsRequest()),
                () => elastic.Cat.Shards(new CatShardsRequest()),
                // CatSnapshots is failing with a JSON deserialization error.
                // It might be a bug in the client or an incompatibility between client
                // and server versions.
                () => elastic.Cat.Snapshots(new CatSnapshotsRequest()),
                () => elastic.Cat.Tasks(new CatTasksRequest()),
                () => elastic.Cat.Templates(new CatTemplatesRequest()),
                () => elastic.Cat.ThreadPool(new CatThreadPoolRequest())
            };
        }

        private static IEnumerable<Func<object>> CatCommandsAsync(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Cat.AliasesAsync(new CatAliasesRequest()),
                () => elastic.Cat.AllocationAsync(new CatAllocationRequest()),
                () => elastic.Cat.CountAsync(new CatCountRequest()),
                () => elastic.Cat.FielddataAsync(new CatFielddataRequest()),
                () => elastic.Cat.HealthAsync(new CatHealthRequest()),
                () => elastic.Cat.HelpAsync(new CatHelpRequest()),
                () => elastic.Cat.IndicesAsync(new CatIndicesRequest()),
                () => elastic.Cat.MasterAsync(new CatMasterRequest()),
                () => elastic.Cat.NodeAttributesAsync(new CatNodeAttributesRequest()),
                () => elastic.Cat.NodesAsync(new CatNodesRequest()),
                () => elastic.Cat.PendingTasksAsync(new CatPendingTasksRequest()),
                () => elastic.Cat.PluginsAsync(new CatPluginsRequest()),
                () => elastic.Cat.RecoveryAsync(new CatRecoveryRequest()),
                () => elastic.Cat.RepositoriesAsync(new CatRepositoriesRequest()),
                () => elastic.Cat.SegmentsAsync(new CatSegmentsRequest()),
                () => elastic.Cat.ShardsAsync(new CatShardsRequest()),
                // CatSnapshots is failing with a JSON deserialization error.
                // It might be a bug in the client or an incompatibility between client
                // and server versions.
                () => elastic.Cat.SnapshotsAsync(new CatSnapshotsRequest()),
                () => elastic.Cat.TasksAsync(new CatTasksRequest()),
                () => elastic.Cat.TemplatesAsync(new CatTemplatesRequest()),
                () => elastic.Cat.ThreadPoolAsync(new CatThreadPoolRequest())
            };
        }

        private static IEnumerable<Func<object>> JobCommands(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.MachineLearning.PutJob(new PutJobRequest("test_job")),
                () => elastic.MachineLearning.ValidateJob(new ValidateJobRequest()),
                () => elastic.MachineLearning.GetInfluencers(new GetInfluencersRequest("test_job")),
                () => elastic.MachineLearning.GetJobs(new GetJobsRequest("test_job")),
                () => elastic.MachineLearning.GetJobStats(new GetJobStatsRequest()),
                () => elastic.MachineLearning.GetModelSnapshots(new GetModelSnapshotsRequest("test_job")),
                () => elastic.MachineLearning.FlushJob(new FlushJobRequest("test_job")),
                () => elastic.MachineLearning.GetOverallBuckets(new GetOverallBucketsRequest("test_job")),
                () => elastic.MachineLearning.ForecastJob(new ForecastJobRequest("test_job")),
                () => elastic.MachineLearning.GetAnomalyRecords(new GetAnomalyRecordsRequest("test_job")),
                () => elastic.MachineLearning.GetBuckets(new GetBucketsRequest("test_job")),
                () => elastic.MachineLearning.GetCategories(new GetCategoriesRequest("test_job")),
                () => elastic.MachineLearning.CloseJob(new CloseJobRequest("test_job")),
                () => elastic.MachineLearning.OpenJob(new OpenJobRequest("test_job")),
                () => elastic.MachineLearning.DeleteJob(new DeleteJobRequest("test_job"))
            };
        }

        private static IEnumerable<Func<object>> JobCommandsAsync(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.MachineLearning.PutJobAsync(new PutJobRequest("test_job")),
                () => elastic.MachineLearning.ValidateJobAsync(new ValidateJobRequest()),
                () => elastic.MachineLearning.GetInfluencersAsync(new GetInfluencersRequest("test_job")),
                () => elastic.MachineLearning.GetJobsAsync(new GetJobsRequest("test_job")),
                () => elastic.MachineLearning.GetJobStatsAsync(new GetJobStatsRequest()),
                () => elastic.MachineLearning.GetModelSnapshotsAsync(new GetModelSnapshotsRequest("test_job")),
                () => elastic.MachineLearning.FlushJobAsync(new FlushJobRequest("test_job")),
                () => elastic.MachineLearning.GetOverallBucketsAsync(new GetOverallBucketsRequest("test_job")),
                () => elastic.MachineLearning.ForecastJobAsync(new ForecastJobRequest("test_job")),
                () => elastic.MachineLearning.GetAnomalyRecordsAsync(new GetAnomalyRecordsRequest("test_job")),
                () => elastic.MachineLearning.GetBucketsAsync(new GetBucketsRequest("test_job")),
                () => elastic.MachineLearning.GetCategoriesAsync(new GetCategoriesRequest("test_job")),
                () => elastic.MachineLearning.CloseJobAsync(new CloseJobRequest("test_job")),
                () => elastic.MachineLearning.OpenJobAsync(new OpenJobRequest("test_job")),
                () => elastic.MachineLearning.DeleteJobAsync(new DeleteJobRequest("test_job"))
            };
        }

        private static IEnumerable<Func<object>> ClusterCommands(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Cluster.AllocationExplain(new ClusterAllocationExplainRequest()),
                () => elastic.Cluster.GetSettings(new ClusterGetSettingsRequest()),
                () => elastic.Cluster.Health(new ClusterHealthRequest()),
                () => elastic.Cluster.PendingTasks(new ClusterPendingTasksRequest()),
                () => elastic.Cluster.PutSettings(new ClusterPutSettingsRequest()),
                () => elastic.Cluster.Reroute(new ClusterRerouteRequest()),
                () => elastic.Cluster.State(new ClusterStateRequest()),
                () => elastic.Cluster.Stats(new ClusterStatsRequest())
            };
        }

        private static IEnumerable<Func<object>> ClusterCommandsAsync(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Cluster.AllocationExplainAsync(new ClusterAllocationExplainRequest()),
                () => elastic.Cluster.GetSettingsAsync(new ClusterGetSettingsRequest()),
                () => elastic.Cluster.HealthAsync(new ClusterHealthRequest()),
                () => elastic.Cluster.PendingTasksAsync(new ClusterPendingTasksRequest()),
                () => elastic.Cluster.PutSettingsAsync(new ClusterPutSettingsRequest()),
                () => elastic.Cluster.RerouteAsync(new ClusterRerouteRequest()),
                () => elastic.Cluster.StateAsync(new ClusterStateRequest()),
                () => elastic.Cluster.StatsAsync(new ClusterStatsRequest())
            };
        }

        private static IEnumerable<Func<object>> UserCommands(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Security.PutRole(new PutRoleRequest("test_role_1")),
                () => elastic.Security.PutRoleMapping(new PutRoleMappingRequest("test_role_1")),
                () => elastic.Security.GetRole(new GetRoleRequest("test_role_1")),
                () => elastic.Security.GetRoleMapping(new GetRoleMappingRequest("test_role_1")),
                () => elastic.Security.DeleteRoleMapping("test_role_1"),
                () => elastic.Security.DeleteRole("test_role_1"),
                () => elastic.Security.PutUser(new PutUserRequest("test_user_1")),
                () => elastic.Security.ChangePassword(new ChangePasswordRequest("test_user_1") { Password = "supersecret" }),
                () => elastic.Security.GetUser(new GetUserRequest("test_user_1")),
                () => elastic.Security.DisableUser("test_user_1"),
                () => elastic.Security.DeleteUser("test_user_1")
            };
        }

        private static IEnumerable<Func<object>> UserCommandsAsync(IElasticClient elastic)
        {
            return new List<Func<object>>
            {
                () => elastic.Security.PutRoleAsync(new PutRoleRequest("test_role_1")),
                () => elastic.Security.PutRoleMappingAsync(new PutRoleMappingRequest("test_role_1")),
                () => elastic.Security.GetRoleAsync(new GetRoleRequest("test_role_1")),
                () => elastic.Security.GetRoleMappingAsync(new GetRoleMappingRequest("test_role_1")),
                () => elastic.Security.DeleteRoleMappingAsync("test_role_1"),
                () => elastic.Security.DeleteRoleAsync("test_role_1"),
                () => elastic.Security.PutUserAsync(new PutUserRequest("test_user_1")),
                () => elastic.Security.ChangePasswordAsync(new ChangePasswordRequest("test_user_1") { Password = "supersecret" }),
                () => elastic.Security.GetUserAsync(new GetUserRequest("test_user_1")),
                () => elastic.Security.DisableUserAsync("test_user_1"),
                () => elastic.Security.DeleteUserAsync("test_user_1")
            };
        }

        private static IEnumerable<Func<object>> WatchCommands(IElasticClient elastic)
        {
            return new List<Func<object>>();
        }

        private static object TaskResult(Task task)
        {
            task.Wait();
            var taskType = task.GetType();

            var isTaskOfT =
                taskType.IsGenericType
             && taskType.GetGenericTypeDefinition() == typeof(Task<>);


            return isTaskOfT ? taskType.GetProperty("Result")?.GetValue(task) : null;
        }

        private class Post
        {
            public int Id { get; set; }
            public string Title { get; set; }
        }
    }
}
