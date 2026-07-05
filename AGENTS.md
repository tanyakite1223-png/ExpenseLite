# AGENTS.md — ExpenseLite AI 協作入口

> 本檔是進入此 repo 時的專案指令入口。
> 專案的完整協作規範仍以 `CLAUDE.md` 為準，請不要在本檔複製第二份完整規則，以免日後兩邊不同步。

## 啟動流程

1. 先讀 `CLAUDE.md`，並把它視為本 repo 的正式協作規範。
2. 再讀 `.claude/current-handoff.md`，掌握目前專案狀態、架構狀態與下一步。
3. 若 `AGENTS.md` 與 `CLAUDE.md` 有衝突，以本檔的工具行為補充為準；其餘專案規範以 `CLAUDE.md` 為準。

## 工具行為補充

- 對話使用繁體中文。
- 可以接手上一個 session 的成果，不需要假設前後 session 使用同一個工具。
- session handoff 目前仍沿用 `.claude/current-handoff.md`，不要因為工具不同就自行搬移或改名。
- git 指令仍依 `CLAUDE.md` 規定：`git add` / `commit` / `push` 等由 Amber 自己下；AI 只提供建議與 commit message。
- 每次修改 code 或文件後，用 1 到 3 句白話說明改了什麼、為什麼這樣做。
