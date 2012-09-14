using System.Collections.Generic;

namespace Raven.Client.AspNetProviders
{
    public class Application
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<string> Roles { get; set; }
    }
}