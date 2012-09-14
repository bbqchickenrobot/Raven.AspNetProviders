using System;

namespace Raven.Client.AspNetProviders
{
    public class Profile
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public bool IsAnonymous { get; set; }
        public DateTime LastActivityDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }
}