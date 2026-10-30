using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Kepnora.Data;
using Volo.Abp.DependencyInjection;

namespace Kepnora.EntityFrameworkCore;

public class EntityFrameworkCoreKepnoraDbSchemaMigrator
    : IKepnoraDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreKepnoraDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the KepnoraDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<KepnoraDbContext>()
            .Database
            .MigrateAsync();
    }
}
