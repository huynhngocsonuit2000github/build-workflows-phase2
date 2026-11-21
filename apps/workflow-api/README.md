# Workflow API

Nguồn sự thật về trạng thái một lần chạy (`WorkflowExecution`): luật chuyển trạng thái, lịch sử, optimistic concurrency. Thiết kế: `docs/architecture/workflow-states.md`.

## Cấu trúc

```
apps/workflow-api/
├── WorkflowApi.slnx
├── src/WorkflowApi.Domain/        # logic thuần, không DB: state machine, bảng chuyển, lịch sử
│   ├── WorkflowStateMachine.cs    # hàm Apply: chỗ DUY NHẤT đổi trạng thái
│   ├── TransitionTable.cs         # bảng chuyển hợp lệ (dữ liệu)
│   └── WorkflowExecution.cs       # thực thể (setter internal)
├── src/WorkflowApi.Api/           # ASP.NET minimal API + EF Core + PostgreSQL
│   ├── Endpoints/ExecutionEndpoints.cs
│   ├── Data/WorkflowDbContext.cs  # Version là concurrency token
│   └── Migrations/
└── tests/
    ├── WorkflowApi.Domain.Tests/  # test logic, không cần Docker
    └── WorkflowApi.Api.Tests/     # test API + PostgreSQL thật (Testcontainers, cần Docker)
```

## Chạy test

```bash
dotnet test WorkflowApi.slnx
```

`WorkflowApi.Api.Tests` tự khởi động một container PostgreSQL (Testcontainers), nên cần Docker đang chạy. Mỗi test dùng một database riêng.

## Chạy API ở máy

1. Bật PostgreSQL: trong `infra/docker/`, copy `.env.example` thành `.env`, đổi mật khẩu, rồi `docker compose up -d`.
2. Đặt chuỗi kết nối bằng user-secrets (không ghi vào file):
   ```bash
   cd apps/workflow-api/src/WorkflowApi.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=workflow;Username=workflow;Password=<mật khẩu trong .env>"
   dotnet run
   ```
   API chạy ở `http://localhost:5300` và tự áp migration lúc khởi động.

## Endpoint

| Method | Route | Ý nghĩa | Mã trả về |
|---|---|---|---|
| POST | `/executions` | Tạo lần chạy; header `Idempotency-Key` bắt buộc (xem bên dưới) | 201, 400, 422 |
| GET | `/executions/{id}` | Trạng thái hiện tại và `version` | 200, 404 |
| GET | `/executions/{id}/history` | Lịch sử theo thời gian | 200, 404 |
| POST | `/executions/{id}/transitions` | Chuyển trạng thái: `toState`, `actor`, `expectedVersion`, `reason` | 200, 400, 403, 404, 409, 422 |

### Idempotency của `POST /executions`

| Tình huống | Kết quả |
|---|---|
| Key mới | Tạo lần chạy, 201 |
| Key đã dùng, cùng nội dung (đã cắt khoảng trắng) | Trả lại nguyên văn kết quả lần đầu, 201, header `Idempotency-Replayed: true`; không tạo thêm |
| Key đã dùng, khác nội dung | 422, `error = IdempotencyKeyReused` |
| Nhiều request cùng key đến cùng lúc | Unique index trên key cho một cái thắng; các cái khác nhận lại kết quả của nó |

Key nên có dạng `jira:<issue>:<id sự kiện>` để mỗi sự kiện Jira chỉ tạo một lần chạy.

Mã lỗi của chuyển trạng thái (trường `error` trong ProblemDetails):

| HTTP | `error` | Khi nào |
|---|---|---|
| 409 | `VersionConflict` | `expectedVersion` cũ, hoặc ai đó ghi trước (phát hiện ở DB) |
| 403 | `ActorNotAllowed` | Loại actor không được kích hoạt chuyển này |
| 422 | `NotAllowed`, `TerminalState`, `GuardFailed` | Chuyển không có trong bảng, đã ở trạng thái cuối, hết lượt vòng lặp |

## Giới hạn đã biết

- **Chưa xác thực:** `actor` nằm trong body nên ai cũng tự nhận là `Human` được. Phải thay bằng danh tính từ token của dịch vụ (service token) trước khi cho người dùng thật dùng (`s7-2`, `s12`). Đừng mở API này ra ngoài máy local khi chưa làm.
- Bản ghi idempotency chưa hết hạn (giữ mãi). Khi dữ liệu lớn thì thêm job dọn theo `CreatedAt`.
