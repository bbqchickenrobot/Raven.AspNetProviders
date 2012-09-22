using System;
using System.Collections.Generic;
using System.Web.SessionState;

namespace Raven.AspNetProviders
{
    public class SessionState
    {
        public SessionState()
        {
            SessionItems = new Dictionary<string, string>();
            CreationDate = DateTime.UtcNow;
        }

        public string Id { get; set; }
        public string ApplicationName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public DateTime LockDate { get; set; }
        public int LockId { get; set; }
        public bool IsLocked { get; set; }
        public IDictionary<string,string> SessionItems { get; set; }
        public SessionStateActions Flags { get; set; }
    }
}