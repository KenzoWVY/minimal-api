using MinimalApi.Domain.Entities;

namespace Test.Domain.Entities;

[TestClass]
public sealed class TestVehicle
{
    [TestMethod]
    public void TestGetSetProperties()
    {
        // Arrange
        var vehicle = new Vehicle();

        // Act
        vehicle.Id = 1;
        vehicle.Name = "testName";
        vehicle.CarBrand = "testBrand";
        vehicle.Year = 2000;

        // Assert
        Assert.AreEqual(1, vehicle.Id);
        Assert.AreEqual("testName", vehicle.Name);
        Assert.AreEqual("testBrand", vehicle.CarBrand);
        Assert.AreEqual(2000, vehicle.Year);
    }
}
