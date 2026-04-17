using PaymentNote.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentNote.Services
{
    public interface IUserServices
    {
        User AuthenticateUser(string username, string password);
        void InitializeAdmin();
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}