using System.Net;
using System.Net.Http.Json;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Application.Features.Identity;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Application.Features.Identity.Queries.GenerateTokens;
using DentalClinic.Application.Features.Identity.Queries.RefreshTokens;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class IdentityControllerTests(WebAppFactory webAppFactory)
{
    private readonly AppHttpClient _client = webAppFactory.CreateAppHttpClient();

    // -------------------------------------------------------------------------
    // 1. POST /api/v1.0/identity/token/generate
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GenerateToken_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var request = new GenerateTokenQuery(
            TestUsers.Admin.Email!,
            TestUsers.AdminPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/identity/token/generate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task GenerateToken_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var request = new GenerateTokenQuery(
            TestUsers.Admin.Email!,
            "WrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/identity/token/generate", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 2. POST /api/v1.0/identity/token/refresh-token
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshToken_WithActiveAccessToken_ShouldReturnConflict()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var generateRequest = new GenerateTokenQuery(
            TestUsers.Admin.Email!,
            TestUsers.AdminPassword);

        var generateResponse = await _client.PostAsJsonAsync("/api/v1.0/identity/token/generate", generateRequest);
        var initialTokens = await generateResponse.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(initialTokens);

        var refreshRequest = new RefreshTokenQuery(
            initialTokens!.AccessToken!,
            initialTokens.RefreshToken!);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/identity/token/refresh-token", refreshRequest);

        // Assert - ينتهي بـ Conflict لأن التوكن لم ينتهِ بعد
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ShouldReturnConflict()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var generateRequest = new GenerateTokenQuery(
            TestUsers.Admin.Email!,
            TestUsers.AdminPassword);

        var generateResponse = await _client.PostAsJsonAsync("/api/v1.0/identity/token/generate", generateRequest);
        var initialTokens = await generateResponse.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(initialTokens);

        var refreshRequest = new RefreshTokenQuery(
            initialTokens!.AccessToken!,
            "InvalidRefreshTokenValue");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/identity/token/refresh-token", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidAccessToken_ShouldReturnConflict()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var refreshRequest = new RefreshTokenQuery(
            "InvalidAccessTokenFormat",
            "SomeRefreshTokenValue");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/identity/token/refresh-token", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 3. GET /api/v1.0/identity/current-user/claims
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCurrentUserInfo_WithAuthenticatedUser_ShouldReturnUserInfo()
    {
        // Arrange
        await webAppFactory.ResetDatabaseAsync();
        await EnsureUserExistsAsync();

        var token = await _client.GenerateTokenAsync(TestUsers.Admin);
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/identity/current-user/claims");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AppUserDto>();
        Assert.NotNull(result);
        Assert.Equal(TestUsers.Admin.Id, result!.UserId);
    }

    [Fact]
    public async Task GetCurrentUserInfo_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/identity/current-user/claims");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private async Task EnsureUserExistsAsync()
    {
        using var scope = webAppFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByIdAsync(TestUsers.Admin.Id);
        if (user is null)
        {
            await userManager.CreateAsync(TestUsers.Admin, TestUsers.AdminPassword);
        }
    }
}