using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.Migrations;

namespace Jumoo.CdfHealthChecker
{
    public class CdfHealthCheckMigrationEventHandler 
        : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {            
            HandleMigration(applicationContext);
        }

        private void HandleMigration(ApplicationContext applicationContext)
        {
            const string productName = "CdfHealthCheck";
            var targetVersion = new SemVersion(1, 0, 0);

            var currentVersion = new SemVersion(0, 0, 0);

            var migrations = applicationContext.Services.MigrationEntryService.GetAll(productName);

            var latest = migrations.OrderByDescending(x => x.Version).FirstOrDefault();

            if (latest != null)
                currentVersion = latest.Version;

            if (targetVersion == currentVersion)
                return;

            var runner = new MigrationRunner(
                applicationContext.Services.MigrationEntryService,
                applicationContext.ProfilingLogger.Logger,
                currentVersion,
                targetVersion,
                productName);

            try
            {
                runner.Execute(applicationContext.DatabaseContext.Database);
            }
            catch (Exception e)
            {
                LogHelper.Error<CdfHealthCheckMigrationEventHandler>("Error running migration", e);
            }
        }
    }
}
