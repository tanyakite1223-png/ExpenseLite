# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔，結束時更新此檔（舊內容歸檔到 `.claude/handoff-archive/`）。
> 功能清單、架構 pattern、設計決策、開發環境詳情 → `.claude/CONTEXT.md`

> 最後更新：2026-09-07 — 版型改版全部完成（含收尾）

---

## 現況

**版型改版 Session 1–5 + 收尾全部完成。** `site.css` 已是乾淨狀態，無過渡期包袱。

下一步由 Amber 決定（新功能 / 改既有功能 / 其他）。

---

## Build 狀態

- `dotnet build` 成功，0 warning / 0 error
- 最新 migration：`AddRequirePasswordChange`（2026-08-16），已套用桌機 DB
- user secrets 正常（`ConnectionStrings:ExpenseLite`、`Identity:SeedPassword`）

---

## 版型現況

設計語言：左側固定 sidebar（220px，深海軍藍 #1C2333 + 金色 #C99A3C）+ 右側主內容區（淺灰底）。

`wwwroot/css/site.css` 結構：
- `:root` 色系變數（`--color-*`、`--navy-*` 等）已全部升為正式，無「過渡期」標記
- Components 區塊含 `btn-sm`、`btn-danger`、`btn-outline-danger`（從 Bootstrap 相容層移入）
- Bootstrap 相容層已移除（2026-09-07）
- `.el-page--form`：`max-width: 640px; min-width: 400px`
- `.el-page--narrow`：`max-width: 720px; min-width: 480px`

登入頁獨立版型（`_LayoutLogin.cshtml`）：左深色品牌欄（520px）+ 右白色表單。

---

## 注意事項

- `WarningMessage` TempData：機制保留，無任何使用者，不要清掉
- `expenselite.css`：若此檔存在於本機，可手動刪除（已被 .gitignore 排除，不進 repo）

---

## 本檔維護紀律

- 每項資訊只有一個出處，需要交叉引用時用「見 CONTEXT.md §某節」指過去，不要複製內容
- 更新時先刪過期內容，再寫新內容；做完的項目不要留在待辦旁邊
- **不放瞬間狀態**：dev server 開著沒、port 有沒有在聽、有沒有未 commit 變更——這些該現查（`git status` / `Get-Service`）
- 被推翻的決策要留一句「為什麼推翻」（詳細理由寫進 CONTEXT.md 或 `/docs/architecture/`）
