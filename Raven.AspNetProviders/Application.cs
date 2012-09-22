using System.Collections.Generic;

namespace Raven.AspNetProviders
{
    public class Application
    {
        public Application()
        {
            Roles = new List<string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<string> Roles { get; set; }
    }
}