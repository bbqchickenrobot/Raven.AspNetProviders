using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.AspNetProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace Raven.AspNetProviders.Tests.Membership
{
    [TestClass]
    public class RavenMembershipProviderTests : RavenMembershipProviderTestBase
    {
        [TestMethod]
        public void RunRavenInMemory()
        {
            using(var store = NewEmbeddableDocumentStore())
            {
                store.Should().NotBeNull();
            }
        }

        [TestMethod]
        public void CreateNewMembershipUserShouldCreateDocument()
        {
            using (var store = NewEmbeddableDocumentStore())
            {
                var provider = new RavenMembershipProvider(store);
                provider.Initialize(Config["name"], Config);

                MembershipCreateStatus status;
                var membershipUser = provider.CreateUser(
                    "user1","password123$","user1@domain.com", null, null, true, null, out status
                    );

                status.Should().Be(MembershipCreateStatus.Success);
                membershipUser.Should().NotBeNull();
                membershipUser.ProviderUserKey.Should().NotBeNull();
            }
        }
    }
}
