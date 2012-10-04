using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace Raven.AspNetProviders.Indexes
{
    public class Users_ByApplicationNameAndUsername : AbstractIndexCreationTask<User> 
    {
        public Users_ByApplicationNameAndUsername()
        {
            Map = users => from user in users
                           select new
                           {
                               user.ApplicationName,
                               user.Username
                           };

            Index(x=>x.Username, FieldIndexing.Analyzed);
        }
    }
}