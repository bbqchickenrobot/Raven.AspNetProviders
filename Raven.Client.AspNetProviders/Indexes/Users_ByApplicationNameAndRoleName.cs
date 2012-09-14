using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace Raven.Client.AspNetProviders.Indexes
{
    public class Users_ByApplicationNameAndRoleName : AbstractIndexCreationTask<User>
    {
        public Users_ByApplicationNameAndRoleName()
        {
            Map = users => from user in users
                           from role in user.Roles
                           select new
                           {
                               user.ApplicationName,
                               RoleName = role
                           };
        }
    }
}