using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web.Profile;
using Raven.Client.Document;

namespace Raven.Client.AspNetProviders
{
    public class RavenProfileProvider : ProfileProvider
    {
        private IDocumentStore _documentStore;
        private string _providerName = "RavenProfileProvider";

        public override string ApplicationName { get; set; }

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _providerName = name;

            base.Initialize(name, config);

            SetConfigurationProperties(config);

            if (_documentStore == null)
            {
                if (string.IsNullOrEmpty(config["connectionStringName"]))
                    throw new ConfigurationErrorsException("Must supply a connectionStringName.");

                _documentStore = new DocumentStore
                {
                    ConnectionStringName = config["connectionStringName"],
                };

                _documentStore.Initialize();
            }
        }

        private void SetConfigurationProperties(NameValueCollection config)
        {
            ApplicationName = string.IsNullOrEmpty(config["applicationName"]) ? System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath : config["applicationName"];
        }

        public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            using (var session = _documentStore.OpenSession())
            {
                
            }
            throw new NotImplementedException();
        }

        public override int DeleteProfiles(string[] usernames)
        {
            throw new NotImplementedException();
        }

        public override int DeleteProfiles(ProfileInfoCollection profiles)
        {
            var userNames = from ProfileInfo p in profiles
                            select p.UserName;

            return DeleteProfiles(userNames.ToArray());
        }

        public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            throw new NotImplementedException();
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            throw new NotImplementedException();
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            throw new NotImplementedException();
        }
    }
}
