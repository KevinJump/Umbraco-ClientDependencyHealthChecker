using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Umbraco.Core.IO;
using Umbraco.Core.Services;
using Umbraco.Web.HealthCheck;

namespace Jumoo.CdfHealthChecker
{
    [HealthCheck("9C042796-9BDB-4C04-AE3A-07191A1274E4", "Client Dependency Cache Age",
        Description = "Check if the client dependency cache is older than you css and js files on disk",
        Group = "Client Dependency")]
    public class ClientDependencyDateCheck : HealthCheck
    {
        private readonly ILocalizedTextService _textService;

        public ClientDependencyDateCheck(HealthCheckContext healthCheckContext) : base(healthCheckContext)
        {
            _textService = healthCheckContext.ApplicationContext.Services.TextService;
        }


        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            switch(action.Alias)
            {
                case "Clear":
                    return ClearCdfFolder();
                case "Increment":
                    return IncrementCdf();
            }

            return new HealthCheckStatus("Unknown")
            {
                ResultType = StatusResultType.Error,
                Description = string.Format("Unknown Action {0}", action.Alias)
            };
        }

        public override IEnumerable<HealthCheckStatus> GetStatus()
        {
            var actions = new List<HealthCheckAction>
                        {
                            new HealthCheckAction("Increment", this.Id)
                            {
                                Name = _textService.Localize("cdfHealthCheck/CmdIncrement")
                            },
                            new HealthCheckAction("Clear", this.Id)
                            {
                                Name = _textService.Localize("cdfHealthCheck/CmdClear")
                            }
                        };

            DateTime cacheTime = DateTime.MaxValue;
            DateTime filesTime = DateTime.MinValue;
            var path = ClientDependency.Core.Config.ClientDependencySettings.Instance.DefaultCompositeFileProcessingProvider.CompositeFilePath;
            var files = path.GetFiles();
            foreach(var file in files)
            {
                if (file.LastWriteTimeUtc < cacheTime)
                    cacheTime = file.LastWriteTimeUtc;
            }

            var folders = new string[] { "~/css", "~/scripts" };

            foreach(var folder in folders)
            {
                var folderPath = IOHelper.MapPath(folder);
                var date = GetLastWriteTime(folderPath);
                if (date > filesTime)
                    filesTime = date;
            }

            if (filesTime > cacheTime)
            {
                var statusTitle = _textService.Localize("cdfHealthCheck/OldCacheTitle");
                return new List<HealthCheckStatus>() {
                    new HealthCheckStatus(statusTitle)
                    {
                        ResultType = StatusResultType.Warning,
                        Description =
                        _textService.Localize("cdfHealthCheck/OldCacheMessage",
                                new [] { cacheTime.ToString("dd-MMM-yyyy HH:mm.ss"), filesTime.ToString("dd-MMM-yyyy HH:mm.ss")}),
                        Actions = actions
                    }
                };
            }

            var statusName = _textService.Localize("cdfHealthCheck/CacheUptoDateTitle");
            return new List<HealthCheckStatus>()
            {
                new HealthCheckStatus(statusName)
                {
                    ResultType = StatusResultType.Success,
                    Description = _textService.Localize("cdfHealthCheck/CacheUptoDateMessage"),
                    Actions = actions
                }
            };
        }

        private DateTime GetLastWriteTime(string folder)
        {
            if (!Directory.Exists(folder))
                return DateTime.MinValue;

            var lastWrite = DateTime.MinValue;

            var dirInfo = new DirectoryInfo(folder);
            foreach(var file in dirInfo.GetFiles())
            {
                if (file.LastWriteTimeUtc > lastWrite)
                    lastWrite = file.LastWriteTimeUtc;
            }

            return lastWrite;
        }

        private HealthCheckStatus ClearCdfFolder()
        {
            var path = ClientDependency.Core.Config.ClientDependencySettings.Instance.DefaultCompositeFileProcessingProvider.CompositeFilePath;

            foreach (var file in path.GetFiles())
            {
                file.Delete();
            }

            return new HealthCheckStatus(_textService.Localize("cdfHealthCheck/FolderCleared"));
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
                if (int.TryParse(node.Value, out version))
                {
                    node.Value = (version + 1).ToString();
                }

                xmlDocument.Save(path);

                return new HealthCheckStatus(
                    _textService.Localize("cdfHealthCheck/CacheIncrements",
                       new[] { (version + 1).ToString() }));
            }

            return new HealthCheckStatus("Failed")
            {
                ResultType = StatusResultType.Error
            };
        }


    }
}
