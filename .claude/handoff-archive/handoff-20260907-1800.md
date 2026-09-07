# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔，結束時更新此檔（舊內容歸檔到 `.claude/handoff-archive/`）。
> 功能清單、架構 pattern、設計決策、開發環境詳情 → `.claude/CONTEXT.md`

> 最後更新：2026-09-05 — Sidebar 版型改版 Session 2（Account + Home + Shared）

---

## 現況

**版型改版進行中**，共 5 個 session；Session 1–2 已完成，Session 3 是下一步。

---

## Build 狀態

- `dotnet build` 成功，0 warning / 0 error
- 最新 migration：`AddRequirePasswordChange`（2026-08-16），已套用桌機 DB
- user secrets 正常（`ConnectionStrings:ExpenseLite`、`Identity:SeedPassword`）

---

## 版型改版進度

**設計參考：** `463977.jpg`（專案根目錄，未進 repo）

### Session 1（2026-09-05，完成）
- `wwwroot/css/site.css`：全新 sidebar 設計語言（深海軍藍 #1C2333 + 金色 #C99A3C）
  - `el-*` 元件類別保留作過渡橋接；Bootstrap 相容層同樣保留
- `_Layout.cshtml`：sidebar 結構
- `_LayoutLogin.cshtml`：左深色品牌欄（固定 520px）+ 右白色表單

### Session 2（2026-09-05，完成）
- `Web/Views/Shared/_LoginPartial.cshtml`：刪除（無任何 @Html.Partial 引用）
- `Web/Views/Shared/Error.cshtml`：移除 `el-lede--single`；`el-facts` 補 `el-facts--nowrap`
- `Web/Views/Account/Login.cshtml`：input / button 固定 300px；所有錯誤集中在按鈕上方；移除 `_ValidationScriptsPartial`
- `Web/Views/Account/ChangePassword.cshtml`：`class=""` 空屬性改 `null`
- `Web/Views/Account/AccessDenied.cshtml`：補紅色盾牌 SVG icon；說明改 `el-lede`
- `Web/Views/Home/Index.cshtml`：移除過期規劃注解
- `Web/Controllers/AccountController.cs`：Login POST `!ModelState.IsValid` 改為統一回「帳號或密碼不正確。」
- `wwwroot/css/site.css`：補 `.login-form-body`、`.el-facts--nowrap`、`.el-access-denied-icon`

### Session 3（下一步）
- Projects（3 views）
- Users（3 views）
- ExpenseCategories（2–3 views）

### Sessions 4–5（排隊中）
- Session 4：CashAdvances（6 views）
- Session 5：ExpenseReports（5 views，Details 最複雜）
- 收尾：移除 `el-*` 過渡橋接與 Bootstrap 相容層；補 `.el-page--form` / `.el-page--narrow` 的 min-width

---

## Session 3 注意事項

- **`el-*` 過渡橋接：本 session 不要動**，等所有 view 全改完後一次移除
- **`WarningMessage` TempData**：機制保留但目前無任何使用者，不要清掉
- **已知待補（等全部改完再做）**：`.el-page--form` / `.el-page--narrow` 缺 `min-width`，縮小時部分表單頁會破版

---

## 本檔維護紀律

- 每項資訊只有一個出處，需要交叉引用時用「見 CONTEXT.md §某節」指過去，不要複製內容
- 更新時先刪過期內容，再寫新內容；做完的項目不要留在待辦旁邊
- **不放瞬間狀態**：dev server 開著沒、port 有沒有在聽、有沒有未 commit 變更——這些該現查（`git status` / `Get-Service`）
- 被推翻的決策要留一句「為什麼推翻」（詳細理由寫進 CONTEXT.md 或 `/docs/architecture/`）
