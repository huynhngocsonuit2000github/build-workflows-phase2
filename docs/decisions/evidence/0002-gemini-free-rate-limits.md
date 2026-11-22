# Bằng chứng cho ADR 0002: rate limit gói Free của project (AI Studio)

- **Ngày ghi:** 2026-11-22
- **Nguồn:** ảnh chụp mục rate limit trong Google AI Studio của project của user (không có key trong ảnh).
- **Lưu ý đọc ảnh:** ảnh không hiện tên cột. Ba cột được đọc theo thứ tự thường gặp là RPM, TPM (vào), RPD; **user xác nhận lại tên cột khi có dịp**.

| Model | Loại | RPM | TPM | RPD |
|---|---|---|---|---|
| `gemini-3.8-flash` | Text-out | 5 | 250.000 | 20 |
| `gemini-3.5-flash-lite` | Text-out | **chưa có** (ảnh chỉ chụp dòng 3.8 Flash) | | |

## Hệ quả đã thấy

- **20 request mỗi ngày** cho model mạnh là rất thấp: chạy eval nhiều ca (`s2-5`) hoặc lặp vòng agent sẽ hết hạn mức trong vài phút. RPD reset lúc nửa đêm giờ Thái Bình Dương.
- 5 request mỗi phút: lớp retry/backoff ở `s2-2` sẽ bị 429 thật khi thử nhanh.
- Việc cần làm: lấy dòng `gemini-3.5-flash-lite`; cân nhắc dùng model rẻ cho eval nếu hạn mức cao hơn; xem lại ADR 0002 theo điều kiện (1) "chạy eval bị 429 làm không hoàn thành được bộ ca" khi tới `s2-5`.
