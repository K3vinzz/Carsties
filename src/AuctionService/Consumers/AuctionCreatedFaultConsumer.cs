using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

// MassTransit的convention: Consumer class名稱必須以Consumer結尾
// AuctionCreatedFaultConsumer用於處理當建立Auction時在RabbitMQ中的錯誤訊息
public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("--> Consuming faulty creation");

        var exception = context.Message.Exceptions.First();

        // 在範例中，SearchService的AuctionCreatedConsumer在model為"Foo"時會throw ArgumentException
        if (exception.ExceptionType == "System.ArgumentException")
        {
            // 將Message中的Model改為"FooBar"
            context.Message.Message.Model = "FooBar";
            // 將新的Message Publish
            await context.Publish(context.Message.Message);
        }
        else // 其他的錯誤，沒有處理
        {
            Console.WriteLine("Not an argement exception - update error dashboard somewhere");
        }
    }
}
