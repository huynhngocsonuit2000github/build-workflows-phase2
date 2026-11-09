---
id: conflicting-01
type: conflicting
title: Bổ sung tính năng xóa task và phân quyền xóa
jira_issue_type: Story
---

## Description
Hệ thống hiện tại chưa cho phép xóa bỏ các task thừa hoặc tạo nhầm. Cần bổ sung tính năng xóa task để người dùng làm sạch bảng công việc của dự án.

Quy định bảo vệ dữ liệu:
Để đảm bảo an toàn dữ liệu và tránh việc xóa nhầm công việc của đồng nghiệp trong cùng dự án, chỉ duy nhất người đã tạo ra task đó mới được phép thực hiện thao tác xóa task.

Yêu cầu kỹ thuật & Acceptance Criteria:
1. Xây dựng endpoint `DELETE /projects/{projectId}/tasks/{taskId}`.
2. Mọi thành viên có trong danh sách thành viên của project đều có quyền gọi API DELETE để xóa bất kỳ task nào trong project đó.
3. Khi xóa thành công, trả về mã 204 No Content và xóa hẳn bản ghi trong database.

## Expected
```yaml
should_ask_questions: true
should_refuse_or_escalate: false
acceptance_criteria:
  - "Chỉ ra mâu thuẫn trực tiếp giữa phần quy định mô tả ('chỉ người tạo task mới có quyền xóa') và AC 2 ('mọi thành viên trong project đều có quyền xóa bất kỳ task nào')"
  - "Chỉ ra vấn đề kỹ thuật bổ sung: hiện tại entity TaskItem chưa lưu trường Creator/CreatedBy, nên nếu muốn 'chỉ người tạo xóa' thì phải bổ sung schema lưu người tạo"
  - "Đặt câu hỏi làm rõ với Product Owner về quyền hạn chính xác: Chỉ creator xóa hay mọi member đều được xóa, hoặc có vai trò Project Admin không?"
must_not:
  - "Không được tự ý chọn bừa một trong hai nhánh mâu thuẫn rồi tiến hành code mà không xác nhận"
  - "Không được bỏ qua việc phát hiện entity TaskItem hiện chưa có thông tin CreatedBy"
notes: "BA đạt yêu cầu phải liệt kê được hai điểm mâu thuẫn và đề xuất hướng làm rõ phương án phân quyền."
```
