# Bằng chứng cho ADR 0002: rate limit gói Free của project (AI Studio)

- **Ngày ghi:** 2026-11-22
- **Nguồn:** ảnh chụp mục rate limit trong Google AI Studio của project của user (không có key trong ảnh).
- **Lưu ý đọc ảnh:** ảnh không hiện tên cột. Ba cột được đọc theo thứ tự thường gặp là RPM, TPM (vào), RPD; **user xác nhận lại tên cột khi có dịp**.

| Model | Loại | RPM | TPM | RPD |
|---|---|---|---|---|
| `gemini-3.8-flash` | Text-out | 5 | 250.000 | 20 |
| `gemini-3.5-flash-lite` | Text-out | 15 | 250.000 | 500 |

## Hệ quả đã thấy

- **20 request mỗi ngày** cho model mạnh là rất thấp: chạy eval nhiều ca (`s2-5`) hoặc lặp vòng agent sẽ hết hạn mức trong vài phút. RPD reset lúc nửa đêm giờ Thái Bình Dương.
- 5 request mỗi phút: lớp retry/backoff ở `s2-2` sẽ bị 429 thật khi thử nhanh.
- Model rẻ `gemini-3.5-flash-lite` có hạn mức cao hơn nhiều: 15 RPM và **500 RPD** (gấp 25 lần model mạnh). Eval nhiều ca (`s2-5`) nên chạy trên model rẻ; model mạnh chỉ dùng cho ca cần chất lượng và đo chọn lọc.
- Xem lại ADR 0002 theo điều kiện (1) "chạy eval bị 429 làm không hoàn thành được bộ ca" khi tới `s2-5`.
