using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WarehouseVisualizer.Models;
using Microsoft.Data.SqlClient;

namespace WarehouseVisualizer.Services
{
    public class SqlDataService : IDataService
    {
        public SqlDataService()
        {
            // Контекст создается при каждом вызове методов
        }

        public void SaveWarehouse(Warehouse warehouse)
        {
            try
            {
                using var context = new WarehouseDbContext();

                // Для отладки - выводим в Output окно
                System.Diagnostics.Debug.WriteLine("=== Начало сохранения склада ===");
                System.Diagnostics.Debug.WriteLine($"Всего ячеек: {warehouse.Cells.Count}");
                System.Diagnostics.Debug.WriteLine($"Ячеек с материалами: {warehouse.Cells.Count(c => c.Material != null)}");

                // 1. Найти или создать склад
                var dbWarehouse = context.Warehouses.FirstOrDefault();
                if (dbWarehouse == null)
                {
                    System.Diagnostics.Debug.WriteLine("Создаем новый склад...");
                    dbWarehouse = new Warehouse
                    {
                        Rows = warehouse.Rows,
                        Columns = warehouse.Columns
                    };
                    context.Warehouses.Add(dbWarehouse);
                    context.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"Склад создан с ID: {dbWarehouse.Id}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Найден склад ID: {dbWarehouse.Id}");
                }

                // 2. Удалить старые ячейки этого склада
                var oldCells = context.WarehouseCells
                    .Where(c => c.WarehouseId == dbWarehouse.Id)
                    .ToList();

                if (oldCells.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Удаляем {oldCells.Count} старых ячеек...");
                    context.WarehouseCells.RemoveRange(oldCells);
                    context.SaveChanges();
                }

                // 3. Сохранить новые ячейки
                int savedCount = 0;
                foreach (var cell in warehouse.Cells)
                {
                    var newCell = new WarehouseCell
                    {
                        WarehouseId = dbWarehouse.Id,
                        Row = cell.Row,
                        Column = cell.Column
                    };

                    if (cell.Material != null && !string.IsNullOrWhiteSpace(cell.Material.Name))
                    {
                        // Найти материал в БД или создать новый
                        Material dbMaterial = null;

                        if (cell.Material.Id > 0)
                        {
                            dbMaterial = context.Materials.Find(cell.Material.Id);
                        }

                        if (dbMaterial == null)
                        {
                            // Создать новый материал
                            dbMaterial = new Material
                            {
                                Name = cell.Material.Name,
                                Type = cell.Material.Type,
                                Quantity = cell.Material.Quantity,
                                Unit = cell.Material.Unit
                            };
                            context.Materials.Add(dbMaterial);
                            context.SaveChanges();
                            System.Diagnostics.Debug.WriteLine($"Создан материал: {dbMaterial.Name} (ID: {dbMaterial.Id})");
                        }

                        newCell.MaterialId = dbMaterial.Id;
                        savedCount++;
                    }
                    else
                    {
                        // Пустая ячейка - MaterialId останется NULL
                        newCell.MaterialId = null;
                    }

                    context.WarehouseCells.Add(newCell);
                }

                context.SaveChanges();
                System.Diagnostics.Debug.WriteLine($"Сохранено {savedCount} ячеек с материалами");
                System.Diagnostics.Debug.WriteLine("=== Сохранение завершено успешно ===");

                WeakReferenceMessenger.Default.Send(new NotificationMessage(
                    $"✅ Сохранено {savedCount} ячеек"));
            }
            catch (DbUpdateException dbEx)
            {
                // Детальная информация об ошибке
                System.Diagnostics.Debug.WriteLine("=== DbUpdateException ===");
                System.Diagnostics.Debug.WriteLine($"Message: {dbEx.Message}");

                if (dbEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Message: {dbEx.InnerException.Message}");

                    if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"SQL Error #{sqlEx.Number}: {sqlEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Procedure: {sqlEx.Procedure}");
                        System.Diagnostics.Debug.WriteLine($"LineNumber: {sqlEx.LineNumber}");
                    }
                }

                // Показать пользователю
                string errorMsg = "❌ Ошибка БД при сохранении. Проверьте структуру таблиц.";
                WeakReferenceMessenger.Default.Send(new NotificationMessage(errorMsg));

                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex}");
                WeakReferenceMessenger.Default.Send(new NotificationMessage($"❌ Ошибка: {ex.Message}"));
                throw;
            }
        }

        // Вспомогательный метод для получения полного сообщения об ошибке
        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $" -> {inner.Message}";
                inner = inner.InnerException;
            }
            return message;
        }

        public Warehouse LoadWarehouse()
        {
            try
            {
                using var context = new WarehouseDbContext();

                var warehouse = context.Warehouses
                    .Include(w => w.Cells)
                    .ThenInclude(c => c.Material)
                    .OrderByDescending(w => w.Id)
                    .FirstOrDefault();

                if (warehouse == null)
                {
                    // Создаем новый склад по умолчанию
                    warehouse = new Warehouse
                    {
                        Rows = 8,
                        Columns = 8
                    };
                    warehouse.RebuildCells(); // Создаем пустые ячейки
                }
                else
                {
                    // Восстанавливаем структуру ячеек
                    warehouse.RebuildCells();
                }

                WeakReferenceMessenger.Default.Send(new NotificationMessage(
                    "✅ Карта склада загружена"));
                return warehouse;
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new NotificationMessage(
                    $"❌ Ошибка загрузки: {ex.Message}"));
                throw new Exception($"Ошибка загрузки: {ex.Message}");
            }
        }

        public List<Material> LoadMaterials()
        {
            try
            {
                using var context = new WarehouseDbContext();
                return context.Materials.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки материалов: {ex.Message}");
            }
        }

        public void SaveHistoryItem(MaterialHistoryItem historyItem)
        {
            try
            {
                if (historyItem == null) return;

                using var context = new WarehouseDbContext();

                var dbItem = new MaterialHistoryItem
                {
                    Action = historyItem.Action ?? string.Empty,
                    Location = historyItem.Location ?? string.Empty,
                    MaterialName = historyItem.MaterialName ?? string.Empty,
                    Quantity = historyItem.Quantity,
                    Timestamp = historyItem.Timestamp
                };

                context.OperationHistory.Add(dbItem);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения истории: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public void SaveMaterial(Material material)
        {
            if (material == null) return;

            try
            {
                using var context = new WarehouseDbContext();

                Material? dbMaterial = null;

                if (material.Id > 0)
                {
                    // Ищем существующий материал
                    dbMaterial = context.Materials.Find(material.Id);
                }

                if (dbMaterial == null)
                {
                    // Новый материал
                    context.Materials.Add(material);
                }
                else
                {
                    // Обновляем существующий
                    dbMaterial.Name = material.Name;
                    dbMaterial.Type = material.Type;
                    dbMaterial.Quantity = material.Quantity;
                    dbMaterial.Unit = material.Unit;
                    context.Materials.Update(dbMaterial);

                    // Сохраняем ID обратно в исходный объект
                    material.Id = dbMaterial.Id;
                }

                context.SaveChanges();

                // Если это был новый материал, получаем его ID
                if (material.Id == 0 && dbMaterial == null)
                {
                    // Материал должен быть отслеживаем, чтобы получить ID
                    // Пересохраняем для получения ID
                    var savedMaterial = context.Materials
                        .FirstOrDefault(m => m.Name == material.Name && m.Type == material.Type);
                    if (savedMaterial != null)
                    {
                        material.Id = savedMaterial.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения материала '{material.Name}': {ex.Message}", ex);
            }
        }

        public string GetDatabaseStateInfo()
        {
            try
            {
                using var context = new WarehouseDbContext();
                var info = new System.Text.StringBuilder(); // Добавьте System.Text если нужно

                info.AppendLine("=== СОСТОЯНИЕ БАЗЫ ДАННЫХ ===");

                // Проверка таблиц
                try
                {
                    var warehouses = context.Warehouses.ToList();
                    info.AppendLine($"Warehouses: {warehouses.Count} записей");
                    foreach (var w in warehouses)
                    {
                        info.AppendLine($"  ID: {w.Id}, Rows: {w.Rows}, Columns: {w.Columns}");
                    }
                }
                catch (Exception ex) { info.AppendLine($"Warehouses ошибка: {ex.Message}"); }

                try
                {
                    var materials = context.Materials.ToList();
                    info.AppendLine($"\nMaterials: {materials.Count} записей");
                    foreach (var m in materials.Take(5))
                    {
                        info.AppendLine($"  ID: {m.Id}, Name: {m.Name}, Type: {m.Type}");
                    }
                    if (materials.Count > 5) info.AppendLine($"  ... и еще {materials.Count - 5} материалов");
                }
                catch (Exception ex) { info.AppendLine($"Materials ошибка: {ex.Message}"); }

                try
                {
                    var cells = context.WarehouseCells.Include(c => c.Material).ToList();
                    info.AppendLine($"\nWarehouseCells: {cells.Count} записей");
                    foreach (var c in cells.Take(5))
                    {
                        info.AppendLine($"  ID: {c.Id}, Row: {c.Row}, Col: {c.Column}, MaterialId: {c.MaterialId}");
                    }
                    if (cells.Count > 5) info.AppendLine($"  ... и еще {cells.Count - 5} ячеек");
                }
                catch (Exception ex) { info.AppendLine($"WarehouseCells ошибка: {ex.Message}"); }

                info.AppendLine("=== КОНЕЦ ===");
                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Ошибка получения состояния БД: {ex.Message}";
            }
        }
    }

    public interface IDataService
    {
        void SaveWarehouse(Warehouse warehouse);
        Warehouse LoadWarehouse();
        List<Material> LoadMaterials();
        void SaveHistoryItem(MaterialHistoryItem historyItem);
        void SaveMaterial(Material material);
    }

    public class NotificationMessage
    {
        public string Value { get; }

        public NotificationMessage(string value)
        {
            Value = value;
        }
    }
}