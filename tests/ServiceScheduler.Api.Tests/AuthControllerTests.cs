using System.Net;
using System.Net.Http.Json;
using ServiceScheduler.Shared.DTOs;

namespace ServiceScheduler.Api.Tests;

public class AuthControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthControllerTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ReturnsOkWithToken_ForValidCredentials()
    {
        var dto = new LoginDto { Email = "newuser@example.com", Password = "Password123!" };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", dto);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.Equal("newuser@example.com", result.Email);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyRegistered()
    {
        var dto = new LoginDto { Email = "duplicate@example.com", Password = "Password123!" };
        await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", dto);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsOkWithToken_ForValidCredentials()
    {
        var dto = new LoginDto { Email = "logintest@example.com", Password = "Password123!" };
        await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", dto);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", dto);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(result!.Token));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        var dto = new LoginDto { Email = "wrongpass@example.com", Password = "CorrectPassword!" };
        await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", dto);

        var badDto = new LoginDto { Email = "wrongpass@example.com", Password = "WrongPassword!" };
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", badDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_ForInvalidEmailFormat()
    {
        var dto = new LoginDto { Email = "not-an-email", Password = "Password123!" };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
