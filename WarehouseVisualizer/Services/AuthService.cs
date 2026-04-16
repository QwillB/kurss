using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Services
{
    public interface IAuthService
    {
        User? Authenticate(string username, string password);
        string HashPassword(string password);
        bool HasPermission(User? user, UserRole requiredRole);
    }

    public class AuthService : IAuthService
    {
        private readonly WarehouseDbContext _context;

        public AuthService()
            : this(new WarehouseDbContext())
        {
        }

        public AuthService(WarehouseDbContext context)
        {
            _context = context;
        }

        public User? Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = _context.Users
                .FirstOrDefault(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            var hashedPassword = HashPassword(password);
            if (user.PasswordHash != hashedPassword)
                return null;

            return user;
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToUpper();
            }
        }

        public bool HasPermission(User? user, UserRole requiredRole)
        {
            if (user == null || !user.IsActive)
                return false;

            if (user.Role == UserRole.Admin)
                return true;

            if (user.Role == UserRole.Storekeeper)
            {
                return requiredRole == UserRole.Storekeeper;
            }

            if (user.Role == UserRole.Auditor)
            {
                return requiredRole == UserRole.Auditor;
            }

            return false;
        }
    }
}
