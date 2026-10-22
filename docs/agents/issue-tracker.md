# Issue tracker: GitHub

本儲存庫的議題與規格集中於 `jamesliu69/LinqToStdf` 的 GitHub Issues。
所有議題操作使用 `gh` 命令列工具。

## 操作慣例

- 建立議題：`gh issue create --title "..." --body "..."`
  多行本文可使用 `--body-file <path>`，或以 heredoc 搭配 `--body-file -`。
- 讀取議題與留言：`gh issue view <number> --comments`
- 讀取標籤：`gh issue view <number> --json labels`
- 列出議題：`gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'`
  視需要加入 `--label` 或調整 `--state`。
- 新增留言：`gh issue comment <number> --body "..."`
- 加上標籤：`gh issue edit <number> --add-label "..."`
- 移除標籤：`gh issue edit <number> --remove-label "..."`
- 關閉議題：`gh issue close <number> --comment "..."`

在此儲存庫內執行時，`gh` 會根據 Git remote 判定目標儲存庫。
若從其他目錄執行，請明確加入 `--repo jamesliu69/LinqToStdf`。

## Pull requests as a triage surface

**PRs as a request surface: no.**

## 技能指令的對應操作

- 「發布至議題追蹤系統」：建立 GitHub issue。
- 「取得相關任務」：執行 `gh issue view <number> --comments`。

GitHub 的議題與 PR 共用編號空間。若編號類型不明，先執行
`gh pr view <number>`；確認不是 PR 後，再使用 `gh issue view <number>`。

## Wayfinding operations

供 `/wayfinder` 使用。以一個總覽議題（map）管理子任務議題。

- 總覽議題：套用 `wayfinder:map`，本文包含 Notes、Decisions-so-far 與 Fog。
- 子任務：透過 `gh api` 的 sub-issues 端點連結至總覽議題。
  若無法使用 sub-issues，改在總覽本文維護任務清單，
  並在子任務本文頂端註明 `Part of #<map>`。
- 任務類型：使用 `wayfinder:<type>`，其中 type 為
  `research`、`prototype`、`grilling` 或 `task`。
- 阻擋關係：優先使用 GitHub 原生議題相依關係。
  新增方式為：
  `gh api --method POST repos/jamesliu69/LinqToStdf/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`
  資料庫 ID 由
  `gh api repos/jamesliu69/LinqToStdf/issues/<n> --jq .id`
  取得；不是議題編號，也不是 node_id。
  若無法使用原生相依關係，在子任務本文頂端記錄
  `Blocked by: #<n>, #<n>`。所有阻擋議題關閉後，任務才算解除阻擋。
- 選取下一個任務：依總覽中的順序，選取第一個仍開啟、
  沒有未解決阻擋關係且尚未指派負責人的子任務。
  原生相依關係以 `issue_dependencies_summary.blocked_by > 0`
  判斷是否仍受阻擋。
- 認領：`gh issue edit <n> --add-assignee @me`。
  認領應是該工作階段的第一個寫入操作。
- 完成：先留言記錄結果，再關閉子任務，
  最後將摘要與連結加入總覽的 Decisions-so-far。
