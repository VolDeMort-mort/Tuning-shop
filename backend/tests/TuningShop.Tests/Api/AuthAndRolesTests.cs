using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;

namespace TuningShop.Tests.Api;

/// <summary>FR-01 і NFR-02 через HTTP (AC-01, AC-21).</summary>
public sealed class AuthAndRolesTests : IAsyncLifetime
{
    private ApiFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new ApiFactory(ApiFactory.NewDatabasePath());
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        ApiFactory.DeleteDatabase(_factory.DatabasePath);
    }

    [Fact]
    public async Task AC01_ValidLogin_ReturnsRole_InvalidLoginRejected()
    {
        var client = _factory.CreateClient();
        var ok = await client.PostAsJsonAsync("/api/auth/login", new { login = "manager", password = "manager123" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Manager", (await ok.Content.ReadFromJsonAsync<CurrentUserDto>(ApiFactory.Json))!.Role.ToString());

        var bad = await client.PostAsJsonAsync("/api/auth/login", new { login = "manager", password = "wrong" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal("invalid_credentials", (await bad.Content.ReadFromJsonAsync<ApiError>(ApiFactory.Json))!.Code);
    }

    [Theory]
    [InlineData("/api/orders/current")]
    [InlineData("/api/clients?query=1")]
    [InlineData("/api/master/works")]
    public async Task AC01_WithoutLogin_Only_TrackingIsAvailable(string url)
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/tracking?number=X&phone=Y")).StatusCode);
    }

    [Theory]
    [InlineData("master1", "master123", "GET", "/api/orders/current")]
    [InlineData("master1", "master123", "POST", "/api/clients")]
    [InlineData("director", "director123", "POST", "/api/clients")]
    [InlineData("director", "director123", "POST", "/api/orders/1/start-work")]
    [InlineData("director", "director123", "GET", "/api/master/works")]
    [InlineData("manager", "manager123", "GET", "/api/master/works")]
    public async Task AC01_AC21_ForeignRoleFunction_Returns403(string login, string password, string method, string url)
    {
        var client = await _factory.LoginAsync(login, password);
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? JsonContent.Create(new { }) : null
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden", (await response.Content.ReadFromJsonAsync<ApiError>(ApiFactory.Json))!.Code);
    }

    [Fact]
    public async Task AC01_DirectorSeesListsAndCards()
    {
        var client = await _factory.LoginAsync("director", "director123");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/orders/current")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/orders/completed")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/clients?query=Бондар")).StatusCode);
    }

    [Fact]
    public async Task AC21_PasswordsStoredOnlyAsHash_SessionExpiresAfter30MinutesIdle()
    {
        _factory.CreateClient(); // запуск сервера створює облікові записи
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TuningDbContext>();
        var employees = await db.Employees.ToListAsync();
        Assert.Equal(4, employees.Count);
        foreach (var (_, login, password, _) in DbInitializer.DefaultEmployees)
        {
            var hash = employees.Single(e => e.Login == login).PasswordHash;
            Assert.DoesNotContain(password, hash);
            Assert.StartsWith("AQAAAA", hash); // формат PasswordHasher (PBKDF2)
        }

        var cookie = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.Equal(TimeSpan.FromMinutes(30), cookie.ExpireTimeSpan);
        Assert.True(cookie.SlidingExpiration);
        Assert.True(cookie.Cookie.HttpOnly);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await _factory.LoginAsync("manager", "manager123");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}
