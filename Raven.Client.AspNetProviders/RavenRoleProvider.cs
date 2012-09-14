using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Web.Security;
using Raven.Abstractions.Data;
using Raven.Client.AspNetProviders.Indexes;
using Raven.Client.Document;
using Raven.Client.Linq;

namespace Raven.Client.AspNetProviders
{
    public class RavenRoleProvider : RoleProvider
    {
        private IDocumentStore _documentStore;
        private string _providerName = "RavenRoleProvider";

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

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            using (var session = _documentStore.OpenSession())
            {
                var users = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Where(x => x.ApplicationName == ApplicationName && x.Username.In(usernames));

                foreach (var user in users)
                {
                    foreach (var roleName in roleNames.Where(roleName => !user.Roles.Contains(roleName)))
                    {
                        user.Roles.Add(roleName);
                    }
                }

                session.SaveChanges();
            }
        }

        public override void CreateRole(string roleName)
        {
            using (var session = _documentStore.OpenSession())
            {
                var app = session.Query<Application>().SingleOrDefault(x => x.Name == ApplicationName);
                if (app != null && !app.Roles.Contains(roleName))
                {
                    app.Roles.Add(roleName);
                }
                else
                {
                    var newApp = new Application
                    {
                        Name = ApplicationName,
                        Roles = new List<string>{ roleName }
                    };
                    session.Store(newApp);
                }
                session.SaveChanges();
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            if (throwOnPopulatedRole && GetUsersInRole(roleName).Length > 0)
            {
                throw new ProviderException("Cannot delete a populated role.");
            }

            _documentStore.DatabaseCommands.UpdateByIndex("Users/ByApplicationNameAndRoleName",
                new IndexQuery{ Query = string.Format("ApplicationName:{0} AND RoleName:{1} ", ApplicationName, roleName) }, 
                new[] 
                {
                    new PatchRequest()
                    {
                        Type = PatchCommandType.Remove,
                        Name = "Roles",
                        Value = roleName
                    }
                });

            using (var session = _documentStore.OpenSession())
            {
                var app = session.Query<Application>().SingleOrDefault(x => x.Name == ApplicationName);
                if (app == null)
                {
                    return false;
                }

                app.Roles.Remove(roleName);
                session.SaveChanges();
            }
            return true;
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            using (var session = _documentStore.OpenSession())
            {
                var users = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Where(x => x.ApplicationName == ApplicationName && x.Roles.Contains(roleName))
                    .Search(x => x.Username, usernameToMatch)
                    .Select(x => x.Username);

                return users.ToArray();
            }
        }

        public override string[] GetAllRoles()
        {
            using (var session = _documentStore.OpenSession())
            {
                var app = session.Query<Application>().SingleOrDefault(x => x.Name == ApplicationName);
                return app == null ? new string[] { } : app.Roles.ToArray();
            }
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var session = _documentStore.OpenSession())
            {
                return session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Where(x => x.ApplicationName == ApplicationName && x.Username == username)
                    .SelectMany(x => x.Roles)
                    .ToArray();
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            using (var session = _documentStore.OpenSession())
            {
                return session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Where(x => x.ApplicationName == ApplicationName)
                    .Select(x => x.Username)
                    .ToArray();
            }
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .SingleOrDefault(x => x.ApplicationName == ApplicationName && x.Username == username);
                return user != null && user.Roles.Contains(roleName);
            }
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            using (var session = _documentStore.OpenSession())
            {
                var users = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Where(x => x.ApplicationName == ApplicationName && x.Username.In(usernames));

                foreach (var user in users)
                {
                    foreach (var roleName in roleNames.Where(roleName => user.Roles.Contains(roleName)))
                    {
                        user.Roles.Remove(roleName);
                    }
                }

                session.SaveChanges();
            }
        }

        public override bool RoleExists(string roleName)
        {
            using (var session = _documentStore.OpenSession())
            {
                var app = session.Query<Application>().SingleOrDefault(x => x.Name == ApplicationName);
                return app != null && app.Roles.Contains(roleName);
            }
        }
    }
}
