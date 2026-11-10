# Thuộc tính chất lượng: xếp ưu tiên theo giai đoạn

> Bản nháp để người quyết định duyệt. Khi thứ hạng đổi, sửa bảng này và ghi lý do trong ADR hoặc phần "Lịch sử" ở cuối.
> Hạng 1 = ưu tiên cao nhất ở giai đoạn đó. Ngưỡng định lượng (SLO) để trống vì chưa đo được; điền ở `s16` và `s17` bằng số đo thật.

## Sáu thuộc tính cho hệ thống AI

| Thuộc tính | Nghĩa trong nền tảng này | Cách kiểm chứng |
|---|---|---|
| **Đúng** | Đầu ra (AC, code, review) đạt yêu cầu và vượt eval | Eval harness, bộ ca `practice/backlog/`, test CI |
| **An toàn** | Không lộ bí mật, không vượt quyền, không bị prompt injection, không bỏ qua cổng | Ca `injection` và `security`, test runner, review chính sách |
| **Truy vết được** | Biết lần chạy nào, prompt version nào, model nào, ai duyệt, vì sao | `LlmCall`, `ToolCall`, `WorkflowHistory`, correlation id |
| **Chi phí** | Chi phí LLM và hạ tầng trên mỗi ticket nằm trong ngân sách | Bản ghi token và chi phí ước lượng mỗi `LlmCall` |
| **Độ trễ** | Thời gian từ ticket đến PR chấp nhận được | Thời gian mỗi bước trong lịch sử trạng thái |
| **Độ tin cậy** | Retry, crash, sự kiện trùng không làm hỏng hay lặp việc | Test idempotency, chaos/game day, tỷ lệ lần chạy hoàn tất |

## Thứ hạng theo giai đoạn

| Thuộc tính | POC (một người, chứng minh khả thi) | Pilot (nhiều người dùng) | Production (nhiều nhóm) |
|---|---|---|---|
| Đúng | **1** | 3 | 4 |
| An toàn | 2 | **1** | **1** |
| Truy vết được | 3 | 4 | 3 |
| Độ tin cậy | 4 | 2 | 2 |
| Chi phí | 5 | 5 | 5 |
| Độ trễ | 6 | 6 | 6 |

## Vì sao xếp như vậy

- **POC: đúng đứng đầu.** Mục tiêu POC là chứng minh một ticket đi trọn Jira → PR cho ra kết quả dùng được; sai thì mọi thứ khác vô nghĩa. An toàn hạng 2 vì từ đầu đã có nguyên tắc cứng (dữ liệu không tin cậy, người duyệt, runner cách ly ở mức cơ bản) nhưng chưa có người dùng thật nên rủi ro thấp hơn. Truy vết hạng 3 vì cần để debug và để làm bằng chứng eval.
- **Pilot: an toàn lên đầu.** Khi nhiều người dùng và agent chạm vào mã thật, một lần lộ bí mật hay vượt quyền gây thiệt hại thật. Độ tin cậy lên hạng 2 vì hàng đợi, lease, retry phải chịu được tải và crash.
- **Production: an toàn và tin cậy giữ đầu**, truy vết lên hạng 3 vì cần kiểm toán, tuân thủ, phân tích sự cố; "đúng" vẫn quan trọng nhưng được bảo vệ bởi các cổng (CI, review, người duyệt) thay vì chỉ dựa vào mô hình.
- **Chi phí và độ trễ luôn thấp nhất** ở cả ba giai đoạn: đánh đổi chúng lấy các thuộc tính trên là chấp nhận được, miễn là có ngân sách trần (chặn chi phí tăng vô hạn, thuộc về `gw`) và không có luồng nào chờ vô hạn. Ghi chú: "thấp nhất" không có nghĩa là bỏ qua; ngân sách và timeout vẫn là bắt buộc.

## Quyết định thiết kế kéo theo

- Đúng ở POC → xây eval sớm (`s2-5`) trước khi thêm tính năng.
- An toàn đứng đầu từ Pilot → runner, gateway, guardrails (`s7`, `gw`) làm trước khi mở rộng người dùng.
- Truy vết → mọi `LlmCall`, `ToolCall`, chuyển trạng thái đều ghi kèm correlation id ngay từ POC, vì thêm sau rất khó.

## Lịch sử

- 2026-11-10: bản đầu (`s0b-5`).
