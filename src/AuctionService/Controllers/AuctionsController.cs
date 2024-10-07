using System.Net.Quic;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionRepository _repo;
    private readonly IMapper _mapper;
    // MassTransit
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(IAuctionRepository repo, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _repo = repo;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }


    // date 為 querystring 因此型別為string，若有date則query在date之後的auctions
    [HttpGet]
    public async Task<ActionResult<List<AuctionDTO>>> GetAllActions(string? date)
    {
        // // 加上 .AsQueryable() 為了進一步query
        // var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        // if (!string.IsNullOrEmpty(date))
        // {
        //     // Datetime.CompareTo()
        //     // < 0 : UpdatedAt 早於 date
        //     // == 0 : 相等
        //     // > 0 : UpdatedAt 晚於 date
        //     query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        // }

        // return await query.ProjectTo<AuctionDTO>(_mapper.ConfigurationProvider).ToListAsync();


        // // var auctions = await _context.Auctions
        // //     .Include(x => x.Item)
        // //     .OrderBy(x => x.Item.Make)
        // //     .ToListAsync();

        // // return _mapper.Map<List<AuctionDTO>>(auctions);

        return await _repo.GetAuctionsAsync(date);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDTO>> GetAuctionById(Guid id)
    {
        var auction = await _repo.GetAuctionByIdAsync(id);

        if (auction == null) return NotFound();

        return auction;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDTO>> CreateAuction(CreateAuctionDTO auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);

        // options.TokenValidationParameters.NameClaimType = "username";
        // User.Identity.Name為username
        auction.Seller = User.Identity!.Name;

        // 類似於一個transaction(全部正常運作 或 全部不運作)
        // ----------------------------------------------------------------------
        _repo.AddAuction(auction);

        var newAuction = _mapper.Map<AuctionDTO>(auction);

        // MassTransit: publish a AuctionCreated object
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));
        // ----------------------------------------------------------------------

        // Save Changes to the database
        var result = await _repo.SaveChangesAsync();

        if (!result) return BadRequest("Could not save changes to the DB");

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);

    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDTO updateAuctionDto)
    {
        var auction = await _repo.GetAuctionEntityById(id);

        if (auction == null) return NotFound();

        // TODO: check seller == user (Ch5. 60.)
        if (auction.Seller != User.Identity.Name) return Forbid();

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        // MassTransit
        await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

        var result = await _repo.SaveChangesAsync();

        if (result) return Ok();

        return BadRequest("Problem saving changes");
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _repo.GetAuctionEntityById(id);

        if (auction == null) return NotFound();

        // TODO: check seller == username
        if (auction.Seller != User.Identity!.Name) return Forbid();

        _repo.RemoveAuction(auction);

        // MassTransit
        await _publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

        var result = await _repo.SaveChangesAsync();
        if (!result) return BadRequest("could not update DB");
        return Ok();


    }

}