using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Modules;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers;

internal abstract class SqlServerBase : IAsyncDisposable
{
    protected const int Port = 1433;
    protected const string ImageName = "mcr.microsoft.com/mssql/server:2019-CU16-ubuntu-20.04";
    protected readonly Stream Stream;

    protected SqlServerBase(ResultsNamingConvention resultsNamingConvention)
    {
        if (resultsNamingConvention == null) throw new ArgumentNullException(nameof(resultsNamingConvention));
        Stream = File.Create(Path.Combine(resultsNamingConvention.ContainerLogs, "sqlserver.txt"));
    }

    protected abstract string Address { get; }
    protected static string TestPassword { get; } = $"@{Guid.NewGuid():N}a";

    internal string CatalogConnection =>
        $"Server={Address},{Port};Integrated Security=true;Initial Catalog=Microsoft.eShopOnWeb.CatalogDb;User Id=sa;Password={TestPassword};Trusted_Connection=false;";

    internal string IdentityConnection =>
        $"Server={Address},{Port};Integrated Security=true;Initial Catalog=Microsoft.eShopOnWeb.Identity;User Id=sa;Password={TestPassword};Trusted_Connection=false;";

    protected abstract TestcontainersContainer Container { get; }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
        await Stream.DisposeAsync();
    }

    internal Task StartAsync() => Container.StartAsync();
}
