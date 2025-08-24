using MinimalApi.Domain.Entities;

namespace Test.Domain.Entities;

[TestClass]
public sealed class TestAdmin
{
    [TestMethod]
    public void TestGetSetProperties()
    {
        // Arrange
        var adm = new Admin();

        // Act
        adm.Id = 1;
        adm.Email = "test@test.com";
        adm.Password = "test";
        adm.Profile = "Adm";

        // Assert
        Assert.AreEqual(1, adm.Id);
        Assert.AreEqual("test@test.com", adm.Email);
        Assert.AreEqual("test", adm.Password);
        Assert.AreEqual("Adm", adm.Profile);
    }
}
