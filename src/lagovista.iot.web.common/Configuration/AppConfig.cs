// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 34d2fec38312efc347ce02567f88a119796a7931c52ff5d0e673b9853a5aa809
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Configuration
{
    public class AppConfig : IAppConfig
    {
        public AppConfig(IConfiguration configuration)
        {
            var systemOwnerSection = configuration.GetSection("SystemOwnerOrg");
            SystemOwnerOrg = new Core.Models.EntityHeader()
            {
                Id = systemOwnerSection.Require("Id"),
                Text = systemOwnerSection.Require("Text")
            };

            var gaSection = configuration.GetSection("GA");
            AnalyticsKey = gaSection.Require("ID");

            var environmentName = configuration.Require("Environment");
            SlotTitle = configuration.Require("SlotTitle");

            if (string.Equals(environmentName, Core.Interfaces.Environments.Development.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Environment = SlotTitle == "LocalDev"
                    ? Core.Interfaces.Environments.Local
                    : Core.Interfaces.Environments.Development;
            }
            else if (string.Equals(environmentName, Core.Interfaces.Environments.Staging.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Environment = Core.Interfaces.Environments.Staging;
            }
            else if (string.Equals(environmentName, Core.Interfaces.Environments.Production.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Environment = Core.Interfaces.Environments.Production;
            }
            else if (string.Equals(environmentName, Core.Interfaces.Environments.Testing.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Environment = Core.Interfaces.Environments.Testing;
            }
            else if (string.Equals(environmentName, Core.Interfaces.Environments.Beta.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Environment = Core.Interfaces.Environments.Beta;
            }
            else
            {
                Environment = Core.Interfaces.Environments.Local;
            }

            AppName = configuration.Optional("AppName", "LagoVista WebHost");

            var isSSL = configuration.Require("IsSSL");
            IsSSL = String.IsNullOrEmpty(isSSL) ? true : String.Equals(isSSL, "true", StringComparison.OrdinalIgnoreCase);

            var hostName = configuration.Require("HostName");
         

            WebAddress = (IsSSL ? "https://" : "http://") + hostName;
            
        }

        public string AppLogo
        {
            get { return "http://bytemaster.blob.core.windows.net/icons/AppLogo.png"; }
        }

        public String AppName { get; set; }

        public string CompanyLogo
        {
            get { return "http://bytemaster.blob.core.windows.net/icons/CompanyLogo.png"; }
        }

        public Environments Environment
        {
            get; set;
        }

        public bool EmitTestingCode
        {
            get { return Environment == Environments.Local || Environment == Environments.Testing; }
        }

        public PlatformTypes PlatformType
        {
            get; set;
        }

        public string WebAddress { get; set; }

        public bool IsSSL { get; set; }

        public String SlotTitle { get; set; }

        public string AppId { get; set; }

        public string APIToken { get; set; }

        public string InstallationId { get; set; }

        public string ClientType { get; set; }

        public VersionInfo Version { get; set; }

        public string AppDescription { get; set; }

        public string TermsAndConditionsLink { get; set; }

        public string PrivacyStatementLink { get; set; }

        public string CompanyName { get; set; }

        public string CompanySiteLink { get; set; }

        public AuthTypes AuthType => AuthTypes.User;

        public string InstanceId { get; set; }
        public string InstanceAuthKey { get; set; }
        public string DeviceId { get; set; }
        public string DeviceRepoId { get; set; }

        public EntityHeader SystemOwnerOrg { get; set; }

        public string DefaultDeviceLabel => "Device";

        public string DefaultDeviceLabelPlural => "Devices";

        public string AnalyticsKey { get; set; }
    }
}
