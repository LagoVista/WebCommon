﻿using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Configuration
{
    public class AppConfig : IAppConfig
    {
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
    }
}
