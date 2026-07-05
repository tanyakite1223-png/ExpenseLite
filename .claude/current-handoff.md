# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-05 — PostgreSQL 開發環境與 git 規則更新

---

## 上次 session 摘要

- Amber 已決定本專案改用 PostgreSQL,並補充開發環境是 Windows 10;目標公司內部全員使用 Mac,正式使用時會由一台 Mac 作為內部主機,安裝 PostgreSQL 並提供 ASP.NET Core HTTPS 服務。Windows 10 開發機目前已用 portable PostgreSQL 18.4 準備本機資料庫環境。git 規則已改為 AI 可協助執行 `git add` / `commit` / `push`,但執行前必須先列出指令與影響範圍並取得 Amber 明確確認。專案尚未建立,請照 `CLAUDE.md` §1 與 §3 從零開始。

## 專案現況

- 已完成功能:(無)
- 進行中 / 未完成:(無)
- build & 跑得起來嗎:(專案尚未建立)
- 開發 DB:portable PostgreSQL 18.4 已放在 `.devtools/postgresql-18.4/`(git ignored),資料目錄在 `.devdata/postgres/`(git ignored),已建立 `expenselite_dev` database 與 `expenselite_app` user。
- 開發環境筆記:`docs/development/postgresql-windows-portable.md`

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:(尚無;預定依 CLAUDE.md §4——分層、Rich domain、最小核心 Aggregate、Money VO、輕量 Repository)
- `/docs/architecture/` 已有的篇章:(尚無)
- 有無偏離 CLAUDE.md 規範(技術債):(無)

## 待 Amber 決定 / 待辦

- 決定第一個要做的功能(建議從報銷單 + 明細的建立 / 檢視開始,正好帶出 Aggregate 與 Money VO)
- 建立專案時使用 PostgreSQL + `Npgsql.EntityFrameworkCore.PostgreSQL`,不要再沿用 SQL Server LocalDB。
- 本機 PostgreSQL 開發腳本在 `scripts/dev/`: `Start-Postgres.ps1`、`Stop-Postgres.ps1`、`Connect-Postgres.ps1`。

## 下一步建議

- 建立 ASP.NET Core(.NET 10)MVC 專案骨架 + `Domain/` `Application/` `Infrastructure/` 資料夾結構
- 設定 EF Core + PostgreSQL 連線

## git 狀態

- 最後 commit:`f5715b9 chore: 準備 PostgreSQL 本機開發環境`
- 有無未 commit 的變更:有;包含 `CLAUDE.md`、`AGENTS.md`、`.claude/current-handoff.md` 的 git 執行規則更新。
