using Xunit;
using WarehouseVisualizer.Models;

namespace WarehouseModelsTests
{
    public class MaterialTests
    {
        [Fact]
        public void Clone_ShouldCreateExactCopy()
        {
            // Arrange
            var original = new Material
            {
                Name = "Steel Beam",
                Quantity = 50,
                Type = MaterialType.Metal,
                Unit = "kg"
            };

            // Act
            var copy = (Material)original.Clone();

            // Assert
            Assert.Equal(original.Name, copy.Name);
            Assert.Equal(original.Quantity, copy.Quantity);
            Assert.Equal(original.Type, copy.Type);
            Assert.Equal(original.Unit, copy.Unit);
            Assert.NotSame(original, copy); // Проверка, что это разные объекты
        }
    }
}
