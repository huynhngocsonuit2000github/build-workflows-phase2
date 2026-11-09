---
id: clear-05
type: clear
title: Bổ sung thao tác đổi trạng thái task trên trang web
jira_issue_type: Story
---

## Description
Trên giao diện web hiện tại, người dùng chỉ có thể xem danh sách task và tạo task mới, chưa thể cập nhật trạng thái của task trực tiếp mà phải dùng công cụ API ngoài.

Yêu cầu:
- Thêm cột "Action" vào bảng danh sách task trên `index.html`.
- Trên mỗi hàng của task, cung cấp một thẻ `<select>` hoặc các nút bấm tương ứng cho phép chọn trạng thái mới (`Todo`, `InProgress`, `Done`).
- Khi người dùng thay đổi trạng thái:
  - Gửi request `PATCH /projects/{projectId}/tasks/{taskId}/status` lên API kèm header `X-User-Id` của user đang chọn.
  - Body gửi đi: `{ "status": "<giá trị mới>" }`.
- Nếu cập nhật thành công: cập nhật lại dữ liệu hiển thị trên bảng hoặc tải lại danh sách task, ẩn thông báo lỗi nếu có.
- Nếu API trả lỗi (ví dụ không có quyền hoặc lỗi kết nối): hiển thị thông báo lỗi chi tiết trong khối `#error` sẵn có trên trang.
- Đặt `data-testid="task-status-select"` hoặc thuộc tính nhận diện rõ ràng trên phần tử điều khiển để hỗ trợ kiểm thử tự động.

Acceptance Criteria:
1. Người dùng có thể đổi trạng thái một task từ `Todo` sang `InProgress` ngay trên trình duyệt và thấy trạng thái mới cập nhật tức thì.
2. Thao tác tiếp tục chuyển sang `Done` hoạt động chuẩn xác.
3. Nếu đổi `User ID` sang người không thuộc dự án và bấm đổi status, trang hiển thị lỗi 403 từ ProblemDetails.

## Expected
```yaml
should_ask_questions: false
should_refuse_or_escalate: false
acceptance_criteria:
  - "Bổ sung cột Action và phần tử chọn trạng thái (select hoặc buttons) cho mỗi hàng trong task-table"
  - "Gọi đúng PATCH /projects/{projectId}/tasks/{taskId}/status khi người dùng thao tác"
  - "Sử dụng header X-User-Id từ ô nhập User ID hiện tại và URL từ config.json"
  - "Xử lý thành công bằng cách cập nhật giao diện, xử lý lỗi bằng cách hiển thị trong #error"
  - "Có gắn data-testid rõ ràng cho phần tử tương tác trạng thái"
must_not:
  - "Không được hard-code địa chỉ API trong mã nguồn web"
  - "Không làm hỏng các data-testid và id có sẵn phục vụ kiểm thử E2E"
notes: "Chỉ sửa mã client HTML/JS/CSS trong thư mục web/, không cần thay đổi API."
```
