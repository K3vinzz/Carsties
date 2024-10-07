using System;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

public class AuctionRepository : IAuctionRepository
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;

    public AuctionRepository(AuctionDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    public void AddAuction(Auction auction)
    {
        _context.Auctions.Add(auction);
    }

    public async Task<List<AuctionDTO>> GetAuctionsAsync(string? date)
    {
        // 加上 .AsQueryable() 為了進一步query
        var query = _context.Auctions.OrderBy(x => x.Item!.Make).AsQueryable();

        if (!string.IsNullOrEmpty(date))
        {
            // Datetime.CompareTo()
            // < 0 : UpdatedAt 早於 date
            // == 0 : 相等
            // > 0 : UpdatedAt 晚於 date
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }

        return await query.ProjectTo<AuctionDTO>(_mapper.ConfigurationProvider).ToListAsync();
    }

    public async Task<AuctionDTO?> GetAuctionByIdAsync(Guid id)
    {
        return await _context.Auctions
        .ProjectTo<AuctionDTO>(_mapper.ConfigurationProvider)
        .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Auction?> GetAuctionEntityById(Guid id)
    {
        return await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public void RemoveAuction(Auction auction)
    {
        _context.Auctions.Remove(auction);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
