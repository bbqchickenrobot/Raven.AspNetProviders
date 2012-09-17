using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Raven.AspNetProviders.Tests.Membership
{
    public class RavenMembershipProviderTestBase : InMemoryDocumentStoreTestBase
    {
        protected NameValueCollection Config;

        [TestInitialize]
        public void Initialize()
        {
            Config = new NameValueCollection {
                {"applicationName", "TestApp"},
                {"name", "RavenMembershipProvider "},
                {"requiresUniqueEmail", "false"}
            };
        }
    }
}