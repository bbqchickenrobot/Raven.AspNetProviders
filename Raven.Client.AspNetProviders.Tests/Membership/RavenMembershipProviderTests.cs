﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Client.AspNetProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace Raven.Client.AspNetProviders.Tests.Membership
{
    [TestClass]
    public class RavenMembershipProviderTests : InMemoryDocumentStoreTestBase
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
