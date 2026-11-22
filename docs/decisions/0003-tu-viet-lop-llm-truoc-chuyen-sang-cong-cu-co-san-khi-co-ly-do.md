# 0003. Tự viết lớp LLM (`ILlmClient`) trước; chuyển sang công cụ có sẵn khi gặp điều kiện cụ thể

- **Trạng thái:** Accepted
- **Ngày:** 2026-11-22
- **Người quyết định:** Son Huynh
- **Việc trong plan:** `s2-2`

> Số liệu và mô tả công cụ lấy từ tài liệu chính thức đọc ngày 2026-11-22 (xem "Nguồn"). Chỗ nào chưa xác nhận được thì ghi rõ là chưa xác nhận.

## Bối cảnh

Mọi lời gọi model (AI BA, Developer, Reviewer) cần đi qua một lớp có timeout, retry có giới hạn theo loại lỗi và model dự phòng (`s2-2`). Câu hỏi: tự viết (`services/agent-service/src/AgentService.Llm`), dùng `Microsoft.Extensions.AI` (`IChatClient`), hay dùng LiteLLM?

Ràng buộc:

- Mục tiêu của dự án là học và hiểu vì sao từng lớp tồn tại; `s2-2` là việc `core` (tự viết).
- Chính sách retry của nền tảng đã chốt (bảng trong `CLAUDE.md`): validation không retry, unauthorized dừng, rate limit backoff, lỗi mạng retry có giới hạn, lỗi vĩnh viễn dừng an toàn.
- Hiện chỉ cần một kiểu gọi: văn bản vào, văn bản ra, một nhà cung cấp (Gemini), hai model (ADR 0002).
- Nền tảng chạy bằng .NET + n8n; thêm một dịch vụ Python là thêm gánh nặng vận hành (nguyên tắc "không thêm thành phần vì đang thịnh hành").

## Quyết định

Chúng ta **tự viết** lớp LLM, giữ nhỏ (một interface `ILlmClient`, một client Gemini, một lớp bọc resilient), và **chưa** dùng `Microsoft.Extensions.AI` hay LiteLLM. Đồng thời ghi sẵn điều kiện để chuyển (mục "Hệ quả"), để việc chuyển là quyết định có số đo chứ không phải theo cảm tính.

## So sánh

|                                      | A. Tự viết (chọn)                                 | B. `Microsoft.Extensions.AI`                                                                                                                                         | C. LiteLLM                                                                                               |
| ------------------------------------ | ------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Là gì                                | Code của mình, ~vài trăm dòng                     | Thư viện .NET: `IChatClient` làm hợp đồng chung, kèm middleware                                                                                                      | Thư viện Python, hoặc dịch vụ gateway (proxy) tự chạy, gọi 100+ LLM theo định dạng kiểu OpenAI           |
| Ngôn ngữ / vận hành                  | .NET, nằm trong agent-service, không thêm dịch vụ | .NET, nằm trong tiến trình, không thêm dịch vụ                                                                                                                       | Python; dùng như SDK thì thêm runtime Python, dùng như proxy thì **thêm một dịch vụ cần chạy và bảo vệ** |
| Retry / dự phòng                     | Tự viết, đúng bảng chính sách của mình            | Trang tài liệu `IChatClient` liệt kê middleware gọi hàm, cache, OpenTelemetry; **không thấy nói retry hoặc dự phòng** (cần tự viết middleware hoặc dùng cơ chế khác) | Có sẵn: `num_retries` và `fallbacks` theo nhóm model (lỗi rate limit, context quá dài, vi phạm nội dung) |
| Theo dõi chi phí, ngân sách, khóa ảo | Tự viết (`s2-4`, `gw`)                            | Tự viết (có OpenTelemetry cho telemetry)                                                                                                                             | Có sẵn ở phần proxy: virtual keys, cost tracking, admin UI                                               |
| Gemini                               | Tự viết client HTTP, đã có                        | **Chưa xác nhận** có bản cài đặt `IChatClient` đủ tin cậy cho Gemini; cần kiểm tra trước khi dựa vào                                                                 | Hỗ trợ nhiều nhà cung cấp (có Vertex AI); chưa kiểm tra riêng Gemini Developer API                       |
| Học được gì                          | Nhiều nhất: tự thấy từng loại lỗi                 | Học cách dùng abstraction chuẩn của hệ sinh thái .NET                                                                                                                | Học cách vận hành một gateway                                                                            |
| Rủi ro                               | Tự bảo trì; dễ sót lỗi lạ                         | Bị buộc theo hình dạng của `IChatClient` (tin nhắn nhiều lượt, đa phương thức) dù mình chỉ cần văn bản                                                               | Thêm bề mặt tấn công và phụ thuộc Python; ranh giới với `gw` chưa chốt                                   |

Vì sao không chọn B hoặc C ngay: dự án đang ở bước học, chỉ có một nhà cung cấp và một kiểu gọi; B không thay được phần khó (phân loại lỗi, dự phòng theo chính sách) mà mình vẫn phải viết; C là một dịch vụ mới, thuộc quyết định của mốc `gw`.

## Hệ quả

- **Được:** hiểu rõ từng nhánh lỗi; test và mutation kiểm soát hoàn toàn (32 test, 4 lần phá code đều bị bắt); không thêm phụ thuộc hay dịch vụ.
- **Mất / phải chịu:** tự bảo trì; còn 5 điểm nợ đã ghi (`FinishReason`, token suy nghĩ, lỗi đọc body, thiếu key, 429 theo ngày); phải tự viết thêm streaming, gọi công cụ nếu sau này cần.
- **Rủi ro và cách giảm:** tự viết lớp nhiễu sót lỗi nhà cung cấp lạ: chạy thử gọi thật, ghi lại lỗi gặp, thêm test. Để dễ chuyển, giữ `ILlmClient` gần hình dạng `IChatClient` (văn bản vào/ra, tùy chọn model và nhiệt độ), chặn phụ thuộc vào kiểu riêng của Gemini ở tầng trên.
- **Khi nào xem lại (điều kiện cụ thể, đo được):**
  1. Cần **gọi công cụ (function calling) hoặc streaming** ở nhiều agent, hoặc cần **hơn hai nhà cung cấp**: đánh giá lấy `IChatClient` làm hợp đồng, viết adapter mỏng.
  2. Số giờ bảo trì lớp này vượt ngưỡng đã đặt (đề xuất: hơn 4 giờ cho mỗi mốc, ghi lại ở cuối mốc).
  3. Tới mốc `gw` (gateway, ngân sách, guardrails, nhiều nhóm dùng): đánh giá LiteLLM như gateway **trước khi** tự viết; cần chạy thử so sánh trên cùng bộ ca eval.
  4. Phát hiện một loại lỗi nhà cung cấp mà lớp tự viết xử lý sai hai lần trở lên.

## Bằng chứng

- Mã nguồn và test: `services/agent-service/` (32 test pass, commit `ab7a14f`); mutation do Claude chạy 2026-11-22 (phá phân loại lỗi, giới hạn retry, dự phòng, ánh xạ 400: test fail đúng).
- Chưa có: so sánh đo được với `Microsoft.Extensions.AI` và LiteLLM trên cùng một tác vụ (sẽ làm khi gặp điều kiện 1 hoặc 3 ở trên); thử gọi Gemini thật.
- Phần "không thấy nói retry/dự phòng" của B dựa vào việc đọc trang `IChatClient` trên Microsoft Learn; chưa đọc toàn bộ gói nên cần xác nhận lại trước khi dựa vào kết luận này.

## Nguồn

- Microsoft.Extensions.AI: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai và https://learn.microsoft.com/en-us/dotnet/ai/ichatclient (bản ổn định mới nhất trên NuGet khi đọc: 11.0.1)
- LiteLLM: https://docs.litellm.ai/docs/ và https://docs.litellm.ai/docs/proxy/reliability
