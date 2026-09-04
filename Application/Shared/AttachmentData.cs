namespace ExpenseLite.Application.Shared;

public sealed record AttachmentData(Stream Stream, string OriginalFileName)
{
    public string Extension => Path.GetExtension(OriginalFileName);
}
