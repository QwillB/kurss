using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseVisualizer.Models
{
    public class User : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        private string _username = string.Empty;
        private string _passwordHash = string.Empty;
        private UserRole _role;
        private string _fullName = string.Empty;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        [Required]
        [MaxLength(256)]
        public string PasswordHash
        {
            get => _passwordHash;
            set { _passwordHash = value; OnPropertyChanged(nameof(PasswordHash)); }
        }

        [Required]
        public UserRole Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(nameof(Role)); }
        }

        [MaxLength(100)]
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        [NotMapped]
        public string RoleDescription => Role switch
        {
            UserRole.Admin => "Администратор",
            UserRole.Storekeeper => "Кладовщик",
            UserRole.Auditor => "Аудитор",
            _ => "Неизвестно"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}