# Quy ước kỹ thuật

Mặc định cho cả repo, gồm nền tảng và TaskLite (`apps/tasklite/`). Thay đổi quy ước nào thì ghi ADR.

## 1. Công nghệ

| Mảng | Lựa chọn |
|---|---|
| Runtime | .NET 10 (SDK 10.0.x), C# mới nhất, `Nullable` bật, analyzer bật |
| API | ASP.NET Core minimal API, lỗi theo ProblemDetails (RFC 9457), OpenAPI tích hợp sẵn |
| Dữ liệu | PostgreSQL + EF Core (Npgsql) cho nền tảng; SQLite cho TaskLite; migration EF Core |
| Concurrency | Optimistic concurrency (cột `xmin` của PostgreSQL) |
| Frontend TaskLite | Một trang (Angular tối giản hoặc HTML + fetch), địa chỉ API đọc từ `config.json` lúc chạy |
| Test | xUnit, `WebApplicationFactory`; Testcontainers cho PostgreSQL ở nền tảng; Playwright cho E2E |
| Đóng gói | Docker multi-stage, Docker Compose cho môi trường local |
| CI | GitHub Actions; kết quả test và scan lưu làm artifact |
| Sơ đồ | Mermaid trong Markdown, theo C4 (Context, Container, Component) và sequence |

Một solution `.slnx` cho nền tảng ở gốc repo; TaskLite có `TaskLite.slnx` riêng trong `apps/tasklite/`. Mỗi app/service có project test riêng đặt cạnh nó.

## 2. HTTP, định danh và cấu hình

- Header dùng chung:
  - `X-Correlation-Id`: nhận từ request hoặc tạo mới, truyền qua mọi lời gọi giữa n8n, Workflow API, agent-service, runner, và ghi vào mọi log.
  - `Idempotency-Key`: bắt buộc với lệnh tạo có tác động phụ (ví dụ `POST /executions`).
  - `X-User-Id`: chỉ dùng trong TaskLite (xác thực giả lập).
- Xác thực giữa các dịch vụ: service token (`Authorization: Bearer`) ở P1, mTLS hoặc workload identity về sau.
- Cấu hình không bí mật trong `appsettings.json`. Bí mật dùng `dotnet user-secrets` khi chạy local, `.env` cho Compose. Commit `.env.example`, không bao giờ commit `.env`.
- Log có cấu trúc (JSON), luôn có `correlationId`, `executionId` nếu có. Không ghi bí mật, token hay nội dung nhạy cảm của prompt.

## 3. Prompt, schema, đánh giá

- Prompt: `agents/<role>/prompts/<tên>.v<N>.md`. Đã dùng thì không sửa, tạo file version mới. Mỗi `LlmCall` ghi prompt version.
- Model: luôn dùng tên model có version cụ thể, không dùng bí danh "latest". Đổi model phải chạy eval và qua duyệt (`s2-7`).
- Schema: `contracts/schemas/<tên>.v<N>.schema.json` (JSON Schema 2020-12). Sự kiện: `contracts/events/<TênSựKiện>.v<N>.schema.json`.
- Đầu ra của LLM luôn được kiểm tra lại bằng schema phía code; sai thì sinh lại có giới hạn rồi chuyển người.
- Đánh giá: bộ ca ở `tests/evaluation/<bộ>/cases/`, kết quả ở `tests/evaluation/<bộ>/results/<ngày>-<prompt hoặc model version>.md`. Mọi số liệu trong ADR phải trỏ tới một file kết quả.
- Backlog thực hành: `practice/backlog/<loại>-<số>.md` trong repo nền tảng, có phần `expected`. Nội dung ticket (không kèm `expected`) mới được đưa lên Jira.

## 4. Kiểm thử

- Mỗi tính năng có test cho 5 tình huống: thành công, lỗi, timeout, trùng lặp, không có quyền (roadmap §10).
- Test tích hợp chạy với database thật (PostgreSQL qua Testcontainers ở nền tảng).
- Đặt tên test: `Method_Scenario_ExpectedResult`.
- Chỉ kết quả CI là bằng chứng. Không bao giờ coi lời agent tự báo là kết quả test.

## 5. Git và PR

- Trunk-based: `main` được bảo vệ (bắt buộc PR, CI xanh, bỏ duyệt cũ khi có push mới).
- Nhánh của người: `feat/<chủ-đề>`, `fix/<chủ-đề>`, `docs/<chủ-đề>`. Nhánh của agent trong TaskLite: `ai/<JIRA-KEY>-<slug>`.
- Commit theo Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`, `chore:`, `refactor:`), kèm id việc trong plan khi liên quan, ví dụ `feat(workflow-api): validate transitions (s1-2)`.
- Mô tả PR gồm: tóm tắt, id việc trong plan, bằng chứng (test, log, ảnh), ADR liên quan, rủi ro.
- Code do AI tạo được đánh dấu (nhãn `ai-generated` trên PR từ `s12-1`).

## 6. ADR

- File: `docs/decisions/NNNN-tieu-de-kebab.md`, đánh số tăng dần, bắt đầu từ `0001`; mẫu ở `0000-template.md`.
- Mục: bối cảnh, quyết định, các phương án đã cân nhắc, hệ quả, trạng thái (Proposed, Accepted, Superseded by NNNN).
- Đã Accepted thì không sửa nội dung; muốn đổi thì viết ADR mới thay thế.
- Quyết định thêm thành phần mới phải trả lời 7 câu hỏi checkpoint (xem `CLAUDE.md`).

## 7. Định nghĩa hoàn thành (một việc trong plan)

1. Mọi tiêu chí `done` của việc đều đạt và có bằng chứng (test, log, file, ảnh).
2. Test liên quan chạy xanh trong CI.
3. Tài liệu và ADR liên quan đã cập nhật.
4. Không có bí mật trong code, log hay commit.
5. Cuối mỗi mốc: cập nhật mục "Trạng thái hiện tại" trong `CLAUDE.md`.
