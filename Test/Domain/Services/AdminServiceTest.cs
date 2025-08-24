using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MinimalApi.Domain.Entities;
using MinimalApi.Domain.Services;
using MinimalApi.Infrastructure.DB;

namespace Test.Domain.Entities;

[TestClass]
public sealed class AdminServiceTest
{
    private AppDbContext CreateTestContext()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var path = Path.GetFullPath(Path.Combine(assemblyPath ?? "", "..", "..", ".."));

        var builder = new ConfigurationBuilder()
            .SetBasePath(path ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        return new AppDbContext(configuration);
    }
    [TestMethod]
    public void TestAdminSave()
    {
        // Arrange
        var context = CreateTestContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Admins");

        var adm = new Admin();
        adm.Id = 1;
        adm.Email = "test@test.com";
        adm.Password = "test";
        adm.Profile = "Adm";

        var admService = new AdminService(context);

        // Act
        admService.Include(adm);

        // Assert
        Assert.AreEqual(1, admService.All(1).Count());
        Assert.AreEqual(1, adm.Id);
        Assert.AreEqual("test@test.com", adm.Email);
        Assert.AreEqual("test", adm.Password);
        Assert.AreEqual("Adm", adm.Profile);
    }

    public void TestAdminSearchId()
    {
        // Arrange
        var context = CreateTestContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Admins");

        var adm = new Admin();
        adm.Email = "test@test.com";
        adm.Password = "test";
        adm.Profile = "Adm";

        var admService = new AdminService(context);

        // Act
        admService.Include(adm);
        var admDb = admService.SearchId(adm.Id) ?? throw new ArgumentNullException();

        // Assert
        Assert.AreEqual(1, admDb.Id);
    }
}
