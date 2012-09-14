using System.Linq.Expressions;
using Raven.Abstractions.Exceptions;
using Raven.Client.AspNetProviders.Indexes;
using Raven.Client.Document;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using Raven.Client.Indexes;
using Raven.Client.Linq;

namespace Raven.Client.AspNetProviders
{
    public class RavenMembershipProvider : MembershipProvider
    {
        private IDocumentStore _documentStore;
        private string _providerName = "RavenMembershipProvider";
        private bool _requiresUniqueEmail;
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private int _maxInvalidPasswordAttempts;
        private int _minRequiredNonAlphanumericCharacters;
        private int _minRequiredPasswordLength;
        private int _passwordAttemptWindow;
        private string _passwordStrengthRegularExpression;
        private bool _requiresQuestionAndAnswer;

        /// <summary>
        /// Public parameterless constructor
        /// </summary>
        public RavenMembershipProvider()
        {}

        /// <summary>
        /// Constructor accepting a document store instance, used for testing.
        /// </summary>
        public RavenMembershipProvider(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        #region Properties

        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get { return _enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return _enablePasswordRetrieval; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return _maxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _minRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return _minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return _passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return MembershipPasswordFormat.Hashed; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return _passwordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return _requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return _requiresUniqueEmail; }
        }

        #endregion

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _providerName = name;

            base.Initialize(name, config);

            SetConfigurationProperties(config);

            if (_documentStore == null)
            {
                if (string.IsNullOrEmpty(config["connectionStringName"]))
                    throw new ConfigurationErrorsException("Must supply a connectionStringName.");

                _documentStore = new DocumentStore
                {
                    ConnectionStringName = config["connectionStringName"],
                };

                _documentStore.Initialize();
            }
            //_documentStore.DatabaseCommands.PutIndex()
            IndexCreation.CreateIndexes(typeof(Users_ByApplicationNameAndUsername).Assembly, _documentStore);
        }

        private void SetConfigurationProperties(NameValueCollection config)
        {
            ApplicationName = string.IsNullOrEmpty(config["applicationName"]) ? System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath : config["applicationName"];
            _requiresUniqueEmail = config["requiresUniqueEmail"] == null || Convert.ToBoolean(config["requiresUniqueEmail"]);
            _enablePasswordReset = config["enablePasswordReset"] == null || Convert.ToBoolean(config["enablePasswordReset"]);
            _enablePasswordRetrieval = config["enablePasswordRetrieval"] == null || Convert.ToBoolean(config["enablePasswordRetrieval"]);
            _maxInvalidPasswordAttempts = config["maxInvalidPasswordAttempts"] != null ? Convert.ToInt32(config["maxInvalidPasswordAttempts"]) : 5;
            _minRequiredNonAlphanumericCharacters = config["minRequiredNonAlphanumericCharacters"] != null ? Convert.ToInt32(config["minRequiredNonAlphanumericCharacters"]) : 1;
            _minRequiredPasswordLength = config["minRequiredPasswordLength"] != null ? Convert.ToInt32(config["minRequiredPasswordLength"]) : 7;
            _passwordAttemptWindow = config["passwordAttemptWindow"] != null ? Convert.ToInt32(config["passwordAttemptWindow"]) : 10;
            _passwordStrengthRegularExpression = config["passwordStrengthRegularExpression"] ?? string.Empty;
            _requiresQuestionAndAnswer = config["requiresQuestionAndAnswer"] != null && Convert.ToBoolean(config["requiresQuestionAndAnswer"]);
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == username);
                if (user != null)
                {
                    if (user.PasswordHash == EncodePassword(oldPassword, user.PasswordSalt))
                    {
                        var salt = GenerateSalt();
                        user.PasswordSalt = salt;
                        user.PasswordHash = EncodePassword(newPassword, salt);

                        session.SaveChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == username);
                if (user != null)
                {
                    if (user.PasswordHash == EncodePassword(password, user.PasswordSalt))
                    {
                        user.PasswordQuestion = newPasswordQuestion;
                        user.PasswordAnswer = EncodePassword(newPasswordAnswer, user.PasswordSalt);

                        session.SaveChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            var args = new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                throw new MembershipCreateUserException(MembershipCreateStatus.InvalidPassword);
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != string.Empty)
            {
                throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateEmail);
            }

            var existingUser = GetUser(username, false);

            if (existingUser != null)
            {
                throw new MembershipCreateUserException(MembershipCreateStatus.DuplicateUserName);
            }

            var salt = GenerateSalt();
            
            var newUser = new User
            {
                ApplicationName = this.ApplicationName,
                Username = username,
                PasswordSalt = salt,
                PasswordHash = EncodePassword(password, salt),
                Email = email,
                PasswordQuestion = passwordQuestion,
                PasswordAnswer = passwordAnswer == null ? string.Empty : EncodePassword(passwordAnswer, salt),
                IsApproved = isApproved,
                Comment = string.Empty,
                CreationDate = DateTime.UtcNow,
                LastActivityDate = DateTime.UtcNow
            };
            
            using (var session = _documentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                try
                {
                    session.Store(newUser);

                    session.SaveChanges();
                }
                catch (Exception ex)
                {
                    // TODO: handle store user exceptions
                    throw new MembershipCreateUserException(MembershipCreateStatus.ProviderError);
                }
            }

            status = MembershipCreateStatus.Success;
            return GetUser(username, false);
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            // TODO: deleteAllRelatedData
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == username);
                if (user != null)
                {
                    session.Delete(user);
                    session.SaveChanges();
                    return true;
                }
            }

            return false;
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers<Users_ByApplicationNameAndEmail>(x => x.Email, emailToMatch, pageIndex, pageSize, out totalRecords);
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers<Users_ByApplicationNameAndUsername>(x => x.Username, usernameToMatch, pageIndex, pageSize, out totalRecords);
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers<Users_ByApplicationNameAndUsername>(x => x.Username, "*" , pageIndex, pageSize, out totalRecords);
        }

        public override int GetNumberOfUsersOnline()
        {
            double userIsOnlineTimeWindow = Membership.UserIsOnlineTimeWindow;

            using (var session = _documentStore.OpenSession())
            {
                return session.Query<User>().Count(u=> u.ApplicationName == ApplicationName && u.LastActivityDate >= DateTime.UtcNow.AddMinutes(-userIsOnlineTimeWindow));
            }
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotSupportedException("Passwords cant be retreived because they are encrypted, use the ResetPassword method instead.");
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == username);
                if (user != null)
                {
                    user.LastActivityDate = DateTime.UtcNow;
                    session.SaveChanges();

                    return UserToMembershipUser(user);
                }
            }

            return null;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Load<User>(providerUserKey.ToString());
                if (user != null)
                {
                    user.LastActivityDate = DateTime.UtcNow;
                    session.SaveChanges();

                    return UserToMembershipUser(user);
                }
                return null;
            }
        }

        public override string GetUserNameByEmail(string email)
        {
            using (var session = _documentStore.OpenSession())
            {
                return session.Query<User, Users_ByApplicationNameAndEmail>()
                    .Where(u => u.ApplicationName == ApplicationName && u.Email == email)
                    .Select(x => x.Username)
                    .SingleOrDefault();
            }
        }

        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Not configured to support password resets");
            }

            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == username);
                if (user != null)
                {
                    if (user.PasswordAnswer == EncodePassword(answer, user.PasswordSalt))
                    {
                        var salt = GenerateSalt();
                        var randomPassword = Membership.GeneratePassword(10, 1);
                        
                        user.PasswordSalt = salt;
                        user.PasswordHash = EncodePassword(randomPassword, salt);
                        user.LastPasswordChangedDate = DateTime.UtcNow;

                        session.SaveChanges();

                        return randomPassword;
                    }
                }
            }
            throw new MembershipPasswordException();
        }

        public override bool UnlockUser(string userName)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == userName);
                if (user != null)
                {
                    user.IsLockedOut = false;
                    session.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        public override void UpdateUser(MembershipUser user)
        {
            using (var session = _documentStore.OpenSession())
            {
                var dbUser = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == user.UserName);
                if (dbUser != null)
                {
                    dbUser.Username = user.UserName;
                    dbUser.Email = user.Email;
                    dbUser.Comment = user.Comment;
                    dbUser.IsApproved = user.IsApproved;
                    dbUser.LastActivityDate = DateTime.UtcNow;
                    // TODO: Update all user properties

                    session.SaveChanges();
                }
            }
        }

        public override bool ValidateUser(string username, string password)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<User, Users_ByApplicationNameAndUsername>()
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .SingleOrDefault(u => u.ApplicationName == ApplicationName && u.Username == username);
                if (user != null && user.PasswordHash == EncodePassword(password, user.PasswordSalt))
                {
                    if (user.IsApproved)
                    {
                        user.LastLoginDate = DateTime.UtcNow;
                        user.LastActivityDate = DateTime.UtcNow;
                        session.SaveChanges();
                        
                        return true;
                    }
                }
            }

            return false;
        }

        private MembershipUserCollection FindUsers<TIndexCreator>(Expression<Func<User, object>> fieldSelector, string searchTerms, int pageIndex, int pageSize, out int totalRecords) 
            where TIndexCreator : AbstractIndexCreationTask, new()
        {
            var membershipUsers = new MembershipUserCollection();
            using (var session = _documentStore.OpenSession())
            {
                var users = session.Query<User, TIndexCreator>()
                    .Where(x => x.ApplicationName == ApplicationName)
                    .Search(fieldSelector, searchTerms, options: SearchOptions.And);
                totalRecords = users.Count();
                var pagedUsers = users.Skip(pageIndex * pageSize).Take(pageSize);
                foreach (var user in pagedUsers)
                {
                    membershipUsers.Add(UserToMembershipUser(user));
                }
            }
            return membershipUsers;
        }

        private static string GenerateSalt()
        {
            var buf = new byte[16];
            (new RNGCryptoServiceProvider()).GetBytes(buf);
            return Convert.ToBase64String(buf);
        }

        private static string EncodePassword(string pass, string salt)
        {
            var passBytes = Encoding.Unicode.GetBytes(pass);
            var saltBytes = Convert.FromBase64String(salt);
            var totalBytes = new byte[saltBytes.Length + passBytes.Length];
            Buffer.BlockCopy(saltBytes, 0, totalBytes, 0, saltBytes.Length);
            Buffer.BlockCopy(passBytes, 0, totalBytes, saltBytes.Length, passBytes.Length);
            //var algorithm = HashAlgorithm.Create("HMACSHA256");
            var algorithm = HashAlgorithm.Create(Membership.HashAlgorithmType);
            var computedHash = algorithm.ComputeHash(totalBytes);
            return Convert.ToBase64String(computedHash);
        }

        private MembershipUser UserToMembershipUser(User user)
        {
            var nullDate = new DateTime(1900, 1, 1, 0, 0, 0);

            return new MembershipUser(
                _providerName, user.Username, user.Id, user.Email, user.PasswordQuestion,
                user.Comment, user.IsApproved, user.IsLockedOut, user.CreationDate,
                user.LastLoginDate.HasValue ? user.LastLoginDate.Value : nullDate,
                user.LastActivityDate,
                user.LastPasswordChangedDate.HasValue ? user.LastPasswordChangedDate.Value : nullDate,
                user.LastLockedOutDate.HasValue ? user.LastLockedOutDate.Value : nullDate
                );
        }
    }
}
