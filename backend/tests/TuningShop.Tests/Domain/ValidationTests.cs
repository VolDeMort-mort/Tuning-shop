using TuningShop.Api.Domain;

namespace TuningShop.Tests.Domain;

/// <summary>Модульні перевірки форматів NFR-03 (AC-22) та правил FR-02, FR-04, FR-07, FR-20.</summary>
public sealed class ValidationTests
{
    [Theory]
    [InlineData("+380501112233", true)]
    [InlineData("+38050111223", false)]   // 8 цифр після коду
    [InlineData("0501112233", false)]     // без +380
    [InlineData("+380 50 111 22 33", true)] // пробіли прибираються нормалізацією
    public void Phone_Format(string input, bool valid) =>
        Assert.Equal(valid, Validation.IsValidPhone(Validation.NormalizePhone(input)));

    [Theory]
    [InlineData("WVWZZZAUZKW123456", true)]
    [InlineData("wvwzzzauzkw123456", true)]  // нормалізується до верхнього регістру
    [InlineData("WVWZZZAUZKW12345", false)]  // 16 символів
    [InlineData("WVWZZZAUZKW1234567", false)] // 18 символів
    [InlineData("WVWZZZAUZKI123456", false)] // літера I заборонена
    [InlineData("WVWZZZAUZKO123456", false)] // літера O заборонена
    [InlineData("WVWZZZAUZKQ123456", false)] // літера Q заборонена
    public void Vin_Format(string input, bool valid) =>
        Assert.Equal(valid, Validation.IsValidVin(Validation.NormalizeVin(input)));

    [Fact]
    public void Client_RequiredFieldsAndFormats()
    {
        Assert.Empty(Validation.ValidateClient("Бондар Андрій", "+380501112233", null));
        var errors = Validation.ValidateClient(" ", "", "not-an-email");
        Assert.Equal(["email", "fullName", "phone"], errors.Keys.Order());
    }

    [Fact]
    public void Car_YearRangeAndRequiredFields()
    {
        var errors = Validation.ValidateCar("", "", 1899, "SHORT", "", 2026);
        Assert.Equal(["make", "model", "plateNumber", "vin", "year"], errors.Keys.Order());
        Assert.Contains("year", Validation.ValidateCar("BMW", "M3", 2028, "WBS8M9C50M5K12345", "AA1111AA", 2026).Keys);
        Assert.Empty(Validation.ValidateCar("BMW", "M3", 2027, "WBS8M9C50M5K12345", "AA1111AA", 2026));
    }

    [Theory]
    [InlineData(0, true)]      // межа: нульова вартість допустима
    [InlineData(-0.01, false)] // менше нуля
    [InlineData(10.5, true)]
    [InlineData(10.555, false)] // більше двох знаків після коми
    public void Work_CostBoundary(double cost, bool valid) =>
        Assert.Equal(valid, Validation.ValidateWork(WorkCategory.Other, "Опис", (decimal)cost).Count == 0);

    [Fact]
    public void Work_RequiresCategoryAndDescription()
    {
        var errors = Validation.ValidateWork(null, "  ", null);
        Assert.Equal(["category", "description", "estimatedCost"], errors.Keys.Order());
        Assert.Contains("category", Validation.ValidateWork((WorkCategory)99, "Опис", 1m).Keys);
    }

    [Theory]
    [InlineData("Койловери", 1, true)]
    [InlineData("Койловери", 0, false)]
    [InlineData("", 2, false)]
    public void Part_NameAndQuantity(string name, int quantity, bool valid) =>
        Assert.Equal(valid, Validation.ValidatePart(name, quantity).Count == 0);
}
