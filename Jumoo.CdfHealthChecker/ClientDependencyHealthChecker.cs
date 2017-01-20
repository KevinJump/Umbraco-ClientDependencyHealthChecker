using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.IO;
using Umbraco.Web.HealthCheck;
using ClientDependency.Core;
using System.IO;
using System.Configuration;
using System.Xml.Linq;
using System.Xml;

namespace Jumoo.CdfHealthChecker
{
    [HealthCheck("B806521A-C82C-4135-86EC-7E3CBC195FD3", "Client Dependency Version Number",
        Description = "Change the Client Dependency framwork version number",
        Group = "Client Dependency")]
    public class ClientDependencyHealthChecker : HealthCheck
    {
        public ClientDependencyHealthChecker(HealthCheckContext healthCheckContext) : base(healthCheckContext)
        {
            
        }

        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            if (action.Alias == "Increment")
                return IncrementCdf();

            return new HealthCheckStatus("Unknown")
            {
                ResultType = StatusResultType.Error,
                Description = string.Format("Unknown Action {0}", action.Alias)
            };
        }

        public override IEnumerable<HealthCheckStatus> GetStatus()
        {
            List<HealthCheckStatus> status = new List<HealthCheckStatus>();

            status.Add(GetCdfVersion());

            return status;
        }

        private HealthCheckStatus GetCdfVersion()
        {
            var path = IOHelper.MapPath("~/config/clientdependency.config");
            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.Load(path);

            var node = xmlDocument.SelectSingleNode("//clientDependency/@version");
            if (node != null)
            {
                int version;

                if (int.TryParse(node.Value, out version))
                {
                    var message = string.Format("CDF Version: {0}", version);

                    var status = new HealthCheckStatus(message)
                    {
                        ResultType = StatusResultType.Info,
                        Description = string.Format("The client dependency framework is running on version {0}, increment this number to generate a new set of cached files", version),
                        Actions = new List<HealthCheckAction>() {
                            new HealthCheckAction("Increment", this.Id)
                            {
                                Name = "Increment"
                            }
                        }
                    };
                    return status;
                }
            }

            return new HealthCheckStatus("Failed to load cdf version");
        }

        private HealthCheckStatus IncrementCdf()
        {
            var path = IOHelper.MapPath("~/config/clientdependency.config");
            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.Load(path);

            var node = xmlDocument.SelectSingleNode("//clientDependency/@version");
            if (node != null)
            {
                int version;
                if (int.TryParse(node.Value, out version) )
                { 
                    node.Value = (version + 1).ToString();
                }

                xmlDocument.Save(path);

                return new HealthCheckStatus(string.Format("Incremented to {0}", version + 1));
            }

            return new HealthCheckStatus("Failed")
            {
                ResultType = StatusResultType.Error
            };
        }

    }
}
