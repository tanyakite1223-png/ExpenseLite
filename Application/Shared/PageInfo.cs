namespace ExpenseLite.Application.Shared;

public static class PagingDefaults
{
    public const int PageSize = 20;
}

/// <summary>
/// 列表分頁的頁面資訊。<see cref="TotalItemCount"/> 是「篩選後」的總筆數，
/// 用來算總頁數，也是列表頁「共 N 筆」文字的資料來源。
/// </summary>
public sealed record PageInfo(int PageNumber, int PageSize, int TotalItemCount)
{
    public int TotalPages => TotalItemCount == 0 ? 1 : (int)Math.Ceiling(TotalItemCount / (double)PageSize);

    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// 把「使用者從 querystring 帶進來的頁碼」夾到合法範圍內（&lt; 1 或超過總頁數時夾回邊界），
    /// 這樣手改網址帶一個超大頁碼也不會回傳空清單。
    /// </summary>
    public static PageInfo Create(int requestedPageNumber, int totalItemCount, int pageSize = PagingDefaults.PageSize)
    {
        var totalPages = totalItemCount == 0 ? 1 : (int)Math.Ceiling(totalItemCount / (double)pageSize);
        var pageNumber = Math.Clamp(requestedPageNumber < 1 ? 1 : requestedPageNumber, 1, totalPages);
        return new PageInfo(pageNumber, pageSize, totalItemCount);
    }
}
