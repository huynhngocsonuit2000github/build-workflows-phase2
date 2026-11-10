# NNNN. Tiêu đề quyết định (động từ + đối tượng)

- **Trạng thái:** Proposed | Accepted | Superseded by NNNN
- **Ngày:** YYYY-MM-DD
- **Người quyết định:** tên
- **Việc trong plan:** id (ví dụ `s0b-5`)

> Quy tắc: ADR đã `Accepted` thì **không sửa nội dung**. Muốn đổi quyết định, viết ADR mới và đổi trạng thái ADR cũ thành `Superseded by NNNN`. Chỉ được sửa lỗi chính tả và thêm link.
> Đặt tên file: `docs/decisions/NNNN-tieu-de-kebab.md`, số tăng dần, bắt đầu từ `0001`.

## Bối cảnh

Vấn đề cụ thể đang gặp là gì? Ràng buộc nào (kỹ thuật, thời gian, chi phí, an toàn)? Vì sao phải quyết ngay bây giờ?

## Quyết định

Một đoạn ngắn, viết ở thì hiện tại: "Chúng ta dùng X cho Y."

## Các phương án đã cân nhắc

| Phương án | Ưu | Nhược |
|---|---|---|
| A. | | |
| B. | | |
| C. | | |

Vì sao loại các phương án còn lại?

## Hệ quả

- **Được:**
- **Mất / phải chịu:**
- **Rủi ro và cách giảm:**
- **Khi nào xem lại quyết định này** (điều kiện cụ thể, đo được):

## Bằng chứng

Link tới kết quả chạy thật (file trong `tests/evaluation/.../results/`, log CI, PR). Mọi con số trong ADR phải trỏ về một file kết quả. Không ghi số ước chừng.

## 7 câu hỏi checkpoint (chỉ cần khi quyết định thêm một thành phần mới)

1. Đang giải quyết vấn đề cụ thể nào?
2. Kiến trúc .NET + n8n hiện tại có giải quyết an toàn được không?
3. Thành phần mới xử lý những kiểu lỗi nào?
4. Nó thêm gánh nặng vận hành gì?
5. Test và quan sát nó thế nào?
6. Kế hoạch chuyển đổi và quay lui là gì?
7. Kết quả đo được nào biện minh cho việc dùng nó?
