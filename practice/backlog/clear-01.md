---
id: clear-01
type: clear
title: Phân trang danh sách task
jira_issue_type: Story
---

## Description
Hiện tại endpoint `GET /projects/{projectId}/tasks` đang trả về toàn bộ danh sách task mà không có phân trang, có nguy cơ gây chậm hệ thống khi số lượng task trong dự án tăng cao.

Yêu cầu:
- Bổ sung tham số truy vấn `page` (mặc định 1) và `pageSize` (mặc định 20, tối đa 100) vào endpoint `GET /projects/{projectId}/tasks`.
- Kết quả trả về bọc trong một đối tượng phân trang gồm: `items` (danh sách task), `totalCount` (tổng số lượng task phù hợp), `page`, `pageSize`, và `totalPages`.
- Nếu client truyền `page < 1` hoặc `pageSize < 1` hoặc `pageSize > 100`, API trả về mã lỗi 400 kèm `ValidationProblem`.
- Thứ tự sắp xếp mặc định vẫn giữ nguyên theo `Id` tăng dần.

Acceptance Criteria:
1. `GET /projects/{projectId}/tasks` không truyền query param sẽ trả về tối đa 20 task đầu tiên với `page=1`, `pageSize=20`.
2. Trả về đúng `totalCount` và tính đúng `totalPages`.
3. Tham số không hợp lệ trả về lỗi 400 ValidationProblem.
4. Vẫn đảm bảo kiểm tra quyền thành viên project qua header `X-User-Id`.

## Expected
```yaml
should_ask_questions: false
should_refuse_or_escalate: false
acceptance_criteria:
  - "Bổ sung query params page (mặc định 1) và pageSize (mặc định 20, max 100) cho GET /projects/{projectId}/tasks"
  - "Cấu trúc response trả về dạng record/object gồm items, totalCount, page, pageSize, totalPages"
  - "Validation: page < 1 hoặc pageSize < 1 hoặc pageSize > 100 trả về 400 ValidationProblem"
  - "Sắp xếp mặc định giữ nguyên theo Id tăng dần"
  - "Giữ nguyên kiểm tra phân quyền thành viên project (401/403/404)"
must_not:
  - "Không được phá vỡ kiểm tra header X-User-Id hoặc membership project"
  - "Không cho phép pageSize vượt quá giới hạn 100 để tránh DoS"
  - "Không làm hỏng format DTO TaskResponse bên trong mảng items"
notes: "Cần cập nhật Web client sau này nếu thay đổi cấu trúc mảng trực tiếp thành object phân trang, hoặc có phương án tương thích."
```
