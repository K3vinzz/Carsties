using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IMapper _mapper;

    public AuctionCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("--> Consuming auction created: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);

        // 範例：
        // 若AuctionCreated的Model為“Foo”時，不會Consume Message
        // 此時會造成MongoDB和Postgres的資料不一致
        if (item.Model == "Foo") throw new ArgumentException("Cannot sell cars with name of Foo");

        await item.SaveAsync();
    }
}
