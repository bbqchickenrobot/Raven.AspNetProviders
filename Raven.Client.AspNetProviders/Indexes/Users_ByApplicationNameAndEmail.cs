using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace Raven.Client.AspNetProviders.Indexes
{
    public class Users_ByApplicationNameAndEmail : AbstractIndexCreationTask<User>
    {
        public Users_ByApplicationNameAndEmail()
        {
            Map = users => from user in users
                           select new {
                               user.ApplicationName,
                               user.Email
                           };

            Index(x => x.Email, FieldIndexing.Analyzed);
        }
    }
}