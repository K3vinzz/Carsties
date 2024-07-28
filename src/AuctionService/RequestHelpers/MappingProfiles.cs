using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;

namespace AuctionService.RequestHelpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // CreateMap<TSource, TDestination>()
        //    .ForMember(destinationMember => destinationMember.SourceMember, 
        //               memberOptions => memberOptions.MapFrom(source => source.SourceMember));

        CreateMap<Auction, AuctionDTO>().IncludeMembers(x => x.Item);
        CreateMap<Item, AuctionDTO>();
        CreateMap<CreateAuctionDTO, Auction>()
            .ForMember(d => d.Item, o => o.MapFrom(s => s));
        CreateMap<CreateAuctionDTO, Item>();

        // 連結AuctionService 和 SearchService
        // AuctionCreated 作為 AuctionService 和 SearchService 的橋樑
        CreateMap<AuctionDTO, AuctionCreated>();
        CreateMap<Auction, AuctionUpdated>().IncludeMembers(a => a.Item);
        CreateMap<Item, AuctionUpdated>();
    }
}
