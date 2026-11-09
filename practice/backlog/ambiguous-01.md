---
id: ambiguous-01
type: ambiguous
title: Làm nổi bật các task bị quá hạn
jira_issue_type: Story
---

## Description
Khách hàng phản ánh rằng hiện tại nhìn vào bảng công việc rất khó phát hiện các việc đã quá hạn. Họ muốn hệ thống làm sao để các task quá hạn phải hiển thị thật nổi bật trên màn hình để nhân viên không bao giờ bị bỏ sót việc.

Yêu cầu sơ bộ:
- Cần có cách nhận biết và làm nổi bật các task đã quá hạn.
- Đảm bảo người dùng mở danh sách lên là nhìn thấy ngay các công việc cần chú ý gấp này.

## Expected
```yaml
should_ask_questions: true
should_refuse_or_escalate: false
acceptance_criteria:
  - "Xác định rõ định nghĩa task quá hạn: DueDate < Ngày hiện tại (theo UTC hay timezone nào?)"
  - "Làm rõ task đã ở trạng thái Done thì có được coi là quá hạn và cần làm nổi bật không (thông thường chỉ áp dụng cho Todo và InProgress)"
  - "Làm rõ cách hiển thị nổi bật trên UI (thêm nhãn 'Overdue', đổi màu dòng/chữ đỏ, biểu tượng cảnh báo, hoặc sắp xếp lên đầu)"
  - "Làm rõ phạm vi xử lý: tính toán hoàn toàn phía Frontend hay Backend bổ sung cờ IsOverdue vào TaskResponse"
must_not:
  - "Không tự ý đưa ra giả định về timezone hoặc tự triển khai giao diện theo ý thích cá nhân mà không trao đổi làm rõ"
  - "Không đánh dấu task Done là quá hạn gây phiền toái cho người dùng trừ khi có yêu cầu cụ thể"
notes: "BA cần tổng hợp danh sách câu hỏi gửi cho Product Owner trước khi phân tích chi tiết kỹ thuật."
```
