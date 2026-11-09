# Phase 1: môi trường, tài sản và bài học

Phase 1 đã xong: ghép n8n + Gemini + Jira + Confluence + GitHub + một runner Node + coding agent (agy) + Claude Code thành luồng ticket → PR → merge → deploy Dev → duyệt Production qua Telegram. Phase 2 **không tái sử dụng code phase 1** (quyết định D1); phase 1 chỉ để tham khảo bài học.

## 1. Môi trường máy

| Thành phần | Giá trị (kiểm tra 2026-11-09) |
|---|---|
| Hệ điều hành | Windows 11 |
| Shell | Git Bash và PowerShell |
| .NET SDK | 10.0.302 (còn có 8.0) |
| Node / npm | 24.15 / 11.12 |
| Docker | Docker Desktop, có Kubernetes (context `docker-desktop`, node `desktop-control-plane`) |
| Git | 2.53; chưa cài GitHub CLI (`gh`) |
| Claude Code CLI | `C:/Users/Window/.local/bin/claude` |
| Chụp màn hình kiểm tra | Chrome headless (`chrome.exe --headless=new --screenshot=...`) |

## 2. Tài sản của phase 1 (chỉ đọc để tham khảo)

| Nơi | Nội dung |
|---|---|
| `D:\AI engineer\ai-task-management` | Runner Node (`automation/runner/runner.js`), workflow n8n (`automation/n8n/*.json`), deploy (`deploy/dev`, `deploy/prod/k8s.yaml`), `docs/how-it-works.md`, `docs/runbook.md` |
| `D:\AI engineer\social-media` | Ứng dụng .NET + Angular của phase 1 |
| `D:\AI engineer\workspaces` | Clone dùng chung, worktree từng lần chạy, log, trạng thái deploy |
| `D:\AI engineer\Documents\3.AI Documents\01.AI workflow n8n` | Tài liệu tổng kết (`html.doc`, `md.doc`, có tham chiếu triển khai từng node n8n) |

## 3. Dịch vụ phase 1 có thể đang chạy

Khi Docker Desktop bật, các container và pod của phase 1 tự chạy lại (`restart: unless-stopped`, Deployment của K8s). Runner Node chỉ chạy khi được khởi động tay.

| Dịch vụ | Cổng / vị trí |
|---|---|
| n8n (container `n8n`) | 5678 |
| Runner Node | 8787 |
| Dev (`social-media-dev`) | web 4200, API 5154 |
| Production (namespace `social-media-prod`) | web 4201, API 5155 |

**Rủi ro chính**: workflow n8n phase 1 poll Jira project `SM` (ticket `Approved`) và các PR `ai/*` của repo `social-media` mỗi 2–5 phút. Phase 2 phải dùng Jira project và Confluence space mới; nếu cần, unpublish các workflow phase 1 trong n8n trước khi thử nghiệm.

Dừng phase 1 khi cần (chỉ làm khi user đồng ý):
- n8n: unpublish các workflow, hoặc `docker stop n8n`.
- Runner: dừng tiến trình đang nghe cổng 8787.
- Dev: `docker compose -p social-media-dev down` (trong `ai-task-management/deploy/dev`).
- Production: `kubectl --context docker-desktop delete namespace social-media-prod`.

## 4. Bài học áp dụng cho phase 2

| Mảng | Phase 1 gặp gì | Phase 2 làm gì |
|---|---|---|
| Trạng thái | Trạng thái nằm ở nhãn Jira, marker trong comment, Set trong bộ nhớ runner; restart là mất | Workflow API có database, state machine, idempotency key (`s1`) |
| Coding agent | Treo ở câu hỏi Y/N, không commit ổn định, dừng hỏi cả chi tiết nhỏ | Lớp bọc tự commit, tự kiểm tra; prompt chỉ cho hỏi khi thiếu thông tin chặn việc; chọn động cơ bằng số đo (`s5-5`) |
| Review | Reviewer đổi kết luận khi chỉ có commit gộp main | Kết luận tính bằng code từ phát hiện có cấu trúc (`s5-3`); eval harness để đo độ ổn định |
| Duyệt PR | Giữ lời duyệt qua commit "AI update" | Có push mới là phải duyệt lại (branch protection, `s10-3`) |
| Runner | Chạy trên máy cá nhân với quyền của user, token trong `.env` | Container dùng-một-lần, chặn mạng, không có bí mật Production (`s7`) |
| Bằng chứng | Build/test do runner chạy là đúng hướng | Giữ nguyên nguyên tắc; lưu thành artifact gắn với lần chạy |
| Cấu hình frontend | Địa chỉ API nằm cứng trong bundle, phải build lại image theo môi trường | Đọc `config.json` lúc chạy |
| Prompt injection | Chưa kiểm tra có hệ thống | Bộ ca injection, guardrails, bọc dữ liệu không tin cậy (`s2-4`, `ctx-2`, `gw-3`, `s15-4`) |

## 5. Bẫy kỹ thuật trên máy này

- Git Bash tự đổi đường dẫn khi gọi `docker exec` hay `docker cp`: thêm `MSYS_NO_PATHCONV=1`, dùng đường dẫn Windows cho `docker cp`.
- Sinh file JavaScript qua heredoc hoặc `node -e` dễ mất dấu `\`: viết file bằng công cụ ghi file, kiểm tra bằng `node --check`.
- `node -e` trên Windows hiểu `/tmp` là `D:\tmp`: dùng thư mục scratchpad hoặc đường dẫn tuyệt đối.
- Import workflow n8n bằng CLI (`n8n import:workflow`) làm workflow mất trạng thái Published: phải Publish lại trên giao diện.
- Ảnh chụp màn hình có thể làm lộ token: nhắc user che ô token trước khi chụp.
