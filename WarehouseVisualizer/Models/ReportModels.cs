using System.Collections.Generic;
using System.ComponentModel;

namespace WarehouseVisualizer.Models
{
    public class MaterialReportItem : INotifyPropertyChanged
    {
        private string _name = "";
        private MaterialType _type;
        private int _quantity;
        private string _unit = "";
        private int _cellCount;
        private string _locations = "";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public MaterialType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        public int CellCount
        {
            get => _cellCount;
            set { _cellCount = value; OnPropertyChanged(nameof(CellCount)); }
        }

        public string Locations
        {
            get => _locations;
            set { _locations = value; OnPropertyChanged(nameof(Locations)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WarehouseReport
    {
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public int TotalCells { get; set; }
        public int OccupiedCells { get; set; }
        public int FreeCells { get; set; }
        public double OccupancyPercentage => TotalCells > 0 ? (double)OccupiedCells / TotalCells * 100 : 0;
        public List<MaterialReportItem> Materials { get; set; } = new List<MaterialReportItem>();
    }

    public class ImportMaterial
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Location { get; set; } // Формат: "ряд-колонка", например "1-3"
    }
}