using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.IO;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Core.Persistence.SqlSyntax;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Jumoo.CdfHealthChecker
{
    [Migration("1.0.0", 1, "CdfHealthCheck")]
    public class ClientDependecyCheckMigration : MigrationBase
    {
        public ClientDependecyCheckMigration(ISqlSyntaxProvider sqlSyntax, ILogger logger) 
            : base(sqlSyntax, logger)
        {
        }

        public override void Up()
        {
            LogHelper.Info<ClientDependecyCheckMigration>("Running v1.0 Up Migration - Add Language Files");
            var tmpLangFolder = IOHelper.MapPath("~/App_Data/TEMP/CdfLangFiles/");
            if (Directory.Exists(tmpLangFolder))
            {
                foreach(var file in Directory.GetFiles(tmpLangFolder, "*.user.xml"))
                {
                    AddTranslation(file);
                }
            }
        }

        public override void Down()
        {
            // TODO: Remove the lang bits. 
        }

        private void AddTranslation(string sourceFile)
        {
            LogHelper.Info<ClientDependecyCheckMigration>("Processing {0} Language", () => sourceFile);

            if (!File.Exists(sourceFile))
                return;

            var sourceDoc = new XmlDocument();
            sourceDoc.Load(sourceFile);

            var fileName = Path.GetFileName(sourceFile);
            var targetFile = IOHelper.MapPath(string.Format("~/Config/Lang/{0}", fileName));

            LogHelper.Info<ClientDependecyCheckMigration>("Looking for {0}", () => targetFile);
            if (File.Exists(targetFile))
            {
                LogHelper.Info<ClientDependecyCheckMigration>(
                    "Source: = {0} Target: {1}", () => sourceFile, () => targetFile);
                try
                {
                    var targetDoc = new XmlDocument();
                    targetDoc.Load(targetFile);

                    var areas = sourceDoc.DocumentElement.SelectNodes("//area");

                    foreach (XmlNode area in areas)
                    {
                        var areaName = area.Attributes["alias"];

                        var targetArea = targetDoc.SelectSingleNode(
                            string.Format("//area [@alias='{0}']", areaName.Value));

                        if (targetArea == null)
                        {
                            // no area - just add what we have to the file.
                            var import = targetDoc.ImportNode(area, true);
                            targetDoc.DocumentElement.AppendChild(import);
                        }
                        else
                        {
                            // we have the ares - we need to write in what we have
                            foreach (XmlNode areaKey in area.ChildNodes)
                            {
                                if (areaKey.NodeType == XmlNodeType.Element)
                                {
                                    var keyToFind = areaKey.Attributes["alias"];

                                    var targetKey = targetArea.SelectSingleNode(
                                        string.Format("./key [@alias='{0}']", keyToFind.Value));

                                    LogHelper.Info<ClientDependecyCheckMigration>("Looking for {0}", () => string.Format("./key [@alias='{0}']", keyToFind.Value));

                                    if (targetKey == null)
                                    {
                                        LogHelper.Info<ClientDependecyCheckMigration>("Adding New");
                                        var keyImport = targetDoc.ImportNode(areaKey, true);
                                        targetArea.AppendChild(keyImport);
                                    }
                                }
                            }
                        }
                    }

                    targetDoc.Save(targetFile);
                }
                catch(Exception ex)
                {
                    LogHelper.Error<ClientDependecyCheckMigration>("Failed to add the language bits to the language files", ex);
                }
            }
        }
    }
}
