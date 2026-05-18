using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OfficeOpenXml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;
using WarehouseVisualizer.Views;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;

namespace WarehouseVisualizer.ViewModels
{
    public partial class WarehouseViewModel : ObservableObject
    {
        private readonly SqlDataService _sqlDataService = new SqlDataService();
        private readonly IAuthService _authService = new AuthService();
        private readonly DispatcherTimer _clockTimer = new DispatcherTimer();
        private readonly DispatcherTimer _simulationTimer = new DispatcherTimer();
        private readonly Random _random = new Random();

        [ObservableProperty]
        private Warehouse _warehouse = new Warehouse { Rows = 8, Columns = 8 };

        [ObservableProperty]
        private ObservableCollection<Material> _availableMaterials = new ObservableCollection<Material>();

        [ObservableProperty]
        private ObservableCollection<MaterialHistoryItem> _history = new ObservableCollection<MaterialHistoryItem>();

        [ObservableProperty]
        private ObservableCollection<Notification> _notifications = new ObservableCollection<Notification>();

        [ObservableProperty]
        private ObservableCollection<ActivityEvent> _activityFeed = new ObservableCollection<ActivityEvent>();

        [ObservableProperty]
        private ObservableCollection<QrPixel> _selectedQrPixels = new ObservableCollection<QrPixel>();

        [ObservableProperty]
        private string _materialSearchText = string.Empty;

        [ObservableProperty]
        private bool _showOnlyLowStock;

        [ObservableProperty]
        private string _activePage = "Карта склада";

        [ObservableProperty]
        private string _currentTimeText = DateTime.Now.ToString("HH:mm:ss");

        [ObservableProperty]
        private bool _isHeatmapEnabled = true;

        [ObservableProperty]
        private string _heatmapMode = "Заполненность";

        [ObservableProperty]
        private double _heatmapOpacity = 0.45;

        [ObservableProperty]
        private double _mapZoom = 1.0;

        [ObservableProperty]
        private WarehouseCell? _suggestedCell;

        [ObservableProperty]
        private int _smartPlacementScore;

        [ObservableProperty]
        private string _smartPlacementReason = "Выберите материал и нажмите «Найти лучшую ячейку».";

        [ObservableProperty]
        private bool _hasPlacementSuggestion;

        [ObservableProperty]
        private bool _isSimulationRunning;

        [ObservableProperty]
        private int _simulationSpeed = 2;

        [ObservableProperty]
        private int _simulationEventsCount;

        [ObservableProperty]
        private string _forecastMessage = "Прогноз склада стабильный.";

        [ObservableProperty]
        private int _forecastRiskPercent = 28;

        [ObservableProperty]
        private int _activeUsersCount = 1;

        [ObservableProperty]
        private ObservableCollection<MaterialReportItem> _reportItems = new ObservableCollection<MaterialReportItem>();

        [ObservableProperty]
        private WarehouseReport _warehouseReport = new WarehouseReport();

        [ObservableProperty]
        private WarehouseCell? _selectedCell;

        [ObservableProperty]
        private Material? _selectedMaterial;

        [ObservableProperty]
        private int _selectedQuantity = 1;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private bool _canImportData;

        [ObservableProperty]
        private bool _canViewHistory = true; // История доступна всем

        // Исправленные свойства прав доступа
        [ObservableProperty]
        private bool _isAdmin;

        [ObservableProperty]
        private bool _isStorekeeper;

        [ObservableProperty]
        private bool _isAuditor;

        [ObservableProperty]
        private bool _canEditWarehouse; // Редактирование склада (только админ и кладовщик)

        [ObservableProperty]
        private bool _canEditMaterials; // Редактирование материалов (только админ и кладовщик)

        [ObservableProperty]
        private bool _canViewReports; // Просмотр отчетов (все)

        [ObservableProperty]
        private bool _canManageUsers; // Управление пользователями (только админ)

        [ObservableProperty]
        private bool _canExportData; // Экспорт данных (админ и аудитор)

        [ObservableProperty]
        private bool _isReadOnlyMode; // Только для чтения (аудитор)

        // Команды объявлены как свойства с инициализацией
        [ObservableProperty]
        private RelayCommand<WarehouseCell>? _editCellCommand;

        [ObservableProperty]
        private RelayCommand<WarehouseCell>? _clearCellCommand;

        [ObservableProperty]
        private RelayCommand? _saveDataCommand;

        [ObservableProperty]
        private RelayCommand? _loadDataCommand;

        [ObservableProperty]
        private RelayCommand? _addNewMaterialCommand;

        [ObservableProperty]
        private RelayCommand<WarehouseCell>? _dropOnCellCommand;

        [ObservableProperty]
        private RelayCommand<(WarehouseCell Source, WarehouseCell Target)>? _moveMaterialCommand;

        [ObservableProperty]
        private RelayCommand<WarehouseCell>? _startDragCommand;

        [ObservableProperty]
        private RelayCommand? _logoutCommand;

        [ObservableProperty]
        private RelayCommand? _exportMaterialsCommand;

        [ObservableProperty]
        private RelayCommand? _exportReportCommand;

        [ObservableProperty]
        private RelayCommand? _importMaterialsCommand;

        [ObservableProperty]
        private RelayCommand? _createTemplateCommand;

        // Новые команды для работы с материалами из списка
        [ObservableProperty]
        private RelayCommand<Material>? _editMaterialCommand;

        [ObservableProperty]
        private RelayCommand<Material>? _deleteMaterialCommand;

        [ObservableProperty]
        private RelayCommand<Material>? _startMaterialDragCommand;

        [ObservableProperty]
        private RelayCommand<Material>? _generateMaterialReportCommand;

        // Команда для обновления отчета
        [ObservableProperty]
        private RelayCommand? _refreshReportCommand;

        [ObservableProperty]
        private RelayCommand? _resetMaterialFilterCommand;

        [ObservableProperty]
        private RelayCommand<Notification>? _markNotificationAsReadCommand;

        [ObservableProperty]
        private RelayCommand? _markAllNotificationsAsReadCommand;

        [ObservableProperty]
        private RelayCommand<string>? _navigateCommand;

        [ObservableProperty]
        private RelayCommand? _zoomInCommand;

        [ObservableProperty]
        private RelayCommand? _zoomOutCommand;

        [ObservableProperty]
        private RelayCommand? _resetMapViewCommand;

        [ObservableProperty]
        private RelayCommand? _findBestCellCommand;

        [ObservableProperty]
        private RelayCommand? _confirmPlacementCommand;

        [ObservableProperty]
        private RelayCommand? _rejectPlacementCommand;

        [ObservableProperty]
        private RelayCommand? _findAlternativeCommand;

        [ObservableProperty]
        private RelayCommand? _toggleSimulationCommand;

        [ObservableProperty]
        private RelayCommand? _generateDemoDataCommand;

        [ObservableProperty]
        private RelayCommand? _generateQrCommand;

        [ObservableProperty]
        private RelayCommand? _createBackupCommand;

        [ObservableProperty]
        private RelayCommand? _restoreBackupCommand;

        [ObservableProperty]
        private RelayCommand? _deleteOldBackupCommand;

        // Новые команды для управления пользователями
        [ObservableProperty]
        private ObservableCollection<User> _users = new ObservableCollection<User>();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private RelayCommand? _addUserCommand;

        [ObservableProperty]
        private RelayCommand<User>? _editUserCommand;

        [ObservableProperty]
        private RelayCommand<User>? _deleteUserCommand;

        [ObservableProperty]
        private RelayCommand? _openUserManagementCommand;

        [ObservableProperty]
        private bool _canEditWarehouseStructure; // Изменение размеров склада (только админ)

        [ObservableProperty]
        private bool _canCreateReports; // Создание отчетов и шаблонов

        [ObservableProperty]
        private bool _canExportMaterialsProp; // Экспорт материалов (только админ) - исправленное имя

        [ObservableProperty]
        private bool _canExportReports; // Экспорт отчетов (админ и аудитор)

        // Свойство для количества занятых ячеек
        public int OccupiedCellsCount => Warehouse?.Cells?.Count(c => c.HasMaterial) ?? 0;

        // Свойство для общего количества ячеек
        public int TotalCellsCount => (Warehouse?.Rows ?? 0) * (Warehouse?.Columns ?? 0);

        public int FreeCellsCount => Math.Max(0, TotalCellsCount - OccupiedCellsCount);

        public int LowStockMaterialsCount => AvailableMaterials.Count(m => m.Quantity <= 5);

        public int UnreadNotificationsCount => Notifications.Count(n => !n.IsRead);

        public int DailyOperationsCount => History.Count(h => h.Timestamp.Date == DateTime.Now.Date);

        public int MonthlyOperationsCount => History.Count(h => h.Timestamp >= DateTime.Now.Date.AddDays(-30));

        public string SelectedCellLocation => SelectedCell?.Location ?? "Ячейка не выбрана";

        public Material? DetailsMaterial => SelectedCell?.Material ?? SelectedMaterial;

        public string SelectedQrCodeText => DetailsMaterial?.QrCode
            ?? (SelectedCell != null ? $"CELL-{SelectedCell.Location}" : "QR не выбран");

        public WarehouseViewModel()
        {
            // Инициализация для дизайнера
            InitializeCommands();
            InitializeTimers();
            InitializeTestData();
            ConfigureMaterialFilter();
            BuildActivityFeed();
            UpdateForecast();
        }

        public WarehouseViewModel(User user)
        {
            CurrentUser = user;
            InitializePermissions();

            Console.WriteLine($"User: {user.Username}, Role: {user.Role}");
            Console.WriteLine($"CanEditMaterials: {CanEditMaterials}");
            Console.WriteLine($"CanImportData: {CanImportData}");
            Console.WriteLine($"CanExportData: {CanExportData}");
            Console.WriteLine($"IsAdmin: {IsAdmin}");
            Console.WriteLine($"IsStorekeeper: {IsStorekeeper}");
            Console.WriteLine($"IsAuditor: {IsAuditor}");

            // Загрузка данных из БД
            _warehouse = _sqlDataService.LoadWarehouse() ?? new Warehouse { Rows = 8, Columns = 8 };
            _availableMaterials = new ObservableCollection<Material>(_sqlDataService.LoadMaterials());

            // Если нет материалов, добавляем стандартные (только если пользователь имеет права)
            if (!_availableMaterials.Any() && CanEditMaterials)
            {
                InitializeDefaultMaterials();
            }

            _history = new ObservableCollection<MaterialHistoryItem>();
            _selectedMaterial = _availableMaterials.FirstOrDefault();

            InitializeCommands();
            InitializeTimers();
            ConfigureMaterialFilter();
            InitializeMessenger();
            LoadHistory();
            GenerateReport();
            BuildActivityFeed();
            UpdateQrPreview();
            UpdateForecast();
        }

        private void InitializeDefaultMaterials()
        {
            var defaultMaterials = new List<Material>
            {
                new Material { Name = "Кабель ВВГнг 3x1.5", Type = MaterialType.Cable, Quantity = 100, Unit = "м" },
                new Material { Name = "Труба ППР 20мм", Type = MaterialType.Pipe, Quantity = 50, Unit = "м" },
                new Material { Name = "Молоток", Type = MaterialType.Tool, Quantity = 20, Unit = "шт." },
                new Material { Name = "Доска 50x100", Type = MaterialType.Lumber, Quantity = 30, Unit = "м" },
                new Material { Name = "Профиль 60x27", Type = MaterialType.Metal, Quantity = 40, Unit = "шт." },
                new Material { Name = "Мешок цемента", Type = MaterialType.Concrete, Quantity = 25, Unit = "шт." },
                new Material { Name = "Утеплитель минеральная вата", Type = MaterialType.Insulation, Quantity = 15, Unit = "м²" },
                new Material { Name = "Краска акриловая", Type = MaterialType.Paint, Quantity = 35, Unit = "л" },
                new Material { Name = "Саморезы 4.2x75", Type = MaterialType.Other, Quantity = 500, Unit = "шт." }
            };

            foreach (var material in defaultMaterials)
            {
                AvailableMaterials.Add(material);
                _sqlDataService.SaveMaterial(material);
            }
        }

        private void InitializeTestData()
        {
            // Только для дизайнера
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                InitializeDefaultMaterials();
                GenerateReport();
            }
        }

        private void InitializeCommands()
        {
            // Команды для ячеек
            EditCellCommand = new RelayCommand<WarehouseCell>(EditCell, CanEditCell);
            ClearCellCommand = new RelayCommand<WarehouseCell>(ClearCell, CanClearCell);

            // Основные команды
            SaveDataCommand = new RelayCommand(SaveData, CanSaveData);
            LoadDataCommand = new RelayCommand(LoadData, CanLoadData);
            AddNewMaterialCommand = new RelayCommand(AddNewMaterial, CanAddNewMaterial);
            DropOnCellCommand = new RelayCommand<WarehouseCell>(DropOnCell, CanDropOnCell);
            MoveMaterialCommand = new RelayCommand<(WarehouseCell, WarehouseCell)>(MoveMaterial);
            StartDragCommand = new RelayCommand<WarehouseCell>(StartDrag);
            LogoutCommand = new RelayCommand(Logout);
            ExportMaterialsCommand = new RelayCommand(ExportMaterials, CanExportMaterials);
            ExportReportCommand = new RelayCommand(ExportReport, CanExportReport);
            ImportMaterialsCommand = new RelayCommand(ImportMaterials, CanImportMaterials);
            CreateTemplateCommand = new RelayCommand(CreateImportTemplate, CanCreateTemplate);

            // Новые команды для работы с материалами
            EditMaterialCommand = new RelayCommand<Material>(EditMaterial, CanEditMaterial);
            DeleteMaterialCommand = new RelayCommand<Material>(DeleteMaterial, CanDeleteMaterial);
            StartMaterialDragCommand = new RelayCommand<Material>(StartMaterialDrag);
            GenerateMaterialReportCommand = new RelayCommand<Material>(GenerateMaterialReport, CanGenerateMaterialReport);
            RefreshReportCommand = new RelayCommand(GenerateReport, CanGenerateReport);
            ResetMaterialFilterCommand = new RelayCommand(ResetMaterialFilter);
            MarkNotificationAsReadCommand = new RelayCommand<Notification>(MarkNotificationAsRead, n => n != null && !n.IsRead);
            MarkAllNotificationsAsReadCommand = new RelayCommand(MarkAllNotificationsAsRead);
            NavigateCommand = new RelayCommand<string>(Navigate);
            ZoomInCommand = new RelayCommand(() => MapZoom = Math.Min(1.6, MapZoom + 0.1));
            ZoomOutCommand = new RelayCommand(() => MapZoom = Math.Max(0.7, MapZoom - 0.1));
            ResetMapViewCommand = new RelayCommand(() => MapZoom = 1.0);
            FindBestCellCommand = new RelayCommand(FindBestCell);
            ConfirmPlacementCommand = new RelayCommand(ConfirmPlacement, () => HasPlacementSuggestion);
            RejectPlacementCommand = new RelayCommand(RejectPlacement, () => HasPlacementSuggestion);
            FindAlternativeCommand = new RelayCommand(FindBestCell);
            ToggleSimulationCommand = new RelayCommand(ToggleSimulation);
            GenerateDemoDataCommand = new RelayCommand(GenerateDemoData);
            GenerateQrCommand = new RelayCommand(GenerateQrPreview);
            CreateBackupCommand = new RelayCommand(CreateBackup);
            RestoreBackupCommand = new RelayCommand(RestoreBackup);
            DeleteOldBackupCommand = new RelayCommand(DeleteOldBackup);

            // Команды для управления пользователями
            AddUserCommand = new RelayCommand(AddUser, () => IsAdmin);
            EditUserCommand = new RelayCommand<User>(EditUser, user => IsAdmin && user != null);
            DeleteUserCommand = new RelayCommand<User>(DeleteUser, user => IsAdmin && user != null);
            OpenUserManagementCommand = new RelayCommand(OpenUserManagement, () => IsAdmin);
        }

        private void InitializeTimers()
        {
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (_, _) => CurrentTimeText = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            _simulationTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, SimulationSpeed));
            _simulationTimer.Tick += (_, _) => GenerateSimulationEvent();
        }

        // В конструкторе WarehouseViewModel добавьте или обновите обработчик:
        private void InitializeMessenger()
        {
            WeakReferenceMessenger.Default.Register<NotificationMessage>(this, (r, m) =>
            {
                // Записываем в консоль для отладки
                Console.WriteLine($"[MESSENGER] {m.Value}");

                // Отображаем только первые 200 символов в статусной строке
                StatusMessage = m.Value.Length > 200 ? m.Value.Substring(0, 200) + "..." : m.Value;

                // В уведомления сохраняем полное сообщение
                Notifications.Insert(0, new Notification
                {
                    Message = m.Value,
                    Type = m.Value.Contains("❌") ? NotificationType.Error :
                           m.Value.Contains("✅") ? NotificationType.Success : NotificationType.Info
                });

                // Ограничиваем количество уведомлений
                if (Notifications.Count > 20)
                {
                    Notifications.RemoveAt(Notifications.Count - 1);
                }

                OnPropertyChanged(nameof(OccupiedCellsCount));
                OnPropertyChanged(nameof(UnreadNotificationsCount));
            });
        }

        partial void OnMaterialSearchTextChanged(string value)
        {
            RefreshMaterialFilter();
        }

        partial void OnShowOnlyLowStockChanged(bool value)
        {
            RefreshMaterialFilter();
        }

        partial void OnSelectedMaterialChanged(Material? value)
        {
            UpdateQrPreview();
            OnPropertyChanged(nameof(DetailsMaterial));
            OnPropertyChanged(nameof(SelectedQrCodeText));
        }

        partial void OnSelectedCellChanged(WarehouseCell? value)
        {
            UpdateQrPreview();
            OnPropertyChanged(nameof(SelectedCellLocation));
            OnPropertyChanged(nameof(DetailsMaterial));
            OnPropertyChanged(nameof(SelectedQrCodeText));
        }

        partial void OnSimulationSpeedChanged(int value)
        {
            _simulationTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, value));
        }

        private void Navigate(string? page)
        {
            if (!string.IsNullOrWhiteSpace(page))
            {
                ActivePage = page;
            }
        }

        private void RefreshMaterialFilter()
        {
            CollectionViewSource.GetDefaultView(AvailableMaterials)?.Refresh();
        }

        private void ConfigureMaterialFilter()
        {
            var view = CollectionViewSource.GetDefaultView(AvailableMaterials);
            if (view == null)
            {
                return;
            }

            view.Filter = item =>
            {
                if (item is not Material material)
                {
                    return false;
                }

                if (ShowOnlyLowStock && material.Quantity > 5)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(MaterialSearchText))
                {
                    return true;
                }

                var search = MaterialSearchText.Trim();
                return material.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || material.Type.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    || material.Unit.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || material.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    || material.QrCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || material.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
            };
        }

        private void ResetMaterialFilter()
        {
            MaterialSearchText = string.Empty;
            ShowOnlyLowStock = false;
            RefreshMaterialFilter();
        }

        private void MarkNotificationAsRead(Notification? notification)
        {
            if (notification == null)
            {
                return;
            }

            var index = Notifications.IndexOf(notification);
            var readNotification = new Notification
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                Timestamp = notification.Timestamp,
                IsRead = true
            };

            if (index >= 0)
            {
                Notifications[index] = readNotification;
            }
            else
            {
                notification.IsRead = true;
            }

            OnPropertyChanged(nameof(UnreadNotificationsCount));
            MarkNotificationAsReadCommand?.NotifyCanExecuteChanged();
        }

        private void MarkAllNotificationsAsRead()
        {
            for (var i = 0; i < Notifications.Count; i++)
            {
                var notification = Notifications[i];
                Notifications[i] = new Notification
                {
                    Id = notification.Id,
                    Message = notification.Message,
                    Type = notification.Type,
                    Priority = notification.Priority,
                    Timestamp = notification.Timestamp,
                    IsRead = true
                };
            }

            StatusMessage = "Все уведомления отмечены как прочитанные.";
            OnPropertyChanged(nameof(UnreadNotificationsCount));
            MarkNotificationAsReadCommand?.NotifyCanExecuteChanged();
        }

        private void FindBestCell()
        {
            var material = SelectedMaterial ?? SelectedCell?.Material;
            if (material == null && AvailableMaterials.Any())
            {
                material = AvailableMaterials.First();
                SelectedMaterial = material;
            }

            if (material == null)
            {
                SmartPlacementReason = "Выберите материал перед автоматическим подбором ячейки.";
                return;
            }

            var freeCells = Warehouse.Cells.Where(c => !c.HasMaterial).ToList();
            if (!freeCells.Any())
            {
                SmartPlacementReason = "Свободных ячеек нет. Прогноз заполненности критический.";
                SmartPlacementScore = 0;
                HasPlacementSuggestion = false;
                return;
            }

            var best = freeCells
                .Select(cell => new
                {
                    Cell = cell,
                    Score = ScoreCellForMaterial(cell, material)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Cell.Row)
                .ThenBy(x => x.Cell.Column)
                .First();

            SuggestedCell = best.Cell;
            SmartPlacementScore = best.Score;
            SmartPlacementReason = $"Лучшая ячейка: {best.Cell.Location}. Учтены совместимость категории, баланс свободного пространства и низкая нагрузка зоны для типа {material.Type}.";
            HasPlacementSuggestion = true;
            ConfirmPlacementCommand?.NotifyCanExecuteChanged();
            RejectPlacementCommand?.NotifyCanExecuteChanged();
        }

        private int ScoreCellForMaterial(WarehouseCell cell, Material material)
        {
            var score = 45;
            var neighbors = Warehouse.Cells
                .Where(c => c.HasMaterial && c.Material != null)
                .Select(c => new
                {
                    Cell = c,
                    Distance = Math.Abs(c.Row - cell.Row) + Math.Abs(c.Column - cell.Column)
                })
                .Where(x => x.Distance <= 3)
                .ToList();

            score += neighbors.Count(x => x.Cell.Material!.Type == material.Type) * 12;
            score += Math.Max(0, 18 - neighbors.Count * 3);
            score += cell.Row <= Warehouse.Rows / 2 ? 8 : 4;

            var movementPressure = History.Count(h => h.ToLocation == cell.Location || h.FromLocation == cell.Location);
            score -= Math.Min(20, movementPressure * 2);

            return Math.Clamp(score, 15, 98);
        }

        private void ConfirmPlacement()
        {
            var material = SelectedMaterial ?? SelectedCell?.Material;
            if (SuggestedCell == null || material == null)
            {
                return;
            }

            if (SuggestedCell.HasMaterial)
            {
                SmartPlacementReason = "Предложенная ячейка уже занята. Запустите поиск альтернативы.";
                return;
            }

            if (SelectedCell?.Material?.Id == material.Id)
            {
                SelectedCell.Material = null;
            }

            SuggestedCell.Material = (Material)material.Clone();
            SuggestedCell.Material.Quantity = Math.Max(1, SelectedQuantity);
            AddHistory("Авторазмещение", SuggestedCell.Location, material.Name, material.Quantity, string.Empty, SuggestedCell.Location);
            Notifications.Insert(0, new Notification
            {
                Message = $"Авторазмещение подтверждено: {material.Name}, ячейка {SuggestedCell.Location}",
                Type = NotificationType.PlacementSuggestion,
                Priority = NotificationPriority.Medium,
                Timestamp = DateTime.Now
            });
            ActivityFeed.Insert(0, new ActivityEvent(DateTime.Now, CurrentUser?.Username ?? "system", "подтвердил авторазмещение", material.Name, SuggestedCell.Location, "ИИ"));
            RejectPlacement();
            SelectedCell = SuggestedCell;
            GenerateReport();
            OnPropertyChanged(nameof(OccupiedCellsCount));
        }

        private void RejectPlacement()
        {
            SuggestedCell = null;
            HasPlacementSuggestion = false;
            SmartPlacementScore = 0;
            SmartPlacementReason = "Предложение очищено. Нажмите «Найти лучшую ячейку», чтобы рассчитать новую рекомендацию.";
            ConfirmPlacementCommand?.NotifyCanExecuteChanged();
            RejectPlacementCommand?.NotifyCanExecuteChanged();
        }

        private void ToggleSimulation()
        {
            IsSimulationRunning = !IsSimulationRunning;
            if (IsSimulationRunning)
            {
                _simulationTimer.Start();
                StatusMessage = "Режим симуляции запущен: сгенерированные события хранятся только в памяти.";
            }
            else
            {
                _simulationTimer.Stop();
                StatusMessage = "Режим симуляции остановлен.";
            }
        }

        private void GenerateSimulationEvent()
        {
            var material = AvailableMaterials.OrderBy(_ => _random.Next()).FirstOrDefault();
            if (material == null)
            {
                return;
            }

            var eventTypes = new[] { "перемещён", "обновлён", "риск низкого остатка", "предложение размещения", "отчёт сформирован" };
            var action = eventTypes[_random.Next(eventTypes.Length)];
            var location = Warehouse.Cells.OrderBy(_ => _random.Next()).FirstOrDefault()?.Location ?? "Н/Д";

            SimulationEventsCount++;
            ActivityFeed.Insert(0, new ActivityEvent(DateTime.Now, $"оператор-{_random.Next(1, 5)}", action, material.Name, location, action.Contains("низкого") ? "Высокий" : "Онлайн"));
            Notifications.Insert(0, new Notification
            {
                Message = $"Симуляция: {material.Name} — {action}, ячейка {location}",
                Type = action.Contains("низкого") ? NotificationType.LowStock : NotificationType.SystemWarning,
                Priority = action.Contains("низкого") ? NotificationPriority.High : NotificationPriority.Low,
                Timestamp = DateTime.Now
            });

            if (ActivityFeed.Count > 60)
            {
                ActivityFeed.RemoveAt(ActivityFeed.Count - 1);
            }

            OnPropertyChanged(nameof(UnreadNotificationsCount));
        }

        private void GenerateDemoData()
        {
            if (!AvailableMaterials.Any())
            {
                InitializeDefaultMaterials();
            }

            foreach (var cell in Warehouse.Cells.Where(c => !c.HasMaterial).Take(12))
            {
                var material = AvailableMaterials.OrderBy(_ => _random.Next()).FirstOrDefault();
                if (material != null)
                {
                    cell.Material = (Material)material.Clone();
                }
            }

            for (var i = 0; i < 12; i++)
            {
                GenerateSimulationEvent();
            }

            GenerateReport();
            UpdateForecast();
            StatusMessage = "Демо-данные созданы для режима защиты.";
        }

        private void BuildActivityFeed()
        {
            ActivityFeed.Clear();
            foreach (var item in History.Take(30))
            {
                ActivityFeed.Add(new ActivityEvent(
                    item.Timestamp,
                    string.IsNullOrWhiteSpace(item.UserName) ? "система" : item.UserName,
                    item.ActionType.ToString(),
                    item.MaterialName,
                    string.IsNullOrWhiteSpace(item.ToLocation) ? item.Location : item.ToLocation,
                    "Аудит"));
            }
        }

        private void UpdateQrPreview()
        {
            var material = DetailsMaterial;
            var seed = material?.QrCode ?? material?.Name ?? SelectedCell?.Location ?? "СКЛАД";
            var pixels = BuildQrPixels(seed);

            SelectedQrPixels.Clear();
            foreach (var pixel in pixels)
            {
                SelectedQrPixels.Add(pixel);
            }
        }

        private void GenerateQrPreview()
        {
            var material = DetailsMaterial;
            if (material != null && string.IsNullOrWhiteSpace(material.QrCode))
            {
                material.QrCode = $"MAT-{material.Id}-{DateTime.Now:yyyyMMddHHmmss}";
            }

            UpdateQrPreview();
            OnPropertyChanged(nameof(SelectedQrCodeText));
            StatusMessage = material != null
                ? $"QR-этикетка обновлена для материала «{material.Name}»."
                : $"QR-этикетка обновлена для ячейки {SelectedCellLocation}.";

            Notifications.Insert(0, new Notification
            {
                Message = StatusMessage,
                Type = NotificationType.Info,
                Priority = NotificationPriority.Low,
                Timestamp = DateTime.Now
            });
            OnPropertyChanged(nameof(UnreadNotificationsCount));
        }

        private void CreateBackup()
        {
            var backupDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
            Directory.CreateDirectory(backupDirectory);

            var fileName = $"warehouse_backup_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(backupDirectory, fileName);
            File.WriteAllText(path,
                $"Резервная копия склада{Environment.NewLine}" +
                $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}{Environment.NewLine}" +
                $"Материалов: {AvailableMaterials.Count}{Environment.NewLine}" +
                $"Ячеек всего: {TotalCellsCount}{Environment.NewLine}" +
                $"Занято: {OccupiedCellsCount}{Environment.NewLine}" +
                $"Заполненность: {WarehouseReport.OccupancyPercentage:F1}%");

            Notifications.Insert(0, new Notification
            {
                Message = $"Резервная копия создана: {fileName}",
                Type = NotificationType.BackupCompleted,
                Priority = NotificationPriority.Medium,
                Timestamp = DateTime.Now
            });

            StatusMessage = $"Резервная копия создана: {path}";
            OnPropertyChanged(nameof(UnreadNotificationsCount));
        }

        private void RestoreBackup()
        {
            var result = MessageBox.Show(
                "Восстановление может заменить текущее состояние склада. В этой версии выполняется безопасная демонстрационная проверка без изменения данных. Продолжить?",
                "Подтверждение восстановления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusMessage = "Восстановление отменено пользователем.";
                return;
            }

            StatusMessage = "Проверка восстановления выполнена в безопасном режиме. Реальные данные не изменены.";
            Notifications.Insert(0, new Notification
            {
                Message = "Проверка восстановления backup выполнена в безопасном режиме.",
                Type = NotificationType.SystemWarning,
                Priority = NotificationPriority.Low,
                Timestamp = DateTime.Now
            });
            OnPropertyChanged(nameof(UnreadNotificationsCount));
        }

        private void DeleteOldBackup()
        {
            var backupDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDirectory))
            {
                StatusMessage = "Папка резервных копий пока пуста.";
                return;
            }

            var oldestBackup = new DirectoryInfo(backupDirectory)
                .GetFiles("warehouse_backup_*.txt")
                .OrderBy(f => f.CreationTime)
                .FirstOrDefault();

            if (oldestBackup == null)
            {
                StatusMessage = "Файлы резервных копий не найдены.";
                return;
            }

            oldestBackup.Delete();
            StatusMessage = $"Удалена старая резервная копия: {oldestBackup.Name}";
        }

        private static IEnumerable<QrPixel> BuildQrPixels(string seed)
        {
            var hash = seed.Aggregate(17, (current, ch) => current * 31 + ch);
            for (var row = 0; row < 15; row++)
            {
                for (var col = 0; col < 15; col++)
                {
                    var finder = (row < 4 && col < 4) || (row < 4 && col > 10) || (row > 10 && col < 4);
                    var value = finder || ((row * 23 + col * 19 + hash) % 7 < 3);
                    yield return new QrPixel(value);
                }
            }
        }

        private void UpdateForecast()
        {
            var occupancy = WarehouseReport.OccupancyPercentage;
            ForecastRiskPercent = (int)Math.Clamp(occupancy + LowStockMaterialsCount * 4 + DailyOperationsCount, 12, 96);
            ForecastMessage = ForecastRiskPercent > 75
                ? "Высокий риск перегрузки. Рекомендуется перераспределить зоны и проверить входящие поставки."
                : ForecastRiskPercent > 45
                    ? "Ожидается умеренный рост нагрузки. Авторазмещение поможет снизить перегрузку."
                    : "Ёмкость стабильна. Перегрузка в текущем цикле не прогнозируется.";
        }

        private void InitializePermissions()
        {
            if (CurrentUser == null)
            {
                IsAdmin = false;
                IsStorekeeper = false;
                IsAuditor = false;
                CanEditWarehouse = false;
                CanEditMaterials = false;
                CanViewReports = false;
                CanManageUsers = false;
                CanExportData = false;
                CanImportData = false;
                CanCreateReports = false;
                CanExportReports = false;
                IsReadOnlyMode = false;
                return;
            }

            IsAdmin = CurrentUser.Role == UserRole.Admin;
            IsStorekeeper = CurrentUser.Role == UserRole.Storekeeper;
            IsAuditor = CurrentUser.Role == UserRole.Auditor;

            // Редактирование склада (размеры) - только администратор
            CanEditWarehouse = IsAdmin;

            // Редактирование материалов - администратор и кладовщик
            CanEditMaterials = IsAdmin || IsStorekeeper;

            // Просмотр отчетов - все
            CanViewReports = true;

            // Управление пользователями - только администратор
            CanManageUsers = IsAdmin;

            // ВСЕ операции с данными (экспорт, импорт, отчеты) - администратор и аудитор
            CanExportMaterialsProp = IsAdmin || IsAuditor;  // Экспорт материалов
            CanExportReports = IsAdmin || IsAuditor;        // Экспорт отчетов
            CanImportData = IsAdmin || IsAuditor;           // Импорт
            CanCreateReports = IsAdmin || IsAuditor;        // Шаблоны

            // Режим только для чтения - аудитор (не может редактировать ячейки)
            IsReadOnlyMode = IsAuditor;
        }

        private void LoadHistory()
        {
            try
            {
                using var context = new WarehouseDbContext();
                var historyItems = context.OperationHistory
                    .OrderByDescending(h => h.Timestamp)
                    .Take(50)
                    .ToList();

                History.Clear();
                foreach (var item in historyItems)
                {
                    History.Add(item);
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"Ошибка загрузки истории: {ex.Message}"));
            }
        }

        // ==================== КОМАНДЫ ДЛЯ МАТЕРИАЛОВ ====================

        private void EditMaterial(Material? material)
        {
            if (material == null) return;

            var materialCopy = (Material)material.Clone();
            var editWindow = new EditMaterialWindow(materialCopy)
            {
                Owner = Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Обновляем материал в коллекции
                var index = AvailableMaterials.IndexOf(material);
                if (index >= 0)
                {
                    // Сохраняем ID
                    materialCopy.Id = material.Id;
                    AvailableMaterials[index] = materialCopy;
                }

                // Сохраняем в БД
                try
                {
                    _sqlDataService.SaveMaterial(materialCopy);

                    // Обновляем материал во всех ячейках, где он используется
                    foreach (var cell in Warehouse.Cells)
                    {
                        if (cell.Material?.Id == materialCopy.Id)
                        {
                            cell.Material = (Material)materialCopy.Clone();
                        }
                    }

                    AddHistory("Редактирование материала", "", materialCopy.Name, materialCopy.Quantity);
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                        $"✅ Материал '{materialCopy.Name}' успешно обновлен"));

                    GenerateReport();
                }
                catch (Exception ex)
                {
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка: {ex.Message}"));
                }
            }
        }

        private bool CanEditMaterial(Material? material)
        {
            return material != null && CanEditMaterials;
        }

        private void DeleteMaterial(Material? material)
        {
            if (material == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить материал '{material.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Проверяем, используется ли материал на складе
                var usedInCells = Warehouse.Cells.Where(c =>
                    c.Material != null && c.Material.Id == material.Id).ToList();

                if (usedInCells.Any())
                {
                    result = MessageBox.Show(
                        $"Этот материал используется в {usedInCells.Count} ячейках. Удалить его со склада?",
                        "Материал используется",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Очищаем ячейки с этим материалом
                        foreach (var cell in usedInCells)
                        {
                            cell.Material = null;
                        }
                        AddHistory("Удаление материала со склада", "", material.Name, material.Quantity);
                    }
                    else
                    {
                        return;
                    }
                }

                // Удаляем из коллекции
                AvailableMaterials.Remove(material);

                // Удаляем из БД
                try
                {
                    using var context = new WarehouseDbContext();
                    var dbMaterial = context.Materials.Find(material.Id);
                    if (dbMaterial != null)
                    {
                        context.Materials.Remove(dbMaterial);
                        context.SaveChanges();
                    }

                    AddHistory("Удаление материала", "", material.Name, material.Quantity);
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                        $"✅ Материал '{material.Name}' успешно удален"));

                    GenerateReport();
                }
                catch (Exception ex)
                {
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка удаления: {ex.Message}"));
                    // Восстанавливаем материал в коллекции
                    AvailableMaterials.Add(material);
                }
            }
        }

        private bool CanDeleteMaterial(Material? material)
        {
            return material != null && CanEditMaterials;
        }

        private void StartMaterialDrag(Material? material)
        {
            if (material != null)
            {
                SelectedMaterial = material;
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                    $"Материал «{material.Name}» выбран. Нажмите свободную ячейку на карте или перетащите материал drag and drop."));
            }
        }

        private void GenerateMaterialReport(Material? material)
        {
            if (material == null) return;

            try
            {
                // Находим все ячейки с этим материалом
                var cellsWithMaterial = Warehouse.Cells
                    .Where(c => c.Material != null && c.Material.Id == material.Id)
                    .ToList();

                var totalQuantity = material.Quantity * cellsWithMaterial.Count;

                var message = $"📊 ОТЧЕТ ПО МАТЕРИАЛУ\n\n" +
                             $"Название: {material.Name}\n" +
                             $"Тип: {material.Type}\n" +
                             $"Количество в ячейке: {material.Quantity} {material.Unit}\n" +
                             $"Всего ячеек: {cellsWithMaterial.Count}\n" +
                             $"Общее количество: {totalQuantity} {material.Unit}\n" +
                             $"Занимаемая площадь: {cellsWithMaterial.Count} ячеек\n" +
                             $"Локации: {string.Join(", ", cellsWithMaterial.Select(c => c.Location))}";

                MessageBox.Show(message, "Отчет по материалу",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка: {ex.Message}"));
            }
        }

        private bool CanGenerateMaterialReport(Material? material)
        {
            return material != null && CanViewReports;
        }

        // ==================== КОМАНДЫ ДЛЯ УПРАВЛЕНИЯ ПОЛЬЗОВАТЕЛЯМИ ====================

        private void AddUser()
        {
            var newUser = new User
            {
                Username = $"user_{DateTime.Now.Ticks}",
                Role = UserRole.Auditor, // По умолчанию аудитор
                FullName = "Новый пользователь",
                PasswordHash = _authService.HashPassword("password123"),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            Users.Add(newUser);
            SaveUserToDb(newUser);
        }

        private void EditUser(User? user)
        {
            if (user == null) return;

            // Создаем копию пользователя для редактирования
            var userCopy = new User
            {
                Id = user.Id,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                Role = user.Role,
                FullName = user.FullName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            // Открыть окно редактирования пользователя
            var editWindow = new EditUserWindow(userCopy)
            {
                Owner = Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Обновляем оригинального пользователя данными из копии
                user.Username = userCopy.Username;
                user.Role = userCopy.Role;
                user.FullName = userCopy.FullName;
                user.IsActive = userCopy.IsActive;

                // Если пароль был изменен
                if (!string.IsNullOrWhiteSpace(userCopy.PasswordHash) &&
                    userCopy.PasswordHash != user.PasswordHash)
                {
                    user.PasswordHash = userCopy.PasswordHash;
                }

                SaveUserToDb(user);
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                    $"✅ Пользователь '{user.Username}' обновлен"));
            }
        }

        private void DeleteUser(User? user)
        {
            if (user == null || user.Role == UserRole.Admin ||
                (CurrentUser != null && user.Username == CurrentUser.Username))
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage("❌ Нельзя удалить администратора или текущего пользователя"));
                return;
            }

            var result = MessageBox.Show(
                $"Удалить пользователя '{user.Username}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Users.Remove(user);
                DeleteUserFromDb(user);
            }
        }

        private void OpenUserManagement()
        {
            LoadUsers();

            var userManagementWindow = new UserManagementWindow(this)
            {
                Owner = Application.Current.MainWindow
            };

            userManagementWindow.ShowDialog();
        }

        public void LoadUsers()  // СТАЛО public
        {
            if (!IsAdmin) return;

            try
            {
                using var context = new WarehouseDbContext();
                var users = context.Users.ToList();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка загрузки пользователей: {ex.Message}"));
            }
        }

        private void SaveUserToDb(User user)
        {
            try
            {
                using var context = new WarehouseDbContext();
                if (user.Id == 0)
                {
                    context.Users.Add(user);
                }
                else
                {
                    context.Users.Update(user);
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка сохранения пользователя: {ex.Message}"));
            }
        }

        private void DeleteUserFromDb(User user)
        {
            try
            {
                using var context = new WarehouseDbContext();
                var dbUser = context.Users.Find(user.Id);
                if (dbUser != null)
                {
                    context.Users.Remove(dbUser);
                    context.SaveChanges();
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"✅ Пользователь '{user.Username}' удален"));
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка удаления пользователя: {ex.Message}"));
            }
        }

        // ==================== СУЩЕСТВУЮЩИЕ МЕТОДЫ ====================

        private bool CanSaveData() => CanEditMaterials; // Админ и кладовщик
        private bool CanLoadData() => CanEditMaterials; // Админ и кладовщик
        private bool CanAddNewMaterial() => CanEditMaterials;
        private bool CanDropOnCell(WarehouseCell? cell) => CanEditMaterials && cell != null;
        private bool CanExportMaterials() => CanExportMaterialsProp && AvailableMaterials.Any(); // Только админ
        private bool CanExportReport() => CanExportReports; // Админ и аудитор
        private bool CanImportMaterials() => CanImportData; // Админ и аудитор
        private bool CanCreateTemplate() => CanCreateReports; // Админ и аудитор
        private bool CanGenerateReport() => CanViewReports;

        private void Logout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Выход из системы",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();

                CloseCurrentWindow();
            }
        }

        private void CloseCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow && window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void StartDrag(WarehouseCell? cell)
        {
            if (!CanEditMaterials) return;
            SelectedCell = cell;
        }

        private void DropOnCell(WarehouseCell? targetCell)
        {
            if (targetCell == null || !CanEditMaterials) return;

            if (SelectedMaterial != null)
            {
                if (targetCell.HasMaterial)
                {
                    WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                        $"Ячейка {targetCell.Location} уже занята. Выберите свободную ячейку или переместите материал drag and drop."));
                    return;
                }

                // Добавление нового материала
                var materialToAdd = (Material)SelectedMaterial.Clone();
                materialToAdd.Quantity = SelectedQuantity;
                targetCell.Material = materialToAdd;
                SelectedCell = targetCell;

                AddHistory("Добавление", targetCell.Location, materialToAdd.Name, materialToAdd.Quantity);
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                    $"Добавлен {materialToAdd.Name} в ячейку {targetCell.Location}. Можно продолжить размещение или перетащить материал drag and drop."));

                GenerateReport();
                OnPropertyChanged(nameof(OccupiedCellsCount));
            }
        }

        private void MoveMaterial((WarehouseCell Source, WarehouseCell Target) cells)
        {
            if (!CanEditMaterials) return;

            var (source, target) = cells;

            if (source == null || target == null) return;
            if (source == target) return;
            if (source.Material == null) return;

            if (target.Material != null)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage("❌ Ячейка уже занята!"));
                return;
            }

            target.Material = source.Material;
            source.Material = null;

            AddHistory("Перемещение", target.Location, target.Material!.Name, target.Material.Quantity, source.Location, target.Location);
            WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                $"✅ Перемещен {target.Material.Name} в ячейку {target.Location}"));

            GenerateReport();
            OnPropertyChanged(nameof(OccupiedCellsCount));
        }

        private void SaveData()
        {
            try
            {
                // Отладка
                DebugWarehouseState();

                _sqlDataService.SaveWarehouse(Warehouse);
                AddHistory("Сохранение", "Все", "Склад", 0);
                GenerateReport();
                OnPropertyChanged(nameof(OccupiedCellsCount));
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ Ошибка сохранения: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nВнутренняя ошибка: {ex.InnerException.Message}";
                }
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage(errorMessage));

                // Вывод в консоль для отладки
                Console.WriteLine($"SaveData Error: {ex}");
            }
        }

        private void LoadData()
        {
            try
            {
                var loadedWarehouse = _sqlDataService.LoadWarehouse();
                if (loadedWarehouse != null)
                {
                    // Полностью заменяем склад на загруженный
                    Warehouse = loadedWarehouse;

                    // Обновляем UI
                    OnPropertyChanged(nameof(Warehouse));
                    OnPropertyChanged(nameof(OccupiedCellsCount));

                    AddHistory("Загрузка", "Все", "Склад", 0);
                    GenerateReport();
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка: {ex.Message}"));
            }
        }

        private void AddNewMaterial()
        {
            var newMaterial = new Material
            {
                Name = "Новый материал",
                Type = MaterialType.Other,
                Quantity = 1,
                Unit = "шт."
            };

            AvailableMaterials.Add(newMaterial);
            SelectedMaterial = newMaterial;

            // Сохраняем в БД
            try
            {
                _sqlDataService.SaveMaterial(newMaterial);
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage("✅ Новый материал добавлен"));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка: {ex.Message}"));
            }
        }

        private void EditCell(WarehouseCell? cell)
        {
            if (cell?.Material == null || !CanEditMaterials) return;

            var materialCopy = (Material)cell.Material.Clone();

            var editWindow = new EditMaterialWindow(materialCopy)
            {
                Owner = Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Заменяем весь объект вместо ручного обновления свойств
                cell.Material = materialCopy;

                AddHistory("Редактирование", cell.Location, cell.Material.Name, cell.Material.Quantity);
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                    $"✅ Обновлен материал в ячейке {cell.Location}"));

                GenerateReport();
            }
        }

        private bool CanEditCell(WarehouseCell? cell)
        {
            return cell?.Material != null && CanEditMaterials;
        }

        private void ClearCell(WarehouseCell? cell)
        {
            if (cell?.Material == null || !CanEditMaterials) return;

            var materialName = cell.Material.Name;
            var location = cell.Location;
            cell.Material = null;

            AddHistory("Удаление", location, materialName, 0);
            WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                $"✅ Удален материал из ячейки {location}"));

            GenerateReport();
            OnPropertyChanged(nameof(OccupiedCellsCount));
        }

        private bool CanClearCell(WarehouseCell? cell)
        {
            return cell?.Material != null && CanEditMaterials;
        }

        private void AddHistory(string action, string location, string materialName, int quantity, string? fromLocation = null, string? toLocation = null)
        {
            var material = AvailableMaterials.FirstOrDefault(m => m.Name == materialName);
            var item = new MaterialHistoryItem
            {
                Action = action,
                ActionType = ResolveHistoryActionType(action),
                MaterialId = material?.Id,
                Location = location ?? "Н/Д",
                FromLocation = fromLocation ?? string.Empty,
                ToLocation = toLocation ?? location ?? "Н/Д",
                MaterialName = materialName ?? "Н/Д",
                Quantity = quantity,
                Timestamp = DateTime.Now,
                UserName = CurrentUser?.Username ?? "система"
            };

            History.Insert(0, item);
            ActivityFeed.Insert(0, new ActivityEvent(item.Timestamp, item.UserName, item.ActionType.ToString(), item.MaterialName, item.Location, "Аудит"));

            try
            {
                _sqlDataService.SaveHistoryItem(item);
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка истории: {ex.Message}"));
            }

            // Ограничиваем количество записей истории
            if (History.Count > 50)
            {
                History.RemoveAt(History.Count - 1);
            }

            OnPropertyChanged(nameof(DailyOperationsCount));
            OnPropertyChanged(nameof(MonthlyOperationsCount));
        }

        private static MaterialHistoryActionType ResolveHistoryActionType(string action)
        {
            if (action.Contains("Перемещение", StringComparison.OrdinalIgnoreCase))
            {
                return MaterialHistoryActionType.Moved;
            }

            if (action.Contains("Удал", StringComparison.OrdinalIgnoreCase))
            {
                return MaterialHistoryActionType.Deleted;
            }

            if (action.Contains("Редакт", StringComparison.OrdinalIgnoreCase))
            {
                return MaterialHistoryActionType.Updated;
            }

            if (action.Contains("Созд", StringComparison.OrdinalIgnoreCase) || action.Contains("Добав", StringComparison.OrdinalIgnoreCase))
            {
                return MaterialHistoryActionType.Created;
            }

            return MaterialHistoryActionType.Updated;
        }

        private void GenerateReport()
        {
            if (!CanViewReports) return;

            var report = new WarehouseReport
            {
                ReportDate = DateTime.Now,
                TotalRows = Warehouse.Rows,
                TotalColumns = Warehouse.Columns,
                TotalCells = Warehouse.Rows * Warehouse.Columns
            };

            // Считаем занятые ячейки и группируем материалы
            var materialGroups = new Dictionary<int, (Material Material, List<string> Locations)>();

            foreach (var cell in Warehouse.Cells)
            {
                if (cell.HasMaterial && cell.Material != null)
                {
                    report.OccupiedCells++;

                    if (!materialGroups.ContainsKey(cell.Material.Id))
                    {
                        materialGroups[cell.Material.Id] = (cell.Material, new List<string>());
                    }
                    materialGroups[cell.Material.Id].Locations.Add(cell.Location);
                }
            }

            report.FreeCells = report.TotalCells - report.OccupiedCells;

            // Создаем отчетные элементы
            ReportItems.Clear();
            foreach (var kvp in materialGroups)
            {
                var material = kvp.Value.Material;
                var locations = kvp.Value.Locations;

                var reportItem = new MaterialReportItem
                {
                    Name = material.Name,
                    Type = material.Type,
                    Quantity = material.Quantity * locations.Count,
                    Unit = material.Unit,
                    CellCount = locations.Count,
                    Locations = string.Join(", ", locations.Take(5)) +
                               (locations.Count > 5 ? "..." : "")
                };

                ReportItems.Add(reportItem);
                report.Materials.Add(reportItem);
            }

            WarehouseReport = report;
            OnPropertyChanged(nameof(FreeCellsCount));
            OnPropertyChanged(nameof(LowStockMaterialsCount));
            OnPropertyChanged(nameof(DailyOperationsCount));
            OnPropertyChanged(nameof(MonthlyOperationsCount));
            UpdateForecast();
            WeakReferenceMessenger.Default.Send(new VmNotificationMessage("📊 Отчет обновлен"));
        }

        private void ExportMaterials()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"Материалы_склада_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Экспорт материалов в Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Материалы");

                        // Заголовки
                        worksheet.Cells[1, 1].Value = "Название";
                        worksheet.Cells[1, 2].Value = "Тип";
                        worksheet.Cells[1, 3].Value = "Количество";
                        worksheet.Cells[1, 4].Value = "Ед. измерения";
                        worksheet.Cells[1, 5].Value = "ID";

                        // Стиль заголовков
                        using (var range = worksheet.Cells[1, 1, 1, 5])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        }

                        // Данные
                        int row = 2;
                        foreach (var material in AvailableMaterials)
                        {
                            worksheet.Cells[row, 1].Value = material.Name;
                            worksheet.Cells[row, 2].Value = material.Type.ToString();
                            worksheet.Cells[row, 3].Value = material.Quantity;
                            worksheet.Cells[row, 4].Value = material.Unit;
                            worksheet.Cells[row, 5].Value = material.Id;
                            row++;
                        }

                        // Авторазмер колонок
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        // Сохраняем
                        var fileInfo = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(fileInfo);

                        WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                            $"✅ Экспортировано {AvailableMaterials.Count} материалов"));
                    }
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка экспорта: {ex.Message}"));
            }
        }

        private void ExportReport()
        {
            try
            {
                GenerateReport();

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"Отчет_склада_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Экспорт отчета в Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        // Лист 1: Сводка
                        var summarySheet = package.Workbook.Worksheets.Add("Сводка");
                        summarySheet.Cells[1, 1].Value = "Отчет по складу";
                        summarySheet.Cells[1, 1].Style.Font.Bold = true;
                        summarySheet.Cells[1, 1].Style.Font.Size = 14;

                        summarySheet.Cells[3, 1].Value = "Дата отчета:";
                        summarySheet.Cells[3, 2].Value = WarehouseReport.ReportDate;
                        summarySheet.Cells[4, 1].Value = "Всего рядов:";
                        summarySheet.Cells[4, 2].Value = WarehouseReport.TotalRows;
                        summarySheet.Cells[5, 1].Value = "Всего колонок:";
                        summarySheet.Cells[5, 2].Value = WarehouseReport.TotalColumns;
                        summarySheet.Cells[6, 1].Value = "Всего ячеек:";
                        summarySheet.Cells[6, 2].Value = WarehouseReport.TotalCells;
                        summarySheet.Cells[7, 1].Value = "Занято ячеек:";
                        summarySheet.Cells[7, 2].Value = WarehouseReport.OccupiedCells;
                        summarySheet.Cells[8, 1].Value = "Свободно ячеек:";
                        summarySheet.Cells[8, 2].Value = WarehouseReport.FreeCells;
                        summarySheet.Cells[9, 1].Value = "Заполненность:";
                        summarySheet.Cells[9, 2].Value = $"{WarehouseReport.OccupancyPercentage:F1}%";

                        // Лист 2: Материалы
                        var materialsSheet = package.Workbook.Worksheets.Add("Материалы");
                        materialsSheet.Cells[1, 1].Value = "Название";
                        materialsSheet.Cells[1, 2].Value = "Тип";
                        materialsSheet.Cells[1, 3].Value = "Общее количество";
                        materialsSheet.Cells[1, 4].Value = "Ед. измерения";
                        materialsSheet.Cells[1, 5].Value = "Кол-во ячеек";
                        materialsSheet.Cells[1, 6].Value = "Локации";

                        // Стиль заголовков
                        using (var range = materialsSheet.Cells[1, 1, 1, 6])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                        }

                        // Данные
                        int row = 2;
                        foreach (var item in ReportItems)
                        {
                            materialsSheet.Cells[row, 1].Value = item.Name;
                            materialsSheet.Cells[row, 2].Value = item.Type.ToString();
                            materialsSheet.Cells[row, 3].Value = item.Quantity;
                            materialsSheet.Cells[row, 4].Value = item.Unit;
                            materialsSheet.Cells[row, 5].Value = item.CellCount;
                            materialsSheet.Cells[row, 6].Value = item.Locations;
                            row++;
                        }

                        // Авторазмер колонок
                        summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();
                        materialsSheet.Cells[materialsSheet.Dimension.Address].AutoFitColumns();

                        // Сохраняем
                        var fileInfo = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(fileInfo);

                        WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                            $"✅ Отчет экспортирован"));
                    }
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка экспорта отчета: {ex.Message}"));
            }
        }

        private void ImportMaterials()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                    Title = "Импорт материалов из Excel",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            WeakReferenceMessenger.Default.Send(new VmNotificationMessage("❌ Файл не содержит данных"));
                            return;
                        }

                        int importedCount = 0;
                        int rowCount = worksheet.Dimension?.Rows ?? 0;

                        // Предполагаем, что первая строка - заголовки
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var name = worksheet.Cells[row, 1].Text?.Trim();
                            var typeStr = worksheet.Cells[row, 2].Text?.Trim();
                            var quantityStr = worksheet.Cells[row, 3].Text?.Trim();
                            var unit = worksheet.Cells[row, 4].Text?.Trim();

                            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(quantityStr))
                                continue;

                            if (!int.TryParse(quantityStr, out int quantity) || quantity <= 0)
                                continue;

                            if (!Enum.TryParse<MaterialType>(typeStr, true, out var type))
                                type = MaterialType.Other;

                            var existingMaterial = AvailableMaterials.FirstOrDefault(m =>
                                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                                m.Type == type);

                            if (existingMaterial != null)
                            {
                                // Обновляем существующий материал
                                existingMaterial.Quantity += quantity;
                                _sqlDataService.SaveMaterial(existingMaterial);
                            }
                            else
                            {
                                // Создаем новый материал
                                var newMaterial = new Material
                                {
                                    Name = name,
                                    Type = type,
                                    Quantity = quantity,
                                    Unit = string.IsNullOrWhiteSpace(unit) ? "шт." : unit
                                };

                                AvailableMaterials.Add(newMaterial);
                                _sqlDataService.SaveMaterial(newMaterial);
                            }

                            importedCount++;
                        }

                        WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                            $"✅ Импортировано {importedCount} материалов"));
                        GenerateReport();
                    }
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка импорта: {ex.Message}"));
            }
        }

        private void CreateImportTemplate()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = "Шаблон_импорта_материалов.xlsx",
                    Title = "Создать шаблон для импорта"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Материалы");

                        // Заголовки
                        worksheet.Cells[1, 1].Value = "Название материала";
                        worksheet.Cells[1, 2].Value = "Тип (Cable, Pipe, Tool, Lumber, Metal, Concrete, Insulation, Paint, Other)";
                        worksheet.Cells[1, 3].Value = "Количество (только числа > 0)";
                        worksheet.Cells[1, 4].Value = "Единица измерения (м, шт., кг и т.д.)";

                        // Примеры данных
                        worksheet.Cells[3, 1].Value = "Кабель ВВГнг 3x1.5";
                        worksheet.Cells[3, 2].Value = "Cable";
                        worksheet.Cells[3, 3].Value = 100;
                        worksheet.Cells[3, 4].Value = "м";

                        worksheet.Cells[4, 1].Value = "Труба ППР 20мм";
                        worksheet.Cells[4, 2].Value = "Pipe";
                        worksheet.Cells[4, 3].Value = 50;
                        worksheet.Cells[4, 4].Value = "м";

                        // Стиль
                        using (var range = worksheet.Cells[1, 1, 1, 4])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                        }

                        // Комментарий
                        worksheet.Cells[6, 1].Value = "Примечание:";
                        worksheet.Cells[7, 1].Value = "1. Первая строка - заголовки, не удаляйте её";
                        worksheet.Cells[8, 1].Value = "2. Заполняйте данные со второй строки";
                        worksheet.Cells[9, 1].Value = "3. Название материала - обязательно";
                        worksheet.Cells[10, 1].Value = "4. Количество должно быть больше 0";

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        var fileInfo = new FileInfo(saveFileDialog.FileName);
                        package.SaveAs(fileInfo);

                        WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                            $"✅ Шаблон создан"));
                    }
                }
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new VmNotificationMessage($"❌ Ошибка создания шаблона: {ex.Message}"));
            }
        }

        //ВРЕМЕНО
        private void DebugWarehouseState()
        {
            try
            {
                Console.WriteLine($"=== DEBUG Warehouse State ===");
                Console.WriteLine($"Rows: {Warehouse.Rows}, Columns: {Warehouse.Columns}");
                Console.WriteLine($"Total Cells: {Warehouse.Cells.Count}");

                int occupiedCount = 0;
                foreach (var cell in Warehouse.Cells)
                {
                    if (cell.HasMaterial && cell.Material != null)
                    {
                        occupiedCount++;
                        Console.WriteLine($"  Cell [{cell.Row},{cell.Column}]: {cell.Material.Name} (ID: {cell.Material.Id})");
                    }
                }

                Console.WriteLine($"Occupied Cells: {occupiedCount}");
                Console.WriteLine($"=== END DEBUG ===");

                WeakReferenceMessenger.Default.Send(new VmNotificationMessage(
                    $"Отладка: {occupiedCount} занятых ячеек"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Debug error: {ex.Message}");
            }
        }
    }

    public class VmNotificationMessage
    {
        public string Value { get; }

        public VmNotificationMessage(string value)
        {
            Value = value;
        }
    }

    public class ActivityEvent
    {
        public ActivityEvent(DateTime timestamp, string userName, string action, string target, string location, string severity)
        {
            Timestamp = timestamp;
            UserName = userName;
            Action = action;
            Target = target;
            Location = location;
            Severity = severity;
        }

        public DateTime Timestamp { get; }
        public string UserName { get; }
        public string Action { get; }
        public string Target { get; }
        public string Location { get; }
        public string Severity { get; }
        public string Details => $"{Timestamp:HH:mm:ss}  {UserName}  {Action}  {Target}  {Location}";
    }

    public class QrPixel
    {
        public QrPixel(bool isDark)
        {
            IsDark = isDark;
        }

        public bool IsDark { get; }
    }
}
