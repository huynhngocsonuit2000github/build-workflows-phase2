# 0002. Chọn Gemini API (gói Free) làm LLM của nền tảng, hai model: `gemini-3.8-flash` và `gemini-3.5-flash-lite`

- **Trạng thái:** Accepted
- **Ngày:** 2026-11-22
- **Người quyết định:** Son Huynh
- **Việc trong plan:** `s2-1`

> Số liệu lấy từ trang chính thức của Google ngày 2026-11-22 (xem "Nguồn"). Giá và giới hạn đổi theo thời gian: trước khi dựa vào một con số, mở lại trang nguồn.

## Bối cảnh

`s2` bắt đầu dùng LLM thật: AI BA phân tích yêu cầu, trả JSON theo schema. Cần chọn nhà cung cấp và hai model: một model mạnh cho việc khó, một model rẻ cho việc dễ.

Ràng buộc:

- Phase 1 đã dùng Gemini nên đã có kinh nghiệm; đang dùng **gói Free**, chưa trả tiền.
- Code (Agy) và review (Claude Code) là công cụ **xây** nền tảng, không phải LLM mà nền tảng gọi lúc chạy.
- Lớp LLM ở `s2-2` phải đổi được nhà cung cấp mà không sửa chỗ gọi (interface trừu tượng), nên quyết định này không khóa cứng.

## Quyết định

Chúng ta dùng **Gemini Developer API, gói Free** làm nhà cung cấp LLM trong giai đoạn học, với hai model:

- **Mạnh:** `gemini-3.8-flash` (bản stable) cho phân tích yêu cầu, viết code, review.
- **Rẻ:** `gemini-3.5-flash-lite` (bản stable) cho việc đơn giản: phân loại, rút trích, kiểm tra định dạng.

Chỉ được gửi lên gói Free **dữ liệu thực hành** (ticket TaskLite, code TaskLite). Không gửi dữ liệu thật hay bí mật (xem Rủi ro).

## So sánh hai model

|                                                          | `gemini-3.8-flash`                                          | `gemini-3.5-flash-lite`                                |
| -------------------------------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------ |
| Phiên bản                                                | Stable, cập nhật 09/2026                                    | Stable, cập nhật 07/2026                               |
| Context vào / ra tối đa                                  | 1.048.576 / 65.536 token                                    | 1.048.576 / 65.536 token                               |
| Structured output (JSON schema)                          | Có                                                          | Có                                                     |
| Gói Free                                                 | Có, miễn phí token                                          | Có, miễn phí token                                     |
| Giá trả phí, đầu vào / 1 triệu token                     | $0,75 đến hết 31/12/2026, từ 01/01/2027 là $1,50            | $0,30                                                  |
| Giá trả phí, đầu ra (gồm token suy nghĩ) / 1 triệu token | $3,75 đến hết 31/12/2026, từ 01/01/2027 là $7,50            | $2,50                                                  |
| Nội dung dùng để cải thiện sản phẩm Google               | Free: **Có**; Paid: Không                                   | Free: **Có**; Paid: Không                              |
| Mô tả của Google                                         | Flash thông minh nhất, cho kỹ thuật phần mềm dài hơi, agent | Tiết kiệm, cho tác vụ khối lượng lớn, dữ liệu đơn giản |

### Ước chi phí một lần phân tích yêu cầu (tiêu chí `done`)

Giả định: đầu vào ≈ 22.500 token (prompt hệ thống ~500 + ticket 800 từ tiếng Việt ~2.000 + context repo ~20.000), đầu ra ≈ 1.500 token. Token suy nghĩ cũng tính vào đầu ra nên đầu ra thật có thể lớn hơn. Đây là **ước lượng**, chưa đo; số đo thật ghi ở `s2-4`.

| Phương án                                 | Công thức                                   | Một lần gọi                                                                                |
| ----------------------------------------- | ------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `gemini-3.8-flash`, trả phí, đến hết 2026 | 22.500 × 0,75 + 1.500 × 3,75 (đơn vị triệu) | ≈ **$0,0225**                                                                              |
| `gemini-3.8-flash`, trả phí, từ 2027      | 22.500 × 1,50 + 1.500 × 7,50                | ≈ **$0,045**                                                                               |
| `gemini-3.5-flash-lite`, trả phí          | 22.500 × 0,30 + 1.500 × 2,50                | ≈ **$0,0105**                                                                              |
| Gói Free                                  |                                             | **$0**, nhưng bị giới hạn tốc độ và nội dung có thể được Google dùng để cải thiện sản phẩm |

Một lần chạy đủ vòng (phân tích, code, review, sửa lỗi) gọi nhiều lần, nên nhân theo số lần gọi. Phần lớn chi phí đầu vào là context repo, nên giảm context là cách giảm chi phí đầu tiên (mốc `ctx`).

### Giới hạn tốc độ

- Giới hạn tính theo **project**, không theo API key; RPD reset lúc nửa đêm giờ Thái Bình Dương; model preview và thử nghiệm bị giới hạn chặt hơn.
- Trang rate limits của Google **không in sẵn số RPM/TPM/RPD của gói Free cho hai model này**; xem số thật của project trong AI Studio (mục rate limit). **Việc cần làm:** chụp hoặc ghi lại số đó vào mục "Bằng chứng" bên dưới (che key nếu có).
- Lên Tier 1 cần bật thanh toán (liên kết billing account).

## Các phương án đã cân nhắc

| Phương án                                | Ưu                                                                                 | Nhược                                                                                 |
| ---------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| A. Gemini Free (chọn)                    | Không tốn tiền, đã quen từ phase 1, structured output, context 1M                  | Rate limit thấp; nội dung có thể dùng để cải thiện sản phẩm; không có context caching |
| B. Gemini trả phí (Tier 1)               | Rate limit cao hơn, nội dung không dùng để cải thiện sản phẩm, có caching và Batch | Tốn tiền, cần thanh toán                                                              |
| C. Nhà cung cấp khác (Anthropic, OpenAI) | Có thể mạnh hơn ở một số việc                                                      | Phải đăng ký thêm, trả phí từ đầu, chưa cần thiết để học lớp LLM                      |

Chọn A vì mục tiêu hiện tại là học và đo, dữ liệu chỉ là ticket thực hành. Interface ở `s2-2` giữ cho việc chuyển sang B hoặc C chỉ là đổi cấu hình và chạy lại eval (`s2-7`).

## Hệ quả

- **Được:** học `s2` mà không tốn tiền; hai model cùng context 1M và đều hỗ trợ structured output nên cùng một schema chạy được cho cả hai.
- **Mất / phải chịu:** rate limit gói Free có thể làm chậm eval (chạy nhiều ca); không có context caching ở Free với model rẻ; model rẻ có thể sai nhiều hơn.
- **Rủi ro và cách giảm:**
  - _Dữ liệu bị dùng để cải thiện sản phẩm Google_: chỉ gửi dữ liệu TaskLite thực hành; không gửi key, token, dữ liệu thật. Quy tắc đầy đủ làm ở `s2-6`.
  - _Giá `gemini-3.8-flash` tăng gấp đôi từ 01/01/2027_: dùng giá mới khi ước ngân sách; ghi xem lại ở điều kiện bên dưới.
  - _Model bị đổi hoặc ngừng_: ghim version, đổi model chỉ khi eval đạt (`s2-7`).
- **Khi nào xem lại quyết định này:** (1) khi chạy eval mà bị 429 làm không hoàn thành được bộ ca; (2) trước khi đưa dữ liệu không phải thực hành vào (cần gói trả phí hoặc enterprise); (3) khi eval của `s2-5` cho thấy model rẻ hoặc mạnh không đạt ngưỡng chất lượng đã đặt; (4) 01/01/2027, khi giá đổi.

## Bằng chứng

- Giá, giới hạn context, mô tả và điều khoản Free/Paid: các trang trong mục "Nguồn", đọc ngày 2026-11-22.
- Ước chi phí ở trên là phép tính tay từ giá nêu trên, chưa phải số đo. Số đo thật: `tests/evaluation/` ở `s2-4` (chưa có).
- Rate limit gói Free của project: **chưa có** (cần user lấy từ AI Studio).
- Cập nhật 2026-11-22 (chỉ thêm link, nội dung quyết định không đổi): số rate limit đã ghi tại [evidence/0002-gemini-free-rate-limits.md](evidence/0002-gemini-free-rate-limits.md).

## Nguồn

- Giá: https://ai.google.dev/gemini-api/docs/pricing
- Giới hạn tốc độ và tier: https://ai.google.dev/gemini-api/docs/rate-limits
- Danh sách model: https://ai.google.dev/gemini-api/docs/models
- Trang model: https://ai.google.dev/gemini-api/docs/models/gemini-3.8-flash, https://ai.google.dev/gemini-api/docs/models/gemini-3.5-flash-lite
