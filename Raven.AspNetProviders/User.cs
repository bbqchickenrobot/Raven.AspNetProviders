using System;
using System.Collections.Generic;

namespace Raven.AspNetProviders
{
    public class User
    {
        public User()
        {
            Roles = new List<string>();
        }

        public int Id { get; set; }
        public string ApplicationName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordQuestion { get; set; }
        public string PasswordAnswer { get; set; }
        public string Email { get; set; }
        public string Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime LastActivityDate { get; set; }
        public DateTime? LastPasswordChangedDate { get; set; }
        //public bool IsOnline { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LastLockedOutDate { get; set; }
        public int FailedPasswordAttemptCount { get; set; }
        public DateTime? FailedPasswordAttemptWindowsStart { get; set; }
        public int FailedPasswordAnswerAttemptCount { get; set; }
        public DateTime? FailedPasswordAnswerAttemptWindowStart { get; set; }

        public IList<string> Roles { get; set; }
    }
}

