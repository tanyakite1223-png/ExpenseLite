# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:(尚未開始 — 這是初始狀態)

---

## 上次 session 摘要

- 尚無。專案尚未建立,這是第一個 session。請照 `CLAUDE.md` §1 從零開始。

## 專案現況

- 已完成功能:(無)
- 進行中 / 未完成:(無)
- build & 跑得起來嗎:(專案尚未建立)

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:(尚無;預定依 CLAUDE.md §4——分層、Rich domain、最小核心 Aggregate、Money VO、輕量 Repository)
- `/docs/architecture/` 已有的篇章:(尚無)
- 有無偏離 CLAUDE.md 規範(技術債):(無)

## 待 Amber 決定 / 待辦

- 決定第一個要做的功能(建議從報銷單 + 明細的建立 / 檢視開始,正好帶出 Aggregate 與 Money VO)

## 下一步建議

- 建立 ASP.NET Core(.NET 10)MVC 專案骨架 + `Domain/` `Application/` `Infrastructure/` 資料夾結構
- 設定 EF Core + LocalDB 連線

## git 狀態

- 最後 commit:(尚無)
- 有無未 commit 的變更:(尚無)
