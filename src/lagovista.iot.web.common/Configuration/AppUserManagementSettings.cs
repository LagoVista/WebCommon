using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Configuration
{
  /*  public class AppUserManagementSettings : IAppUserManagementSettings
    {

        public AppUserManagementSettings(IConfigurationRoot configuration)
        {
            bool shouldConsolidate = true;
            bool.TryParse(configuration["ConsolidateCollections"], out shouldConsolidate);
            ShouldConsolidateCollections = shouldConsolidate;


            var tokenConfigSection = configuration.GetSection("Tokens");
            var refreshTokenTime = tokenConfigSection["RefreshTokenExpires"];
            RefreshTokenExpiresTimeSpan = TimeSpan.FromSeconds(String.IsNullOrEmpty(refreshTokenTime) ? Convert.ToInt32(refreshTokenTime) : 12600);
            var accessTokenTime = tokenConfigSection["AccessTokenExpires"];
            AccessTokenExpiresTimeSpan = TimeSpan.FromSeconds(String.IsNullOrEmpty(refreshTokenTime) ? Convert.ToInt32(refreshTokenTime) : 12600);
            var authTokenTime = tokenConfigSection["accessTokenTime"];


            var smtp = configuration.GetSection("Smtp");
            SmtpServer = new ConnectionSettings()
            {
                Uri = smtp["Server"],
                UserName = smtp["UserName"],
                Password = smtp["Password"]
            };

            SmtpFrom = smtp["FromAddress"];

            var sms = configuration.GetSection("Sms");

            SmsServer = new ConnectionSettings()
            {
                AccountId = sms["TwillioAccountId"],
                AccessKey = sms["TWillioAuthToken"]
            };

            FromPhoneNumber = sms["TwillioOutgoingNumber"];

            var userStorage = configuration.GetSection("UserStorage");

            UserStorage = new ConnectionSettings()
            {
                Uri = userStorage["EndPoint"],
                AccessKey = userStorage["AccessKey"],
                ResourceName = userStorage["DbName"]
            };

            var userTableStorage = configuration.GetSection("userTableStorage");


            UserTableStorage = new ConnectionSettings()
            {
                AccessKey = userTableStorage["AccessKey"],
                AccountId = userTableStorage["Name"]
            };
        }

        public TimeSpan AccessTokenExpiresTimeSpan { get; private set; }

        public TimeSpan RefreshTokenExpiresTimeSpan { get; private set; }

        public IConnectionSettings UserStorage { get; set; }

        public IConnectionSettings SmsServer { get; set; }

        public IConnectionSettings SmtpServer { get; set; }

        public string SmtpFrom { get; private set; }


        public string FromPhoneNumber { get; private set; }

        public IConnectionSettings UserTableStorage { get; private set; }

        public bool ShouldConsolidateCollections
        {
            get; private set;
        }
    }*/
}
