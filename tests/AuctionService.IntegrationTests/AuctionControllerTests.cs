using System;
using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

// IClassFixture<CustomWebAppFactory> : 用於share CustomWebAppFactory，已達到share postgres container, MassTransit
// IAsyncLifetime :  InitializeAsync會在每個Test前執行，DisposeAsync會在每個Test後執行
[Collection("Shared collection")]
public class AuctionControllerTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly string GT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

    public AuctionControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuctions_ShouldReturn3Auctions()
    {
        // arrange? (Don't need)

        // act
        var response = await _httpClient.GetFromJsonAsync<List<AuctionDTO>>("api/auctions");

        // assert
        Assert.Equal(3, response!.Count);
    }

    [Fact]
    public async Task GetAuctionsById_WithValidId_ShouldReturnTheAuction()
    {
        // arrange? (Don't need)

        // act
        var response = await _httpClient.GetFromJsonAsync<AuctionDTO>($"api/auctions/{GT_ID}");

        // assert
        Assert.Equal("GT", response!.Model);
    }

    [Fact]
    public async Task GetAuctionsById_WithInvalidId_ShouldReturn404()
    {
        // arrange? (Don't need)

        // act
        var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response!.StatusCode);
    }

    [Fact]
    public async Task GetAuctionsById_WithInvalidGuid_ShouldReturn400()
    {
        // arrange? (Don't need)

        // act
        var response = await _httpClient.GetAsync($"api/auctions/NotAGuid");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response!.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithNoAuth_ShouldReturn401()
    {
        // arrange?
        var auction = new CreateAuctionDTO { Make = "test" };

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response!.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithAuth_ShouldReturn201()
    {
        // arrange
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);
        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDTO>();

        // assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("bob", createdAuction?.Seller);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
    {
        // arrange
        var auction = GetAuctionForCreate();
        auction.Make = null;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync("api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
    {
        // arrange
        var auctionDto = GetAuctionForUpdate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", auctionDto);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
    {
        // arrange 
        var auctionDto = GetAuctionForUpdate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("notBob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", auctionDto);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    // 不需要Initialize
    public Task InitializeAsync() => Task.CompletedTask;

    // 每次test結束都要重置db的資料
    public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope(); // 取得當前test的scope
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }


    private static CreateAuctionDTO GetAuctionForCreate()
    {
        return new CreateAuctionDTO
        {
            Make = "test",
            Model = "testModel",
            ImageUrl = "test",
            Color = "test",
            Mileage = 10,
            Year = 10,
            ReservePrice = 10,
            AuctionEnd = new DateTime(),
        };
    }

    private static UpdateAuctionDTO GetAuctionForUpdate()
    {
        return new UpdateAuctionDTO
        {
            Make = "test",
            Model = "testModel",
            Year = 10,
            Color = "test",
            Mileage = 10,
        };
    }

}
