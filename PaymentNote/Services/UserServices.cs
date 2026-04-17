using PaymentNote.Models;
using PaymentNote.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentNote.Services
{
    public class UserServices : IUserServices
    {
        private readonly DbPaymentNoteEntities1 _dbContext;
        private readonly User db;

        public UserServices(DbPaymentNoteEntities1 dbContext)
        {
            _dbContext = dbContext;
        }

        public UserServices(User Db)
        {
            this.db = Db;
        }

        public void InitializeAdmin()
        {
            var existingAdmin = _dbContext.Users.FirstOrDefault(u => u.username == "admin" && u.is_admin == true && u.deleted != true);
            if (existingAdmin == null)
            {
                var admin = new User
                {
                    username = "admin",
                    password = HashPassword("123456"),
                    is_admin = true,
                    created_at = DateTime.Now,
                    created_by = "System",
                    deleted = false
                };


                _dbContext.Users.Add(admin);
                _dbContext.SaveChanges();
                System.Diagnostics.Debug.WriteLine("Admin user created");
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication user: {username}");
            var user = _dbContext.Users.FirstOrDefault(u => u.username == username && u.deleted != true);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User Not Found In Database");


                return null;
            }

            var hashedPassword = HashPassword(password);


            bool isPasswordValid = VerifyPassword(password, user.password);
            System.Diagnostics.Debug.WriteLine($"Password Verification Result {isPasswordValid}");
            if (isPasswordValid)
            {
                user.Last_Login = DateTime.Now;

                return user;
            }


            return hashedPassword == user.password ? user : null;
        }
        public string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            string hashedInputPassword = HashPassword(password);
            return hashedInputPassword == passwordHash;
        }
    }
}