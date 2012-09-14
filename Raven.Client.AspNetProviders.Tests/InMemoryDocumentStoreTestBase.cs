using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Database.Server;

namespace Raven.Client.AspNetProviders.Tests
{
    public class InMemoryDocumentStoreTestBase
    {
        protected EmbeddableDocumentStore NewEmbeddableDocumentStore()
        {
            //NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8080);

            var store = new EmbeddableDocumentStore {
                RunInMemory = true,
                //UseEmbeddedHttpServer = true
            };
            store.Initialize();
            return store;
        }
    }
}
