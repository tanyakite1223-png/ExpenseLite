namespace ExpenseLite.Application.Shared;

public interface IAttachmentStorageService
{
    /// <summary>
    /// 把 stream 存到磁碟，回傳相對路徑（如 "2026/09/guid.pdf"）。
    /// 相對路徑儲存在 DB；完整路徑由實作端組合 base path。
    /// </summary>
    Task<string> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken);

    /// <summary>刪除指定相對路徑的檔案。不存在時靜默忽略。</summary>
    Task DeleteAsync(string storedPath, CancellationToken cancellationToken);

    /// <summary>開啟指定相對路徑的檔案供讀取。找不到時回傳 null。</summary>
    Task<Stream?> OpenReadAsync(string storedPath, CancellationToken cancellationToken);
}
