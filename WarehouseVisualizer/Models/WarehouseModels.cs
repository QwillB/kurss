using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseVisualizer.Models
{
    public class Material : INotifyPropertyChanged, ICloneable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        private string _name = string.Empty;
        private int _quantity;
        private MaterialType _type;
        private string _unit = "шт.";
        private string _qrCode = string.Empty;
        private MaterialStatus _status = MaterialStatus.Active;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public MaterialType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(128)]
        public string QrCode
        {
            get => string.IsNullOrWhiteSpace(_qrCode) ? $"MAT-{Id}" : _qrCode;
            set { _qrCode = value; OnPropertyChanged(nameof(QrCode)); }
        }

        public MaterialStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {
            return new Material
            {
                Id = this.Id,
                Name = this.Name,
                Quantity = this.Quantity,
                Type = this.Type,
                Unit = this.Unit,
                CreatedAt = this.CreatedAt,
                QrCode = this.QrCode,
                Status = this.Status
            };
        }
    }

    public class WarehouseCell : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int? MaterialId { get; set; } // Nullable, так как ячейка может быть пустой

        private Material? _material;

        public int Row { get; set; }
        public int Column { get; set; }
        public string Location => $"{Row + 1}-{Column + 1}";

        [ForeignKey("MaterialId")]
        public Material? Material
        {
            get => _material;
            set
            {
                // Автоматическая очистка некорректных данных
                if (value != null && (string.IsNullOrWhiteSpace(value.Name) || value.Quantity <= 0))
                {
                    _material = null;
                    MaterialId = null;
                }
                else
                {
                    _material = value;
                    MaterialId = value?.Id;
                }

                OnPropertyChanged(nameof(Material));
                OnPropertyChanged(nameof(HasMaterial));
            }
        }

        public bool HasMaterial => Material != null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Warehouse : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        private int _rows;
        private int _columns;
        private ObservableCollection<WarehouseCell> _cells = new ObservableCollection<WarehouseCell>();

        public int Rows
        {
            get => _rows;
            set
            {
                if (value != _rows)
                {
                    _rows = value;
                    OnPropertyChanged(nameof(Rows));
                    RebuildCells();
                }
            }
        }

        public int Columns
        {
            get => _columns;
            set
            {
                if (value != _columns)
                {
                    _columns = value;
                    OnPropertyChanged(nameof(Columns));
                    RebuildCells();
                }
            }
        }

        public ObservableCollection<WarehouseCell> Cells
        {
            get => _cells;
            set
            {
                _cells = value;
                OnPropertyChanged(nameof(Cells));
            }
        }

        public Warehouse()
        {
            RebuildCells();
        }

        // В файле Models\WarehouseModels.cs, в классе Warehouse замените метод:
        public void RebuildCells()
        {
            var existingCells = new Dictionary<(int, int), Material>();

            // Собираем только валидные ячейки
            foreach (var cell in Cells.Where(c =>
                c.Row < Rows &&
                c.Column < Columns &&
                c.Material != null &&
                !string.IsNullOrWhiteSpace(c.Material.Name)))
            {
                existingCells[(cell.Row, cell.Column)] = cell.Material;
            }

            Cells.Clear();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    var cell = new WarehouseCell { Row = row, Column = col };

                    if (existingCells.TryGetValue((row, col), out var material))
                    {
                        cell.Material = material;
                    }

                    Cells.Add(cell);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void InitializeCellsFromDatabase(List<WarehouseCell> loadedCells)
        {
            Cells.Clear();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    var cell = new WarehouseCell { Row = row, Column = col };

                    // Находим соответствующую ячейку из загруженных данных
                    var loadedCell = loadedCells?.FirstOrDefault(c => c.Row == row && c.Column == col);
                    if (loadedCell?.Material != null)
                    {
                        cell.Material = loadedCell.Material;
                    }

                    Cells.Add(cell);
                }
            }

            OnPropertyChanged(nameof(Cells));
        }
    }

    public class MaterialHistoryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public Material? Material { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public MaterialHistoryActionType ActionType { get; set; } = MaterialHistoryActionType.Updated;

        [MaxLength(20)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(20)]
        public string FromLocation { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ToLocation { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MaterialName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Comment { get; set; } = string.Empty;

        [NotMapped] // Это свойство не будет сохраняться в БД
        public string Details
        {
            get
            {
                var route = !string.IsNullOrWhiteSpace(FromLocation) || !string.IsNullOrWhiteSpace(ToLocation)
                    ? $"{FromLocation} → {ToLocation}".Trim()
                    : Location;
                var user = string.IsNullOrWhiteSpace(UserName) ? "system" : UserName;
                return $"{Timestamp:dd.MM HH:mm} | {ActionType} | {route} | {MaterialName} x{Quantity} | {user}";
            }
        }
    }

    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
        public bool IsRead { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
