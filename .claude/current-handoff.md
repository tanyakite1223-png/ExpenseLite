# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-12 — 筆電 PostgreSQL 環境已修復

---

## 上次 session 摘要

- Amber 要的是「直接把這台筆電的 PostgreSQL 修到可用」,不是只留筆記。
- 這台筆電目前採用 Scoop PostgreSQL 18.4 binaries,固定 data directory 為 `.localdb/postgres/data`。
- PostgreSQL 已正式啟動在 `127.0.0.1:5432`,並已驗證:
  - `pg_isready -h 127.0.0.1 -p 5432` 回 `accepting connections`
  - `postgres` 管理者可用密碼登入
  - `expenselite_app` 可登入 `expenselite_dev`
  - `expenselite_app` 可 create/drop table,後續 EF Core migration 有基本建表權限
  - `pg_hba.conf` 的 local / IPv4 / IPv6 local connection 都使用 `scram-sha-256`
  - `postgres` 與 `expenselite_app` 的密碼 hash 都是 `SCRAM-SHA-256`
- 原本的 `C:\Users\icq98\scoop\persist\postgresql\data` 不再作為本 repo 開發用 data directory,因為先前曾遇到 `postmaster.pid` permission 問題。
- `.NET SDK 10.0.301` 已透過 Scoop 安裝且本體可用;但裸 `dotnet` 指令目前仍會先解析到 `C:\Program Files\dotnet\dotnet.exe`,該位置只有 runtime。非系統管理員 shell 無法修改 Machine PATH,若要永久讓裸 `dotnet` 指到 Scoop SDK,需用系統管理員權限調整 Machine PATH。

## 專案現況

- 已完成功能:(無)
- 進行中 / 未完成:這台筆電的本機 PostgreSQL 已可用; `.NET SDK` 裸指令 PATH 還未永久修正
- build & 跑得起來嗎:這台 checkout 尚未合併桌機上的 ASP.NET Core MVC 專案骨架
- 可用的開發工具:`git` 可用; `dotnet-sdk 10.0.301` 已安裝且可用; PostgreSQL 18.4 binaries 可用
- 本機 PostgreSQL 目前的關鍵狀態:
  - 固定 data 目錄:`.localdb/postgres/data`
  - host / port:`127.0.0.1:5432`
  - database:`expenselite_dev`
  - application user:`expenselite_app`
  - auth method:`scram-sha-256`
- `docs/development/postgresql-windows-portable.md` 已補上這台筆電的 Scoop PostgreSQL + `.localdb` 落點。
- Amber 同步在桌機用 Codex 開發,桌機那邊的 ASP.NET Core MVC 專案骨架已就緒;這台筆電先不要重建骨架,等桌機 branch 合併進來。

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:(尚無;仍是 CLAUDE.md §4 的預定方向)
- `/docs/architecture/` 已有的篇章:(尚無)
- 有無偏離 CLAUDE.md 規範(技術債):無新增架構技術債;目前只是在處理本機環境

## 待 Amber 決定 / 待辦

- 若要讓裸 `dotnet` 在任何新 shell 都直接抓到 Scoop SDK,需用系統管理員 PowerShell 把 `C:\Users\icq98\scoop\apps\dotnet-sdk\current` 放到 Machine PATH 的 `C:\Program Files\dotnet\` 前面。
- 等桌機 branch 合併後,在這台筆電驗證專案 build / run / EF Core 連線。

## 下一步建議

- 把這台筆電的環境修復變更 commit 到獨立 branch,再由桌機合併。
- 合併桌機開發成果後,用這台筆電的 PostgreSQL 跑一次完整 build / run。

## git 狀態

- 最後 commit:`f5715b9 chore: 準備 PostgreSQL 本機開發環境`
- 有無未 commit 的變更:有;包含筆電 PostgreSQL scripts / 文件 / handoff 更新,準備 commit 到環境修復 branch
