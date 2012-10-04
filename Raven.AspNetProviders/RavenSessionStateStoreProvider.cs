using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.SessionState;
using Raven.Client;
using Raven.Client.Document;
using Raven.Json.Linq;

namespace Raven.AspNetProviders
{
    public class RavenSessionStateStoreProvider : SessionStateStoreProviderBase
    {
        private IDocumentStore _documentStore;
        private SessionStateSection _sessionStateConfig;

        /// <summary>
        /// Public parameterless constructor
        /// </summary>
        public RavenSessionStateStoreProvider()
        {
        }

        /// <summary>
        /// Constructor accepting a document store instance, used for testing.
        /// </summary>
        public RavenSessionStateStoreProvider(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public string ApplicationName { get; set; }

        /// <summary>
        /// Initializes the provider.
        /// </summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
        /// <exception cref="T:System.ArgumentNullException">The name of the provider is null.</exception>
        /// <exception cref="T:System.ArgumentException">The name of the provider has a length of zero.</exception>
        /// <exception cref="T:System.InvalidOperationException">An attempt is made to call 
        /// <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/> 
        /// on a provider after the provider has already been initialized.
        /// </exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

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
            ApplicationName = string.IsNullOrEmpty(config["applicationName"])
                                  ? HostingEnvironment.ApplicationVirtualPath
                                  : config["applicationName"];
            _sessionStateConfig = (SessionStateSection) ConfigurationManager.GetSection("system.web/sessionState");
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:System.Web.SessionState.SessionStateStoreProviderBase"/> implementation.
        /// </summary>
        public override void Dispose()
        {
            if (_documentStore != null && !_documentStore.WasDisposed)
            {
                _documentStore.Dispose();
            }
        }

        /// <summary>
        /// Sets a reference to the <see cref="T:System.Web.SessionState.SessionStateItemExpireCallback"/> 
        /// delegate for the Session_OnEnd event defined in the Global.asax file.
        /// </summary>
        /// <returns>
        /// true if the session-state store provider supports calling the Session_OnEnd event; otherwise, false.
        /// </returns>
        /// <param name="expireCallback">The <see cref="T:System.Web.SessionState.SessionStateItemExpireCallback"/>  
        /// delegate for the Session_OnEnd event defined in the Global.asax file.
        /// </param>
        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        /// <summary>
        /// Called by the <see cref="T:System.Web.SessionState.SessionStateModule"/> object for per-request initialization.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext"/> for the current request.</param>
        public override void InitializeRequest(HttpContext context)
        {
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked,
                                                      out TimeSpan lockAge, out object lockId,
                                                      out SessionStateActions actions)
        {
            return GetItem(context, id, out locked, out lockAge, out lockId, out actions, false);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked,
                                                               out TimeSpan lockAge, out object lockId,
                                                               out SessionStateActions actions)
        {
            return GetItem(context, id, out locked, out lockAge, out lockId, out actions, true);
        }

        private SessionStateStoreData GetItem(HttpContext context, string id, out bool locked,
                                                      out TimeSpan lockAge, out object lockId,
                                                      out SessionStateActions actions, bool lockRecord)
        {
            id = "sessionstates/" + id;
            lockAge = TimeSpan.Zero;
            lockId = null;
            locked = false;
            actions = 0;

            using (var session = _documentStore.OpenSession())
            {
                var sessionState = session.Query<SessionState>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                    .SingleOrDefault(x => x.Id == id && x.ApplicationName == ApplicationName);

                if (sessionState == null)
                {
                    return null;
                }

                if (sessionState.IsLocked)
                {
                    locked = true;
                    lockId = sessionState.LockId;
                    lockAge = DateTime.UtcNow.Subtract(sessionState.LockDate);
                    actions = sessionState.Flags;
                    return null;
                }

                if (sessionState.ExpireDate < DateTime.UtcNow)
                {
                    session.Delete(sessionState);
                    session.SaveChanges();
                    return null;
                }

                if (lockRecord)
                {
                    sessionState.IsLocked = true;
                    sessionState.LockId += 1;
                    sessionState.LockDate = DateTime.UtcNow;
                    session.SaveChanges();
                }

                lockId = sessionState.LockId;

                if (actions == SessionStateActions.InitializeItem)
                {
                    return new SessionStateStoreData(new SessionStateItemCollection(),
                                                     SessionStateUtility.GetSessionStaticObjects(context),
                                                     (int) _sessionStateConfig.Timeout.TotalMinutes);
                }

                var sessionItems = new SessionStateItemCollection();
                foreach (var sessionItem in sessionState.SessionItems)
                {
                    sessionItems[sessionItem.Key] = sessionItem.Value;
                }

                return new SessionStateStoreData(sessionItems, SessionStateUtility.GetSessionStaticObjects(context),
                                                 (int) _sessionStateConfig.Timeout.TotalMinutes);
            }
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            id = "sessionstates/" + id;

            using (var session = _documentStore.OpenSession())
            {
                var sessionState = session.Query<SessionState>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                    .Single(x => x.Id == id && x.ApplicationName == ApplicationName && x.LockId == (int)lockId);

                sessionState.IsLocked = false;

                var expireDate = DateTime.UtcNow.AddMinutes(_sessionStateConfig.Timeout.TotalMinutes);

                sessionState.ExpireDate = expireDate;
                session.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expireDate);

                session.SaveChanges();
            }
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item,
                                                        object lockId, bool newItem)
        {
            id = "sessionstates/" + id;
            var sessionItems = new Dictionary<string, string>();
            for (var i = 0; i < item.Items.Count; i++)
            {
                sessionItems.Add(item.Items.Keys[i],item.Items[i].ToString());
            }
            
            using (var session = _documentStore.OpenSession())
            {
                SessionState sessionState;
                if (newItem)
                {
                    sessionState = session.Query<SessionState>()
                        .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                        .SingleOrDefault(
                            x => x.Id == id && x.ApplicationName == ApplicationName && x.ExpireDate < DateTime.UtcNow);

                    if (sessionState != null)
                    {
                        throw new InvalidOperationException(string.Format("Item aleady exist with SessionId={0} and ApplicationName={1}", id, lockId));
                    }

                    sessionState = new SessionState {
                        Id = id,
                        ApplicationName = ApplicationName
                    };

                    session.Store(sessionState);
                }
                else
                {
                    sessionState = session.Query<SessionState>()
                        .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                        .Single(x => x.Id == id && x.ApplicationName == ApplicationName && x.LockId == (int)lockId);
                }

                var expireDate = DateTime.UtcNow.AddMinutes(_sessionStateConfig.Timeout.TotalMinutes);

                sessionState.ExpireDate = expireDate;
                session.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expireDate);
                sessionState.SessionItems = sessionItems;
                sessionState.IsLocked = false;

                session.SaveChanges();
            }
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            id = "sessionstates/" + id;

            using (var session = _documentStore.OpenSession())
            {
                var sessionState = session.Query<SessionState>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                    .SingleOrDefault(x => x.Id == id && x.ApplicationName == ApplicationName && x.LockId == (int) lockId);

                if (sessionState != null)
                {
                    session.Delete(sessionState);
                    session.SaveChanges();
                }
            }
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            id = "sessionstates/" + id;

            using (var session = _documentStore.OpenSession())
            {
                var sessionState = session.Query<SessionState>()
                    .SingleOrDefault(x => x.Id == id && x.ApplicationName == ApplicationName);

                if (sessionState != null)
                {
                    var expireDate = DateTime.UtcNow.AddMinutes(_sessionStateConfig.Timeout.TotalMinutes);
                    sessionState.ExpireDate = expireDate;
                    session.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expireDate);
                    session.SaveChanges();
                }
            }
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(),
                                             SessionStateUtility.GetSessionStaticObjects(context), timeout);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            id = "sessionstates/" + id;
            using (var session = _documentStore.OpenSession())
            {
                var expireDate = DateTime.UtcNow.AddMinutes(timeout);

                var sessionState = new SessionState {
                    Id = id,
                    ApplicationName = ApplicationName,
                    ExpireDate = expireDate
                };

                session.Store(sessionState);
                session.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expireDate);
                session.SaveChanges();
            }
        }

        public override void EndRequest(HttpContext context)
        {
        }
    }
}