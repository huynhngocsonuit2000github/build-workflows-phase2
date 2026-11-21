# Workflow n8n của phase 2

n8n riêng của phase 2 chạy ở `http://localhost:5679` (compose `infra/docker/`). n8n **chỉ điều phối**: nhận sự kiện, gọi Workflow API, rẽ nhánh theo câu trả lời, thông báo. Trạng thái lần chạy nằm ở Workflow API, **không** ở n8n hay nhãn Jira.

## `intake.json`: nhận sự kiện Jira, tạo lần chạy

```
Jira webhook ─► Create execution ─► Created by API? ─ok─► Get current state ─► Route by state ─Created──► Start requirement analysis ─► Respond started
 (POST /webhook    (POST /executions,      │                (GET /executions/{id})        │
  /jira-intake)     Idempotency-Key)       └rejected─► Respond rejected                   └already moving─► Respond duplicate
```

| Bước | Làm gì | Vì sao |
|---|---|---|
| Create execution | `POST /executions` với `Idempotency-Key = jira:<issue>:<X-Atlassian-Webhook-Identifier>` (thiếu header thì dùng `timestamp`) | Cùng một sự kiện Jira luôn ra cùng một key, nên gửi lại bao nhiêu lần cũng chỉ có một lần chạy |
| Created by API? | 201 (mới hoặc trả lại) thì đi tiếp; khác thì `rejected` | Lỗi đầu vào (400, 422) không retry |
| Get current state | Đọc trạng thái **hiện tại** từ API | Câu trả lời của `POST /executions` khi gửi lại là kết quả gốc (`Created`), không phải trạng thái bây giờ |
| Route by state | Còn `Created` thì chuyển sang `RequirementAnalysis`; đã đi tiếp thì báo `duplicate` | Rẽ nhánh theo trạng thái API trả về, không theo nhãn Jira |
| Start requirement analysis | `POST /executions/{id}/transitions` với `expectedVersion` | Hai webhook cùng lúc: một cái 200, cái kia 409, nên vẫn chỉ chuyển một lần |

HTTP node bật retry 3 lần cho lỗi mạng: an toàn vì mọi lời gọi đều idempotent (key hoặc `expectedVersion`).

## Cài và kích hoạt

Trên máy này Docker engine nằm trong WSL (`DOCKER_HOST=tcp://127.0.0.1:2375`), và `docker exec` gọi từ Windows **không in ra output**. Chạy qua WSL:

```bash
wsl -e bash -lc "cd '/mnt/d/AI engineer/build-workflows-phase2' \
  && docker cp workflows/n8n/intake.json platform-n8n-1:/tmp/intake.json \
  && docker exec platform-n8n-1 n8n import:workflow --input=/tmp/intake.json \
  && docker exec platform-n8n-1 n8n publish:workflow --id=wfIntakePhase2v1 \
  && cd infra/docker && docker compose restart n8n"
```

Sửa workflow trên giao diện n8n thì nhớ export lại vào file này (Download) để có version trong git.

## Thử

```bash
curl -s -X POST http://localhost:5679/webhook/jira-intake \
  -H "Content-Type: application/json" -H "X-Atlassian-Webhook-Identifier: wh-001" \
  -d '{"webhookEvent":"jira:issue_updated","timestamp":1763700000000,"issue":{"key":"TAS-4"}}'
```

| Gửi | Kết quả |
|---|---|
| Lần đầu | `{"outcome":"started", ...,"state":"RequirementAnalysis"}` |
| Gửi lại y hệt | `{"outcome":"duplicate", ...}` cùng `executionId` |
| Lần trước tới được API nhưng n8n mất phản hồi, rồi gửi lại | `started`: nhận lại execution cũ, thấy vẫn `Created`, chuyển tiếp |
| Payload không có `issue.key` | `{"outcome":"rejected","apiStatus":400}` |
| 5 webhook giống nhau cùng lúc | Đúng 1 `started`, 4 `duplicate`, DB có 1 lần chạy |

Jira Cloud không gọi được `localhost`; để nhận webhook thật cần một đường hầm (ngrok, Cloudflare Tunnel) hoặc dùng node Jira Trigger/poll. Làm khi tới `s6`.
