using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using ZstdSharp.Unsafe;


namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    [HttpGet]
    // [FromQuery] 若不加則會去Request body尋找SearchParams
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
    {
        // 建立 MongoDB 的 query
        // PagedSearch<TProjection, T>
        // The TProjection is the type that the query will be projected into 
        // - in this case the Item (which we are already working with so not really any projection) but is necessary to provide the type.
        // The 'T' type is the original type stored in the collection which again is the Item for our code.
        var query = DB.PagedSearch<Item, Item>();

        // 檢查是否有searchTerm
        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
        }

        // Orders
        query = searchParams.OrderBy switch
        {
            "make" => query.Sort(x => x.Ascending(a => a.Make)),
            "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
            _ => query.Sort(x => x.Ascending(a => a.AuctionEnd)) // 預設排序為auction最接近結束時間
        };

        // Filters
        query = searchParams.FilterBy switch
        {
            "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),  // 已結束
            "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6)
                && x.AuctionEnd > DateTime.UtcNow), // 在六小時內結束
            _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)  // 預設為尚未結束
        };

        // 判斷querystring是否有seller
        if (!string.IsNullOrEmpty(searchParams.Seller))
        {
            query.Match(x => x.Seller == searchParams.Seller);
        }

        // 判斷querystring是否有winner
        if (!string.IsNullOrEmpty(searchParams.Winner))
        {
            query.Match(x => x.Winner == searchParams.Winner);
        }


        // 分頁、每頁資料筆數
        query.PageNumber(searchParams.PageNumber);
        query.PageSize(searchParams.PageSize);

        var result = await query.ExecuteAsync();

        return Ok(new
        {
            results = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount

        });
    }
}
