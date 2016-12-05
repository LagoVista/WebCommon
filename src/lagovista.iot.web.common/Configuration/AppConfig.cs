using LagoVista.UserManagement.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoI.Web.Common.Configuration
{
    public class AppConfig
    {
        public string AppLogo
        {
            get { return "http://bytemaster.blob.core.windows.net/icons/logo-wide.png"; }
        }

        public String AppName { get; set; }

        public string CompanyLogo
        {
            get { return "http://bytemaster.blob.core.windows.net/icons/logo-wide.png"; }
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
    }
}
