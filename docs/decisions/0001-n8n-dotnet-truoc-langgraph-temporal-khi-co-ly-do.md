# 0001. Dùng n8n + .NET trước; chỉ thêm LangGraph hoặc Temporal khi có lý do đo được

- **Trạng thái:** Accepted
- **Ngày:** 2026-11-10
- **Người quyết định:** Son Huynh
- **Việc trong plan:** `s0b-5`

## Bối cảnh

Nền tảng cần đưa một ticket Jira đã duyệt đi qua nhiều bước (phân tích yêu cầu, duyệt, lập kế hoạch, code, build/test, review, PR, deploy), có 3 cổng người, phải truy vết được và phục hồi an toàn khi retry, crash hoặc sự kiện trùng. Có nhiều công cụ điều phối trông phù hợp: n8n (tích hợp), LangGraph (đồ thị trạng thái cho agent), Temporal (thực thi bền vững). Đây là dự án học, một người làm, giai đoạn POC cần chứng minh một ticket đi trọn Jira → PR.

Nguyên tắc đã chốt trong `CLAUDE.md`: code tất định giữ trạng thái, quyền, cổng chất lượng và tác động phụ; không thêm thành phần chỉ vì đang thịnh hành.

## Quyết định

Giai đoạn POC và Pilot dùng **n8n + .NET**:

- **n8n** chỉ điều phối: nhận sự kiện, gọi API, thông báo. Không giữ trạng thái.
- **Workflow API (.NET + PostgreSQL)** là nguồn sự thật về trạng thái lần chạy, luật chuyển trạng thái, quyền và lịch sử.
- **agent-service (.NET)** chạy agent bằng vòng lặp công cụ có giới hạn, do mình kiểm soát.

**LangGraph** và **Temporal** chưa dùng. Chỉ đánh giá lại khi gặp một trong các điều kiện ở mục "Khi nào xem lại".

## Các phương án đã cân nhắc

| Phương án                     | Ưu                                                                                                                                    | Nhược                                                                                                                       |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| A. n8n + .NET (chọn)          | Ít thành phần, ít vận hành; học được bản chất (state machine, idempotency, retry) vì tự dựng; trạng thái nằm trong code kiểm thử được | Phải tự viết state machine, retry, hàng đợi; n8n không thích hợp cho luồng rất dài hoặc logic bù trừ phức tạp               |
| B. Temporal ngay từ đầu       | Thực thi bền vững, timer và signal sẵn, replay                                                                                        | Thêm cụm vận hành (server, worker), đường cong học lớn, che mất những thứ cần học ở `s1`/`s8`; chưa có yêu cầu thực tế      |
| C. LangGraph ngay từ đầu      | Đồ thị trạng thái, checkpoint cho agent                                                                                               | Gắn vào hệ sinh thái Python, trái stack .NET; chưa có agent nào cần chu trình phức tạp; dễ để LLM chi phối luồng điều khiển |
| D. Toàn bộ trong .NET, bỏ n8n | Một công nghệ, ít điểm nối                                                                                                            | Mất lớp tích hợp trực quan với Jira, GitHub, thông báo; tự viết lại phần n8n làm sẵn                                        |

Loại B và C vì chưa có yêu cầu cụ thể nào biện minh cho chi phí vận hành. Loại D vì n8n rẻ cho phần tích hợp và không giữ trạng thái nên không làm yếu nguyên tắc "code giữ trạng thái".

## Hệ quả

- **Được:** kiến trúc nhỏ, chạy được trên máy cá nhân; mọi chuyển trạng thái đi qua code .NET có test; dễ thay n8n sau này vì nó không giữ trạng thái.
- **Mất / phải chịu:** tự chịu trách nhiệm cho retry có phân loại, idempotency, hàng đợi có lease, phục hồi sau crash (làm ở `s1`, `s8`).
- **Rủi ro và cách giảm:**
  - Workflow n8n phình thành logic nghiệp vụ → giữ n8n mỏng, mọi quyết định nằm ở Workflow API; xuất workflow n8n vào `workflows/n8n/` để review.
  - Tự dựng state machine có lỗi → test chuyển trạng thái hợp lệ và không hợp lệ, optimistic concurrency.
- **Khi nào xem lại quyết định này:**
  - Temporal: luồng chạy rất lâu, phải sống sót qua crash tiến trình hoặc hạ tầng, cần timer/signal bền vững, hoặc logic retry và bù trừ viết tay trở nên khó bảo trì.
  - LangGraph: một agent cần trạng thái dạng đồ thị, chu trình có kiểm soát, checkpoint, hoặc tương tác người nằm trong chính agent mà vòng lặp công cụ đơn giản không đáp ứng.
  - Khi đó: ghi yêu cầu, làm prototype, ước chi phí vận hành, đo lợi ích, rồi viết ADR mới (làm ở `s14`).

## Bằng chứng

Chưa có số đo (quyết định này đặt ra trước khi có hệ thống). Điều kiện xem lại ở trên là điều kiện để thu bằng chứng sau này; số liệu phải trỏ về file kết quả theo quy ước trong `engineering-standards.md`.

## 7 câu hỏi checkpoint

Áp dụng cho việc **không** thêm Temporal và LangGraph, tức trả lời theo hướng "vì sao chưa cần":

1. **Vấn đề cụ thể:** chưa có. Yêu cầu hiện tại (một ticket đi trọn Jira → PR, có 3 cổng người) làm được bằng state machine .NET + n8n.
2. **.NET + n8n giải quyết an toàn được không:** được, với điều kiện trạng thái nằm ở Workflow API và mọi chuyển trạng thái được kiểm tra bằng code.
3. **Thành phần mới xử lý lỗi gì:** Temporal xử lý crash giữa chừng của luồng dài; LangGraph xử lý trạng thái agent phức tạp. Cả hai chưa xuất hiện ở POC; crash được xử lý bằng retry có phân loại và hàng đợi có lease (`s8`).
4. **Gánh nặng vận hành:** Temporal cần server và worker; LangGraph kéo thêm runtime Python. Với một người làm, đó là chi phí lớn so với lợi ích hiện tại.
5. **Test và quan sát:** hiện test được bằng test .NET (`WebApplicationFactory`, Testcontainers) và log có correlation id; thành phần mới sẽ cần thêm cách test replay hoặc checkpoint.
6. **Chuyển đổi và quay lui:** vì n8n không giữ trạng thái và Workflow API là nguồn sự thật, thêm Temporal sau này chủ yếu thay phần điều phối; giữ nguyên hợp đồng API để quay lui được.
7. **Kết quả đo biện minh:** chưa có. Chỉ thêm khi đo được vấn đề cụ thể (ví dụ tỷ lệ luồng mất trạng thái sau crash, hay độ phức tạp retry vượt ngưỡng đã đặt trước).
