using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Web.HealthCheck;

namespace Jumoo.CdfHealthChecker
{
    [HealthCheck("5B4E0688-DD0A-4ED4-B88D-4C145F423510", "Client Dependency Files",
        Description = "Clean the files in the client dependency cache",
        Group = "Client Dependency")]
    public class ClientDependencyFileClear : HealthCheck
    {
        public ClientDependencyFileClear(HealthCheckContext healthCheckContext) : base(healthCheckContext)
        {
        }

        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            if (action.Alias == "Clear")
                return ClearCdfFolder();

            return new HealthCheckStatus("Unknown")
            {
                ResultType = StatusResultType.Error,
                Description = string.Format("Unknown Action {0}", action.Alias)
            };
        }

        public override IEnumerable<HealthCheckStatus> GetStatus()
        {
            List<HealthCheckStatus> status = new List<HealthCheckStatus>();
            status.Add(GetCdfFolder());
            return status;
        }

        private HealthCheckStatus GetCdfFolder()
        {
            var path = ClientDependency.Core.Config.ClientDependencySettings.Instance.DefaultCompositeFileProcessingProvider.CompositeFilePath;

            var files = path.GetFiles();

            var msg = string.Format("CDF Path: {0}", path);
            var status = new HealthCheckStatus(msg)
            {
                ResultType = StatusResultType.Info,
                Description = string.Format("There are currently {1} files in the cdf folder. Clearing these files will refresh both front end and back end scripts and stylesheets", path, files.Count()),
            };

            if (files.Count() > 0)
            {
                status.Actions = new List<HealthCheckAction>()
                {
                    new HealthCheckAction("Clear", this.Id)
                    {
                        Name = "Clear"
                    }
                };
            }

            return status;
        }

        private HealthCheckStatus ClearCdfFolder()
        {
            var path = ClientDependency.Core.Config.ClientDependencySettings.Instance.DefaultCompositeFileProcessingProvider.CompositeFilePath;

            foreach (var file in path.GetFiles())
            {
                file.Delete();
            }

            return new HealthCheckStatus("Folder Cleared");
        }

    }
}
