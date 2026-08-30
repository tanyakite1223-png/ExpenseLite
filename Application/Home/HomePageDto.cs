namespace ExpenseLite.Application.Home;

public sealed record HomePageDto(
    string DisplayName,
    bool IsManager,
    HomeTodoDto? Lede,
    IReadOnlyList<HomeTodoDto> Items);

/// <param name="Count">數字，畫面上是大字。</param>
/// <param name="What">「張報銷單等你審核」——接在數字後面唸得通的句子。</param>
/// <param name="Why">一句補充，讓人知道急不急。</param>
/// <param name="Url">點過去的目標，含篩選參數。</param>
/// <param name="NeedsAttention">true 會轉洋紅：作廢沒收拾、退回要改這類。</param>
public sealed record HomeTodoDto(
    int Count,
    string What,
    string Why,
    string Url,
    bool NeedsAttention);
