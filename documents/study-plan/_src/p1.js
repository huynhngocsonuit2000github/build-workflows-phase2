const MS = 'https://learn.microsoft.com';
module.exports = {
  id: 'p1', code: 'P1', color: '#ec4899',
  title: ['POC: điều phối workflow và các agent AI đầu tiên', 'POC: workflow orchestration and the first AI agents'],
  goal: ['4–6 tuần · một luồng Jira → PR trọn vẹn, AI có kiểm soát', '4–6 weeks · one complete Jira → PR flow with controlled AI'],
  desc: ['Phase 1 dùng nhãn Jira và marker trong comment làm trạng thái. Ở đây trạng thái chuyển vào một Workflow API .NET có database, agent được gọi qua một lớp LLM có kiểm soát, có công cụ được cấp quyền và có giới hạn vòng lặp.', 'Phase 1 used Jira labels and comment markers as state. Here state moves into a .NET Workflow API with a database, and agents are called through a controlled LLM layer, with permissioned tools and bounded loops.'],
  milestones: [
    {
      id: 's1', title: ['Workflow API và state machine', 'Workflow API and state machine'],
      items: [
        {
          id: 's1-1', kind: 'learn', h: '2–3',
          t: ['Học workflow tất định, state machine, idempotency và correlation ID', 'Learn deterministic workflows, state machines, idempotency and correlation IDs'],
          goal: ['Vẽ được state machine của một lần chạy và giải thích vì sao nhãn Jira không đủ làm nguồn trạng thái.', 'You can draw a run\'s state machine and explain why Jira labels are not enough as the source of state.'],
          learn: [
            ['Workflow orchestration (tuần tự, tất định) khác agent orchestration (suy luận, không tất định)', 'Workflow orchestration (ordered, deterministic) vs agent orchestration (reasoning, non-deterministic)'],
            ['Event, trigger, webhook, polling; at-least-once nghĩa là sự kiện có thể đến hai lần', 'Events, triggers, webhooks, polling; at-least-once means an event may arrive twice'],
            ['Execution ID khác correlation ID: một cái định danh lần chạy, một cái nối các lời gọi liên quan', 'Execution ID vs correlation ID: one identifies the run, the other ties related calls together'],
            ['Retry, timeout và vì sao timeout không có nghĩa là thao tác bên kia đã thất bại', 'Retry, timeout, and why a timeout does not mean the remote operation failed'],
          ],
          ai: [['ChatGPT', ['Đối chiếu với phase 1', 'Compare with phase 1'], ['Ở phase 1, tôi dùng nhãn Jira `ai-picked`, marker trong comment PR và Set trong bộ nhớ runner để biết việc nào đã làm. Hãy liệt kê các kịch bản hỏng của cách này (runner restart, hai lần poll chồng nhau, comment bị xóa, mất phản hồi khi tạo PR) và giải thích một Workflow API có database, state machine và idempotency key giải quyết từng kịch bản thế nào.', 'In phase 1 I used the Jira label `ai-picked`, markers in PR comments and in-memory Sets in the runner to know what was done. List the failure scenarios of this approach (runner restart, overlapping polls, deleted comment, lost response when creating a PR) and explain how a Workflow API with a database, a state machine and idempotency keys handles each one.']]],
          res: [['n8n docs', 'https://docs.n8n.io/'], ['Idempotent receiver pattern', 'https://microservices.io/patterns/communication-style/idempotent-consumer.html']],
          pr: [
            ['Vẽ state machine với 13 trạng thái của roadmap (Created → … → Completed/Failed/Cancelled)', 'Draw the state machine with the roadmap\'s 13 states (Created → … → Completed/Failed/Cancelled)'],
            ['Ghi ra mỗi chuyển trạng thái do ai kích hoạt: n8n, agent, người, CI', 'Note who triggers each transition: n8n, agent, human, CI'],
            ['Lưu sơ đồ và bảng chuyển trạng thái vào `docs/architecture/workflow-states.md`', 'Save the diagram and transition table to `docs/architecture/workflow-states.md`'],
          ],
          done: [
            ['Có sơ đồ state machine và bảng chuyển trạng thái hợp lệ trong repo', 'The state diagram and the valid-transition table are in the repo'],
            ['Giải thích bằng lời 3 kịch bản sự kiện trùng và cách chặn', 'Explain out loud 3 duplicate-event scenarios and how to block them'],
          ],
        },
        {
          id: 's1-2', kind: 'core', h: '3–4',
          t: ['Tự viết state machine của WorkflowExecution: bảng chuyển hợp lệ, từ chối chuyển sai, ghi lịch sử', 'Write the WorkflowExecution state machine yourself: valid-transition table, reject invalid moves, record history'],
          goal: ['Mọi chuyển trạng thái đi qua một chỗ duy nhất trong .NET, chuyển sai bị từ chối và mọi thay đổi có dòng lịch sử.', 'Every transition goes through one place in .NET, invalid moves are rejected and every change has a history row.'],
          learn: [
            ['Entity WorkflowExecution (Id, JiraIssueKey, CurrentState, Status, CorrelationId, Version) và WorkflowHistory', 'The WorkflowExecution (Id, JiraIssueKey, CurrentState, Status, CorrelationId, Version) and WorkflowHistory entities'],
            ['Bảng chuyển trạng thái là dữ liệu, không rải `if` khắp nơi', 'The transition table is data, not `if`s scattered everywhere'],
            ['Quy tắc nghiệp vụ nằm trong domain, không nằm trong n8n hay prompt', 'Business rules live in the domain, not in n8n or in a prompt'],
          ],
          ai: [['Claude', ['Review state machine sau khi tự viết', 'Review the state machine after you write it'], ['Tôi tự viết state machine cho WorkflowExecution: <dán code>. Đừng viết lại. Hãy chỉ ra chuyển trạng thái nào còn thiếu hoặc không nên cho phép, chỗ nào hai request đồng thời có thể làm hỏng trạng thái, và test nào tôi nên thêm.', 'I wrote the WorkflowExecution state machine myself: <paste code>. Do not rewrite it. Point out missing transitions or ones that should not be allowed, where two concurrent requests could corrupt the state, and which tests I should add.']]],
          res: [['.NET architecture: DDD', MS + '/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/']],
          pr: [
            ['Tạo `apps/workflow-api` với hai entity và migration', 'Create `apps/workflow-api` with the two entities and a migration'],
            ['Viết `WorkflowExecution.TransitionTo(state, actor, reason)` kiểm tra bảng chuyển hợp lệ', 'Write `WorkflowExecution.TransitionTo(state, actor, reason)` that checks the valid-transition table'],
            ['Viết unit test cho mọi chuyển hợp lệ và một số chuyển sai', 'Write unit tests for every valid transition and several invalid ones'],
          ],
          done: [
            ['Chuyển sai (ví dụ Created → Completed) ném lỗi và không ghi gì', 'An invalid move (e.g. Created → Completed) throws and writes nothing'],
            ['Mỗi chuyển hợp lệ có một dòng WorkflowHistory với người thực hiện và lý do', 'Each valid move has a WorkflowHistory row with actor and reason'],
          ],
        },
        {
          id: 's1-3', kind: 'build', h: '3–4',
          t: ['Lưu trạng thái với optimistic concurrency và mở API xem trạng thái, lịch sử', 'Persist state with optimistic concurrency and expose status and history APIs'],
          goal: ['Hai request cập nhật cùng lúc không ghi đè nhau, và ai cũng xem được một lần chạy đang ở đâu.', 'Two concurrent updates never overwrite each other, and anyone can see where a run stands.'],
          learn: [
            ['Optimistic concurrency: cột Version hoặc `xmin` của PostgreSQL, `DbUpdateConcurrencyException`', 'Optimistic concurrency: a Version column or PostgreSQL `xmin`, `DbUpdateConcurrencyException`'],
            ['Trả 409 Conflict khi phiên bản đã đổi', 'Return 409 Conflict when the version changed'],
            ['API đọc: trạng thái hiện tại và lịch sử theo thời gian', 'Read APIs: current state and history over time'],
          ],
          ai: [['Claude Code', ['Làm API và test đồng thời', 'Build the API and concurrency tests'], ['Trong `apps/workflow-api`, cấu hình optimistic concurrency cho WorkflowExecution (dùng `xmin` của PostgreSQL), thêm `POST /executions/{id}/transitions` (body: toState, actor, reason, expectedVersion) trả 409 khi phiên bản lệch, `GET /executions/{id}` và `GET /executions/{id}/history`. Viết test tích hợp bắn hai request chuyển trạng thái song song và kiểm tra chỉ một cái thắng.', 'In `apps/workflow-api`, configure optimistic concurrency for WorkflowExecution (using PostgreSQL `xmin`), add `POST /executions/{id}/transitions` (body: toState, actor, reason, expectedVersion) returning 409 on a version mismatch, `GET /executions/{id}` and `GET /executions/{id}/history`. Write an integration test that fires two transition requests in parallel and checks only one wins.']]],
          res: [['EF Core concurrency', MS + '/ef/core/saving/concurrency']],
          pr: [
            ['Cấu hình concurrency token và xử lý ngoại lệ thành 409', 'Configure the concurrency token and map the exception to 409'],
            ['Thêm 3 endpoint ở trên', 'Add the 3 endpoints above'],
            ['Viết test hai request song song', 'Write the two-parallel-requests test'],
          ],
          done: [
            ['Test song song: đúng một request thành công, request kia nhận 409', 'Parallel test: exactly one request succeeds, the other gets 409'],
            ['Xem được lịch sử của một lần chạy qua API', 'A run\'s history can be read through the API'],
          ],
        },
        {
          id: 's1-4', kind: 'core', h: '3–4',
          t: ['Làm tiếp nhận idempotent và workflow n8n gọi Workflow API thay cho nhãn Jira', 'Build idempotent intake and an n8n workflow that calls the Workflow API instead of Jira labels'],
          goal: ['Webhook Jira đến hai lần chỉ tạo một lần chạy; n8n định tuyến theo trạng thái API trả về.', 'A Jira webhook delivered twice creates only one run; n8n routes on the state the API returns.'],
          learn: [
            ['Idempotency key và ràng buộc unique là tuyến phòng thủ cuối', 'Idempotency keys and a unique constraint are the last line of defence'],
            ['Lưu kết quả của thao tác để lần gọi lặp trả lại đúng kết quả cũ', 'Store the operation result so a repeated call returns the same answer'],
            ['n8n chỉ điều phối: nhận sự kiện, gọi API, định tuyến, thông báo; không giữ trạng thái', 'n8n only orchestrates: receive, call the API, route, notify; it holds no state'],
          ],
          ai: [['ChatGPT', ['Thiết kế khóa idempotency', 'Design the idempotency key'], ['Webhook Jira có thể gửi lại cùng một sự kiện, và n8n có thể timeout sau khi API đã tạo xong lần chạy. Hãy giúp tôi chọn khóa idempotency cho `POST /executions` (từ issue key, loại sự kiện, webhook id hay header `Idempotency-Key`), cách lưu kết quả để trả lại khi gọi lặp, và ràng buộc unique nào trong database chặn trường hợp hai request đến cùng lúc.', 'A Jira webhook may resend the same event, and n8n may time out after the API already created the run. Help me choose the idempotency key for `POST /executions` (issue key, event type, webhook id or an `Idempotency-Key` header), how to store the result to return it on repeats, and which database unique constraint blocks two requests arriving at once.']]],
          res: [['n8n Webhook node', 'https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/']],
          pr: [
            ['Tự viết `POST /executions` nhận `Idempotency-Key`, có unique index và trả lại kết quả cũ khi gọi lặp', 'Write `POST /executions` yourself, taking an `Idempotency-Key`, with a unique index, returning the stored result on repeats'],
            ['Tạo workflow n8n `intake`: nhận webhook (hoặc poll) → gọi API → rẽ nhánh theo trạng thái', 'Create the n8n `intake` workflow: webhook (or poll) → call the API → branch on state'],
            ['Gửi cùng một webhook hai lần và một lần nữa sau khi giả lập timeout', 'Send the same webhook twice, and once more after simulating a timeout'],
          ],
          done: [
            ['Gửi trùng sự kiện không tạo lần chạy thứ hai', 'A duplicate event does not create a second run'],
            ['n8n không còn dùng nhãn Jira làm nguồn trạng thái', 'n8n no longer uses Jira labels as the source of state'],
          ],
        },
        {
          id: 's1-5', kind: 'eval', h: '2–3',
          t: ['Kiểm chứng bước 1: chuyển sai, sự kiện trùng, lần chạy lỗi và retry an toàn', 'Verify step 1: invalid transitions, duplicate events, failed runs and safe retry'],
          goal: ['Tiêu chí hoàn thành của bước 1 có test chứng minh, không chỉ có code.', 'Step 1\'s exit criteria are proven by tests, not only by code.'],
          learn: [
            ['Phân biệt lần chạy lỗi tạm thời (retry được) và lỗi vĩnh viễn (cần người)', 'Tell temporary failures (retryable) from permanent ones (need a person)'],
            ['Retry an toàn nghĩa là chạy lại không tạo tác động phụ trùng', 'A safe retry means running again creates no duplicate side effects'],
          ],
          ai: [['Claude', ['Tìm ca test còn thiếu', 'Find missing test cases'], ['Đây là danh sách test của Workflow API: <dán danh sách>. Hãy so với các tiêu chí: trạng thái được lưu, chuyển sai bị từ chối, sự kiện trùng không tạo lần chạy trùng, lịch sử xem được, lần chạy lỗi tìm được và retry an toàn. Liệt kê ca nào chưa được test.', 'Here is the Workflow API test list: <paste list>. Compare it with these criteria: state is persisted, invalid transitions are rejected, duplicate events create no duplicate runs, history is inspectable, a failed run can be found and safely retried. List which cases are not tested yet.']]],
          res: [['.NET testing', MS + '/dotnet/core/testing/']],
          pr: [
            ['Thêm endpoint liệt kê lần chạy theo trạng thái Failed', 'Add an endpoint that lists runs in the Failed state'],
            ['Thêm `POST /executions/{id}/retry` chỉ cho phép từ Failed và ghi lịch sử', 'Add `POST /executions/{id}/retry`, only allowed from Failed, with history'],
            ['Viết test cho từng tiêu chí của bước 1', 'Write a test for each of step 1\'s criteria'],
          ],
          done: [
            ['Có test xanh cho cả 5 tiêu chí của bước 1', 'There is a green test for each of step 1\'s 5 criteria'],
            ['Retry một lần chạy lỗi không tạo lần chạy mới', 'Retrying a failed run does not create a new run'],
          ],
        },
      ],
    },
    {
      id: 's2', title: ['LLM engineering', 'LLM engineering'],
      items: [
        {
          id: 's2-1', kind: 'learn', h: '2–3',
          t: ['Học token, context window, chi phí, structured output và rủi ro prompt injection', 'Learn tokens, context windows, cost, structured outputs and prompt injection risks'],
          goal: ['Ước được chi phí một lần gọi và giải thích vì sao đầu ra AI chỉ là đề xuất.', 'You can estimate the cost of a call and explain why AI output is only a proposal.'],
          learn: [
            ['Token, cửa sổ ngữ cảnh, độ trễ và chi phí đầu vào khác đầu ra', 'Tokens, context window, latency, and input vs output cost'],
            ['Chỉ dẫn hệ thống, developer và người dùng; version hóa prompt', 'System, developer and user instructions; prompt versioning'],
            ['Structured output theo JSON schema và cách kiểm tra lại phía code', 'Structured output against a JSON schema, and validating it again in code'],
            ['Ảo giác, độ bất định, chọn model và temperature', 'Hallucination, uncertainty, model choice and temperature'],
            ['Prompt injection: nội dung ticket hay repo có thể chứa chỉ dẫn độc hại', 'Prompt injection: ticket or repo content may contain malicious instructions'],
          ],
          ai: [['Claude', ['Học qua bài tập', 'Learn through exercises'], ['Dạy tôi về token và chi phí bằng một ví dụ cụ thể: một mô tả ticket 800 từ, context repo 20.000 token, đầu ra 1.500 token. Tính chi phí ước lượng với hai mức giá giả định khác nhau, rồi đố tôi 3 câu về cách giảm chi phí mà không giảm chất lượng.', 'Teach me tokens and cost with a concrete example: an 800-word ticket description, 20,000 tokens of repo context, 1,500 output tokens. Estimate the cost at two different assumed prices, then quiz me with 3 questions on cutting cost without cutting quality.']]],
          res: [['Anthropic docs', 'https://docs.anthropic.com/'], ['OpenAI platform docs', 'https://platform.openai.com/docs/'], ['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
          pr: [
            ['Đọc tài liệu về structured output của nhà cung cấp bạn chọn', 'Read the structured-output docs of the provider you pick'],
            ['Viết ghi chú ngắn: bảng giá, giới hạn context, rate limit của 2 model bạn định dùng', 'Write a short note: pricing, context limit and rate limits of the 2 models you plan to use'],
            ['Liệt kê 3 cách prompt injection có thể đi vào hệ thống qua Jira hoặc repo', 'List 3 ways prompt injection could enter the system through Jira or the repo'],
          ],
          done: [
            ['Tính được chi phí ước lượng của một lần phân tích yêu cầu', 'You can estimate the cost of one requirement analysis call'],
            ['Có ghi chú so sánh 2 model trong `docs/decisions/`', 'A note comparing 2 models is in `docs/decisions/`'],
          ],
        },
        {
          id: 's2-2', kind: 'core', h: '3–4',
          t: ['Tự viết lớp trừu tượng LLM trong .NET: interface nhà cung cấp, timeout, retry và fallback', 'Write the .NET LLM abstraction yourself: provider interface, timeout, retry and fallback'],
          goal: ['Mọi lời gọi model đi qua một interface có timeout, retry có giới hạn và đổi model dự phòng.', 'Every model call goes through one interface with a timeout, bounded retry and a fallback model.'],
          learn: [
            ['Interface nhà cung cấp tách code nghiệp vụ khỏi SDK cụ thể', 'A provider interface keeps business code apart from a specific SDK'],
            ['Retry khác nhau theo lỗi: rate limit thì backoff, lỗi validation thì không', 'Retry differs by error: back off on rate limits, never on validation errors'],
            ['`Microsoft.Extensions.AI` và `IChatClient` như một lựa chọn có sẵn', '`Microsoft.Extensions.AI` and `IChatClient` as a ready-made option'],
          ],
          ai: [['Claude', ['Review thiết kế sau khi tự viết', 'Review the design after you write it'], ['Tôi tự viết `ILlmClient` với timeout, retry và fallback: <dán code>. Đừng viết lại. Hãy chỉ ra lỗi nào đang bị retry mà không nên, chỗ nào có thể retry vô hạn, và cách để test các nhánh lỗi mà không gọi nhà cung cấp thật.', 'I wrote `ILlmClient` with timeout, retry and fallback myself: <paste code>. Do not rewrite it. Point out errors that are retried but should not be, any place that could retry forever, and how to test the error paths without calling the real provider.']]],
          res: [['.NET AI', MS + '/dotnet/ai/']],
          pr: [
            ['Tạo `services/agent-service` với `ILlmClient` và một bản cài đặt cho nhà cung cấp chính', 'Create `services/agent-service` with `ILlmClient` and one implementation for your main provider'],
            ['Thêm timeout, retry phân loại theo lỗi và model dự phòng', 'Add a timeout, error-classified retry and a fallback model'],
            ['Viết bản giả (fake) của client để test các nhánh lỗi', 'Write a fake client to test the error paths'],
          ],
          done: [
            ['Test: rate limit được retry có giới hạn, lỗi validation không retry', 'Test: rate limits get bounded retries, validation errors are not retried'],
            ['Model chính lỗi thì gọi model dự phòng và ghi lại', 'When the main model fails, the fallback is called and logged'],
          ],
        },
        {
          id: 's2-3', kind: 'build', h: '3–4',
          t: ['Làm AI BA trả phân tích yêu cầu có cấu trúc, kiểm tra theo schema và version prompt', 'Make the AI BA return a structured requirement analysis, validated against a schema, with versioned prompts'],
          goal: ['Đầu ra BA (summary, userStory, acceptanceCriteria, openQuestions, assumptions) luôn hợp lệ hoặc bị từ chối.', 'The BA output (summary, userStory, acceptanceCriteria, openQuestions, assumptions) is always valid or rejected.'],
          learn: [
            ['Prompt template lưu như file có version, không viết cứng trong code', 'Prompt templates stored as versioned files, not hard-coded'],
            ['Kiểm tra lại schema ở phía code dù model đã hỗ trợ structured output', 'Validate the schema again in code even if the model supports structured output'],
            ['Đầu ra sai: sinh lại có giới hạn, rồi chuyển người', 'Invalid output: bounded regeneration, then hand to a person'],
          ],
          ai: [['Claude Code', ['Dựng pipeline BA', 'Build the BA pipeline'], ['Trong `services/agent-service`, thêm `RequirementAnalysis` (summary, userStory, acceptanceCriteria[], openQuestions[], assumptions[]) và JSON schema tương ứng trong `contracts/schemas/`. Prompt nằm ở `agents/ba/prompts/analyze.v1.md`. Gọi `ILlmClient`, kiểm tra schema, sinh lại tối đa 2 lần nếu sai, rồi trả lỗi có thể đọc được. Ghi lại phiên bản prompt và model trong kết quả.', 'In `services/agent-service`, add `RequirementAnalysis` (summary, userStory, acceptanceCriteria[], openQuestions[], assumptions[]) and its JSON schema in `contracts/schemas/`. The prompt lives in `agents/ba/prompts/analyze.v1.md`. Call `ILlmClient`, validate against the schema, regenerate at most 2 times when invalid, then return a readable error. Record the prompt and model versions in the result.']]],
          res: [['Anthropic docs', 'https://docs.anthropic.com/'], ['JSON Schema', 'https://json-schema.org/']],
          pr: [
            ['Viết schema và prompt v1', 'Write the schema and prompt v1'],
            ['Nối pipeline: gọi model → kiểm tra → sinh lại có giới hạn', 'Wire the pipeline: call the model → validate → bounded regeneration'],
            ['Chạy với 3 ticket rõ ràng trong backlog thực hành và lưu kết quả', 'Run it on 3 clear tickets from the practice backlog and keep the results'],
          ],
          done: [
            ['Cả 3 ticket cho đầu ra hợp lệ theo schema', 'All 3 tickets produce schema-valid output'],
            ['Mỗi kết quả ghi rõ phiên bản prompt và model', 'Each result records the prompt and model versions'],
          ],
        },
        {
          id: 's2-4', kind: 'eval', h: '2–3',
          t: ['Ghi token và chi phí, xử lý đầu ra sai và thử prompt injection cơ bản', 'Record tokens and cost, handle invalid output and try basic prompt injection'],
          goal: ['Mỗi lời gọi LLM có dòng ghi token, chi phí ước lượng, độ trễ, model và prompt version.', 'Every LLM call has a row with tokens, estimated cost, latency, model and prompt version.'],
          learn: [
            ['Bảng `LlmCall`: lời gọi nào, của lần chạy nào, tốn bao nhiêu', 'An `LlmCall` table: which call, for which run, at what cost'],
            ['Không ghi nội dung nhạy cảm hay bí mật vào log', 'Never log sensitive content or secrets'],
            ['Thử injection để biết hệ thống phản ứng thế nào trước khi người khác thử', 'Try injection yourself before someone else does'],
          ],
          ai: [['ChatGPT', ['Soạn ca thử injection', 'Draft injection cases'], ['Soạn 5 mô tả ticket Jira chứa prompt injection khác nhau (yêu cầu bỏ qua chỉ dẫn, đòi in ra system prompt, chèn chỉ dẫn trong khối code, giả làm tin nhắn của admin, yêu cầu gọi công cụ ghi). Mỗi ca kèm kết quả mong muốn của một AI BA an toàn.', 'Write 5 Jira ticket descriptions containing different prompt injections (ignore instructions, print the system prompt, instructions hidden in a code block, pretending to be an admin, asking to call a write tool). For each, give the expected behaviour of a safe AI BA.']]],
          res: [['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
          pr: [
            ['Thêm bảng `LlmCall` gắn với execution id và correlation id', 'Add an `LlmCall` table linked to the execution and correlation ids'],
            ['Chạy 5 ca injection và ghi kết quả vào `tests/evaluation/injection.md`', 'Run the 5 injection cases and record results in `tests/evaluation/injection.md`'],
            ['Sửa prompt hoặc kiểm tra đầu ra cho ca nào bị lọt', 'Fix the prompt or output checks for any case that got through'],
          ],
          done: [
            ['Truy vấn được tổng chi phí LLM của một lần chạy', 'The total LLM cost of a run can be queried'],
            ['Kết quả 5 ca injection được ghi lại, ca lọt đã có biện pháp', 'The 5 injection results are recorded; any leak has a fix'],
          ],
        },
      ],
    },
    {
      id: 's3', title: ['Tool calling và MCP', 'Tool calling and MCP'],
      items: [
        {
          id: 's3-1', kind: 'learn', h: '2',
          t: ['Học tool calling, schema công cụ, phân quyền đọc/ghi và các khái niệm MCP', 'Learn tool calling, tool schemas, read/write authorization and MCP concepts'],
          goal: ['Giải thích được vòng gọi công cụ và vì sao kết quả công cụ cũng là dữ liệu không tin cậy.', 'You can explain the tool-call loop and why tool results are also untrusted data.'],
          learn: [
            ['Model đề xuất lời gọi công cụ, code của bạn mới là bên thực thi', 'The model proposes a tool call; your code is what executes it'],
            ['Kiểm tra tham số và quyền trước khi thực thi', 'Validate arguments and permissions before executing'],
            ['MCP: host, client, server; tools, resources, prompts', 'MCP: host, client, server; tools, resources, prompts'],
            ['Công cụ chỉ đọc khác công cụ ghi; công cụ ghi cần duyệt', 'Read-only vs write tools; write tools need approval'],
          ],
          ai: [['Claude', ['Học bằng sơ đồ', 'Learn with a diagram'], ['Vẽ (bằng văn bản) luồng gọi công cụ giữa ứng dụng .NET của tôi, model và một công cụ `get_jira_issue`, chỉ rõ chỗ nào kiểm tra tham số, chỗ nào kiểm tra quyền, chỗ nào ghi audit. Sau đó giải thích MCP thay đổi luồng này thế nào.', 'Draw (as text) the tool-call flow between my .NET app, the model and a `get_jira_issue` tool, marking where arguments are validated, where permissions are checked and where audit is written. Then explain how MCP changes this flow.']]],
          res: [['Model Context Protocol', 'https://modelcontextprotocol.io/'], ['Anthropic docs: tool use', 'https://docs.anthropic.com/']],
          pr: [
            ['Đọc phần tool use của nhà cung cấp và phần Concepts của MCP', 'Read your provider\'s tool-use docs and the MCP Concepts section'],
            ['Vẽ sơ đồ luồng gọi công cụ có các điểm kiểm tra', 'Draw the tool-call flow with its check points'],
          ],
          done: [
            ['Sơ đồ luồng gọi công cụ nằm trong `docs/architecture/`', 'The tool-call flow diagram is in `docs/architecture/`'],
          ],
        },
        {
          id: 's3-2', kind: 'core', h: '3–4',
          t: ['Tự viết sổ đăng ký công cụ: schema, kiểm tra tham số, phân quyền theo vai trò và audit', 'Write the tool registry yourself: schemas, argument validation, role-based authorization and audit'],
          goal: ['Agent chỉ gọi được công cụ của vai trò mình, tham số sai bị chặn, mọi lời gọi có audit.', 'An agent can only call its role\'s tools, bad arguments are blocked and every call is audited.'],
          learn: [
            ['Công cụ = tên, mô tả, schema đầu vào, quyền, timeout', 'A tool = name, description, input schema, permission, timeout'],
            ['Phân quyền theo vai trò agent (BA, Developer, Reviewer, QA)', 'Authorization by agent role (BA, Developer, Reviewer, QA)'],
            ['Timeout và lỗi công cụ trả về model như dữ liệu, không làm sập vòng lặp', 'Tool timeouts and failures go back to the model as data, not crashes'],
          ],
          ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết `ToolRegistry` với phân quyền theo vai trò: <dán code>. Đừng viết lại. Hãy tìm cách một agent gọi được công cụ ngoài vai trò, tham số nào có thể lọt kiểm tra, và chỗ nào audit có thể bị thiếu khi công cụ lỗi.', 'I wrote a role-based `ToolRegistry` myself: <paste code>. Do not rewrite it. Find ways an agent could call a tool outside its role, arguments that could slip past validation, and where audit could be missing when a tool fails.']]],
          res: [['Model Context Protocol', 'https://modelcontextprotocol.io/']],
          pr: [
            ['Định nghĩa `ITool` và `ToolRegistry` với danh sách công cụ theo vai trò', 'Define `ITool` and a `ToolRegistry` with tools listed per role'],
            ['Kiểm tra tham số theo schema và quyền trước khi chạy', 'Validate arguments against the schema and check permission before running'],
            ['Ghi bảng `ToolCall` (agent, công cụ, tham số đã che, kết quả, thời gian)', 'Write a `ToolCall` table (agent, tool, redacted args, outcome, duration)'],
          ],
          done: [
            ['Test: BA gọi công cụ của Developer bị từ chối và có audit', 'Test: the BA calling a Developer tool is rejected and audited'],
            ['Test: công cụ timeout trả lỗi có kiểm soát', 'Test: a tool timeout returns a controlled error'],
          ],
        },
        {
          id: 's3-3', kind: 'build', h: '3–4',
          t: ['Cấp cho BA bộ công cụ giới hạn và dựng một MCP server chỉ đọc cho tài liệu', 'Give the BA a limited toolset and build one read-only MCP server for documentation'],
          goal: ['BA đọc Jira, metadata project và tài liệu đã duyệt; chỉ ghi được bản nháp comment sau khi người duyệt.', 'The BA reads Jira, project metadata and approved docs; it can only write a draft comment after human approval.'],
          learn: [
            ['Bắt đầu bằng công cụ chỉ đọc, công cụ ghi phải qua duyệt', 'Start with read-only tools; write tools require approval'],
            ['Không cấp shell, filesystem, Git hay công cụ production cho agent', 'Never give the agent shell, filesystem, Git or production tools'],
            ['MCP C# SDK để dựng server', 'The MCP C# SDK for building a server'],
          ],
          ai: [['Claude Code', ['Dựng MCP server', 'Build the MCP server'], ['Dùng MCP C# SDK, tạo `services/docs-mcp` mở 2 công cụ chỉ đọc: `search_requirements(query)` và `read_document(path)` chỉ trong thư mục `docs/approved/`, chặn `..` và đường dẫn tuyệt đối. Viết test chứng minh không đọc được file ngoài thư mục.', 'Using the MCP C# SDK, create `services/docs-mcp` exposing 2 read-only tools: `search_requirements(query)` and `read_document(path)` limited to `docs/approved/`, blocking `..` and absolute paths. Write tests proving files outside that folder cannot be read.']]],
          res: [['MCP C# SDK', 'https://github.com/modelcontextprotocol/csharp-sdk'], ['Model Context Protocol', 'https://modelcontextprotocol.io/']],
          pr: [
            ['Cài 4 công cụ đọc: Jira issue, metadata project, tìm yêu cầu, đọc tài liệu', 'Implement 4 read tools: Jira issue, project metadata, search requirements, read docs'],
            ['Cài công cụ ghi `add_draft_jira_comment` cần bước duyệt', 'Implement the write tool `add_draft_jira_comment` behind an approval step'],
            ['Dựng `services/docs-mcp` và nối vào agent', 'Build `services/docs-mcp` and connect it to the agent'],
          ],
          done: [
            ['BA lấy được thông tin ticket và tài liệu qua công cụ', 'The BA gets ticket and doc information through tools'],
            ['Comment chỉ xuất hiện trên Jira sau khi được duyệt', 'A comment appears on Jira only after approval'],
          ],
        },
        {
          id: 's3-4', kind: 'eval', h: '2',
          t: ['Kiểm chứng bước 3: chọn đúng công cụ, chặn hành động không được phép, xử lý lỗi công cụ', 'Verify step 3: correct tool choice, unauthorized actions blocked, tool failures handled'],
          goal: ['Tiêu chí của bước 3 có test và log audit chứng minh.', 'Step 3\'s criteria are proven by tests and audit logs.'],
          learn: [
            ['Đánh giá việc chọn công cụ bằng các câu hỏi có đáp án biết trước', 'Evaluate tool choice with questions whose answers are known'],
            ['Nội dung trả về từ công cụ có thể chứa injection', 'Tool results can carry injection'],
          ],
          ai: [['ChatGPT', ['Soạn bộ câu hỏi đánh giá', 'Draft an evaluation set'], ['Soạn 10 yêu cầu gửi cho AI BA, mỗi yêu cầu kèm công cụ đúng cần gọi và tham số mong đợi. Thêm 2 ca tài liệu trả về chứa chỉ dẫn độc hại để kiểm tra agent không làm theo.', 'Write 10 requests for the AI BA, each with the correct tool to call and the expected arguments. Add 2 cases where a document returned by a tool contains malicious instructions, to check the agent does not follow them.']]],
          res: [['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
          pr: [
            ['Chạy 10 ca và chấm công cụ, tham số', 'Run the 10 cases and score tool and arguments'],
            ['Thử gọi công cụ ghi không qua duyệt và thử tham số sai', 'Try calling the write tool without approval and with bad arguments'],
            ['Ghi kết quả vào `tests/evaluation/tools.md`', 'Record results in `tests/evaluation/tools.md`'],
          ],
          done: [
            ['Tỷ lệ chọn đúng công cụ được ghi lại', 'The correct-tool rate is recorded'],
            ['Hành động không được phép bị chặn và có dòng audit', 'Unauthorized actions are blocked and audited'],
          ],
        },
      ],
    },
    {
      id: 's4', title: ['Kiến trúc agent: AI BA', 'Agent architecture: the AI BA'],
      items: [
        {
          id: 's4-1', kind: 'learn', h: '2',
          t: ['Học vòng lặp agent, giới hạn vòng lặp, điều kiện dừng, human-in-the-loop và rủi ro bộ nhớ', 'Learn the agent loop, bounded iteration, stop conditions, human-in-the-loop and memory risks'],
          goal: ['Thiết kế được vòng lặp agent có điểm dừng rõ và điểm chờ người duyệt.', 'You can design an agent loop with clear stop points and a human approval point.'],
          learn: [
            ['Quan sát → suy luận → hành động → quan sát', 'Observe → reason → act → observe'],
            ['Giới hạn số bước, số lần gọi công cụ và thời gian', 'Limits on steps, tool calls and time'],
            ['Lập kế hoạch tách khỏi thực thi', 'Planning kept apart from execution'],
            ['Bộ nhớ dài hạn có thể mang dữ liệu sai hoặc độc hại sang lần chạy sau', 'Long-term memory can carry wrong or malicious data into later runs'],
          ],
          ai: [['Claude', ['Phản biện thiết kế', 'Critique the design'], ['Đây là thiết kế vòng lặp AI BA của tôi: <mô tả các bước, giới hạn, điểm dừng>. Hãy chỉ ra trường hợp vòng lặp chạy mãi, trường hợp agent tự bịa thông tin thay vì hỏi, và chỗ cần đặt điểm duyệt của người.', 'Here is my AI BA loop design: <describe steps, limits, stop points>. Point out cases where it could loop forever, where the agent would invent information instead of asking, and where a human approval point belongs.']]],
          res: [['Anthropic: Building effective agents', 'https://www.anthropic.com/engineering/building-effective-agents']],
          pr: [
            ['Đọc bài "Building effective agents"', 'Read "Building effective agents"'],
            ['Viết thiết kế vòng lặp BA: bước, giới hạn, điều kiện dừng, điểm duyệt', 'Write the BA loop design: steps, limits, stop conditions, approval point'],
          ],
          done: [
            ['Thiết kế vòng lặp BA có giới hạn số bước và điểm duyệt rõ ràng', 'The BA loop design has a step limit and a clear approval point'],
          ],
        },
        {
          id: 's4-2', kind: 'core', h: '4–5',
          t: ['Tự viết vòng lặp AI BA: đọc ticket, lấy ngữ cảnh, phát hiện thiếu thông tin, chờ duyệt, lưu yêu cầu đã duyệt', 'Write the AI BA loop yourself: read the ticket, gather context, flag missing info, wait for approval, save the approved requirement'],
          goal: ['Một ticket đi qua BA thành yêu cầu đã duyệt, lưu như artifact có version, trước khi được code.', 'A ticket goes through the BA into an approved, versioned requirement artifact before any coding.'],
          learn: [
            ['Trạng thái AwaitingRequirementApproval chặn bước sau cho tới khi người duyệt', 'The AwaitingRequirementApproval state blocks the next step until a person approves'],
            ['Thiếu thông tin thì hỏi lại, không đoán', 'Missing information is asked, not guessed'],
            ['Artifact yêu cầu đã duyệt là đầu vào duy nhất cho Developer', 'The approved requirement artifact is the only input to the Developer'],
          ],
          ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết vòng lặp AI BA: <dán code>. Đừng viết lại. Kiểm tra: có giới hạn bước chưa, có chỗ nào agent có thể bỏ qua bước duyệt, câu hỏi mở có được chuyển cho người không, và artifact đã duyệt có thể bị sửa sau khi duyệt không.', 'I wrote the AI BA loop myself: <paste code>. Do not rewrite it. Check: is there a step limit, can the agent skip the approval step anywhere, are open questions passed to a person, and can the approved artifact be changed after approval.']]],
          res: [['n8n docs', 'https://docs.n8n.io/']],
          pr: [
            ['Viết vòng lặp với giới hạn bước và chỉ dùng công cụ của BA', 'Write the loop with a step limit, using BA tools only'],
            ['Chuyển sang AwaitingRequirementApproval và gửi yêu cầu duyệt (Jira hoặc Telegram) qua n8n', 'Move to AwaitingRequirementApproval and send the approval request (Jira or Telegram) through n8n'],
            ['Khi duyệt: lưu `RequirementArtifact` bất biến có version và người duyệt', 'On approval: save an immutable, versioned `RequirementArtifact` with the approver'],
          ],
          done: [
            ['Không có đường nào chuyển sang ImplementationPlanning khi chưa duyệt', 'There is no path to ImplementationPlanning without approval'],
            ['Artifact đã duyệt không sửa được, chỉ tạo được phiên bản mới', 'An approved artifact cannot be edited, only superseded by a new version'],
          ],
        },
        {
          id: 's4-3', kind: 'eval', h: '2–3',
          t: ['Đánh giá BA trên bộ yêu cầu đại diện: rõ ràng, mơ hồ, mâu thuẫn, nhạy cảm bảo mật', 'Evaluate the BA on representative requirements: clear, ambiguous, conflicting, security-sensitive'],
          goal: ['Biết BA làm tốt và làm sai ở đâu, với số liệu trên một bộ ca cố định.', 'You know where the BA does well and where it fails, with numbers on a fixed case set.'],
          learn: [
            ['Bộ đánh giá cố định để so sánh khi đổi prompt hay model', 'A fixed evaluation set to compare prompt or model changes'],
            ['Chấm theo tiêu chí: có hỏi khi thiếu thông tin không, AC có kiểm chứng được không', 'Score by criteria: does it ask when info is missing, are the ACs verifiable'],
          ],
          ai: [['ChatGPT', ['Soạn bộ ca', 'Draft the case set'], ['Đây là backlog thực hành của TaskLite: <dán backlog>. Bổ sung cho đủ 12 ca (4 rõ ràng, 3 mơ hồ, 3 mâu thuẫn nội tại, 2 nhạy cảm bảo mật) và với mỗi ca ghi điều một BA tốt phải làm: AC chính, câu hỏi phải hỏi, điều không được tự giả định.', 'Here is the TaskLite practice backlog: <paste backlog>. Extend it to 12 cases (4 clear, 3 ambiguous, 3 self-contradictory, 2 security-sensitive) and for each note what a good BA must do: key ACs, questions it must ask, assumptions it must not make.']]],
          res: [['Anthropic docs', 'https://docs.anthropic.com/']],
          pr: [
            ['Lưu bộ ca vào `tests/evaluation/ba/`', 'Save the cases under `tests/evaluation/ba/`'],
            ['Viết script chạy BA trên toàn bộ ca và lưu kết quả', 'Write a script that runs the BA on all cases and stores results'],
            ['Chấm thủ công lần đầu, ghi tỷ lệ đạt theo nhóm', 'Score manually the first time and record pass rates per group'],
          ],
          done: [
            ['Có bảng kết quả theo 4 nhóm ca', 'There is a results table for the 4 case groups'],
            ['Ca mơ hồ và mâu thuẫn được BA nêu câu hỏi thay vì tự đoán', 'Ambiguous and conflicting cases get questions from the BA instead of guesses'],
          ],
        },
      ],
    },
    {
      id: 's5', title: ['AI Developer và AI Reviewer', 'AI Developer and AI Reviewer'],
      items: [
        {
          id: 's5-1', kind: 'learn', h: '2',
          t: ['Học lấy ngữ cảnh repo, kế hoạch triển khai, thay đổi dạng patch và review độc lập', 'Learn repo context gathering, implementation planning, patch-based changes and independent review'],
          goal: ['Biết đưa cho Developer agent ngữ cảnh gì, và vì sao reviewer phải độc lập.', 'You know what context to give the Developer agent and why the reviewer must be independent.'],
          learn: [
            ['Lấy ngữ cảnh: tìm file liên quan, index codebase, giới hạn token', 'Context gathering: find related files, index the codebase, token limits'],
            ['Kế hoạch triển khai trước code: file nào đổi, test nào thêm', 'Plan before code: which files change, which tests are added'],
            ['Thay đổi dạng patch dễ review và dễ hoàn tác', 'Patch-based changes are easy to review and revert'],
            ['Mức độ phát hiện review và giới hạn số vòng sửa', 'Review finding severity and fix-round limits'],
          ],
          ai: [['Claude', ['Rút kinh nghiệm từ phase 1', 'Learn from phase 1'], ['Ở phase 1, agent viết code (agy) đôi khi dừng để hỏi những chi tiết nhỏ, quên commit, và reviewer (Claude Code) đổi kết luận khi chỉ có commit gộp main. Dựa trên đó, hãy đề xuất: ngữ cảnh tối thiểu cho Developer agent, định dạng kế hoạch triển khai cần duyệt, và cách làm review ổn định hơn giữa các lần chạy.', 'In phase 1, the coding agent (agy) sometimes stopped to ask about small details and forgot to commit, and the reviewer (Claude Code) changed its verdict on a commit that only merged main. Based on that, propose: the minimum context for the Developer agent, a reviewable implementation-plan format, and how to make reviews more stable across runs.']]],
          res: [['Anthropic: Building effective agents', 'https://www.anthropic.com/engineering/building-effective-agents']],
          pr: [
            ['Viết định dạng `ImplementationPlan` (file, thay đổi, test, rủi ro) vào `contracts/schemas/`', 'Write an `ImplementationPlan` format (files, changes, tests, risks) in `contracts/schemas/`'],
            ['Viết định dạng `ReviewFinding` (file, dòng, mức độ, mô tả, đề xuất)', 'Write a `ReviewFinding` format (file, line, severity, description, suggestion)'],
          ],
          done: [
            ['Hai schema ImplementationPlan và ReviewFinding nằm trong repo', 'The ImplementationPlan and ReviewFinding schemas are in the repo'],
          ],
        },
        {
          id: 's5-2', kind: 'build', h: '4–6',
          t: ['Làm AI Developer: nhận yêu cầu đã duyệt, lập kế hoạch, code trong worktree riêng, chạy lệnh được phép, nộp bằng chứng', 'Build the AI Developer: take the approved requirement, plan, code in its own worktree, run allowed commands, hand in evidence'],
          goal: ['Một feature nhỏ được Developer agent làm trong worktree cách biệt, kèm tóm tắt thay đổi và bằng chứng build/test do hệ thống chạy.', 'A small feature is implemented by the Developer agent in an isolated worktree, with a change summary and build/test evidence run by the system.'],
          learn: [
            ['Bước lập kế hoạch có thể cần duyệt trước khi code', 'The planning step may need approval before coding'],
            ['Agent không tự báo đạt; hệ thống chạy build/test và lưu kết quả', 'The agent never reports success itself; the system runs build/test and keeps the results'],
            ['Dùng lại kinh nghiệm worktree và chạy CLI agent từ phase 1', 'Reuse the worktree and agent-CLI experience from phase 1'],
          ],
          ai: [['Claude Code', ['Làm Developer service', 'Build the Developer service'], ['Trong `services/agent-service`, thêm Developer agent: nhận `RequirementArtifact` đã duyệt, sinh `ImplementationPlan` theo schema, tạo worktree `ai/<key>-<slug>`, gọi CLI agent viết code với prompt có kế hoạch, rồi chạy các lệnh trong danh sách cho phép (`dotnet build`, `dotnet test`, `ng build`, `ng test`). Lưu exit code và log làm bằng chứng gắn với execution id. Agent không được tự commit hay push; service làm việc đó sau khi kiểm tra.', 'In `services/agent-service`, add the Developer agent: take an approved `RequirementArtifact`, produce a schema-valid `ImplementationPlan`, create the worktree `ai/<key>-<slug>`, call the coding CLI with a prompt containing the plan, then run the allowlisted commands (`dotnet build`, `dotnet test`, `ng build`, `ng test`). Store exit codes and logs as evidence linked to the execution id. The agent must not commit or push; the service does that after checking.']]],
          res: [['git worktree', 'https://git-scm.com/docs/git-worktree']],
          pr: [
            ['Sinh và lưu ImplementationPlan, chuyển trạng thái ImplementationPlanning → Coding', 'Produce and store the ImplementationPlan, move ImplementationPlanning → Coding'],
            ['Tạo worktree, chạy CLI agent với giới hạn thời gian', 'Create the worktree and run the coding CLI with a time limit'],
            ['Service chạy lệnh được phép, lưu bằng chứng, rồi commit theo quy ước', 'The service runs allowed commands, stores evidence, then commits by convention'],
          ],
          done: [
            ['Một feature nhỏ được làm xong trong worktree riêng', 'One small feature is implemented in its own worktree'],
            ['Bằng chứng build/test do service chạy được lưu và gắn với lần chạy', 'Build/test evidence run by the service is stored and linked to the run'],
          ],
        },
        {
          id: 's5-3', kind: 'build', h: '3–4',
          t: ['Làm AI Reviewer trả phát hiện có cấu trúc kèm file, dòng và mức độ, không sửa code', 'Build the AI Reviewer returning structured findings with file, line and severity, never editing code'],
          goal: ['Reviewer nhận diff và AC đã duyệt, trả danh sách phát hiện hợp lệ theo schema, chỉ có quyền đọc.', 'The Reviewer takes the diff and the approved ACs and returns schema-valid findings, with read-only access.'],
          learn: [
            ['Review theo trục: đúng chức năng, dễ bảo trì, bảo mật, độ phủ test', 'Review along axes: correctness, maintainability, security, test coverage'],
            ['Phát hiện phải có bằng chứng file và dòng', 'Findings must cite file and line'],
            ['Reviewer không thay CI và không thay người duyệt', 'The reviewer replaces neither CI nor the human reviewer'],
          ],
          ai: [['Claude Code', ['Làm Reviewer service', 'Build the Reviewer service'], ['Thêm Reviewer agent: đầu vào là diff của nhánh so với main, `RequirementArtifact` đã duyệt và kết quả CI. Đầu ra là mảng `ReviewFinding` theo schema và một kết luận approve/request_changes/block suy ra bằng code từ mức độ phát hiện, không để model tự chọn kết luận. Reviewer chỉ có công cụ đọc file.', 'Add the Reviewer agent: input is the branch diff vs main, the approved `RequirementArtifact` and the CI results. Output is an array of schema-valid `ReviewFinding`s and an approve/request_changes/block verdict derived in code from finding severities, not chosen by the model. The Reviewer only has file-read tools.']]],
          res: [['GitHub REST API: pull request reviews', 'https://docs.github.com/rest/pulls/reviews']],
          pr: [
            ['Viết Reviewer với công cụ chỉ đọc', 'Write the Reviewer with read-only tools'],
            ['Suy ra kết luận bằng code từ mức độ phát hiện', 'Derive the verdict in code from finding severities'],
            ['Đăng phát hiện lên PR dạng review comment theo dòng', 'Post findings on the PR as line-level review comments'],
          ],
          done: [
            ['Phát hiện hợp lệ theo schema và trỏ đúng file, dòng', 'Findings are schema-valid and point to the right file and line'],
            ['Kết luận được tính bằng code, không do model chọn', 'The verdict is computed in code, not chosen by the model'],
          ],
        },
        {
          id: 's5-4', kind: 'core', h: '3',
          t: ['Tự viết vòng sửa có giới hạn: chỉ sửa phát hiện hành động được, dừng sau N vòng', 'Write the bounded fix loop yourself: fix only actionable findings, stop after N rounds'],
          goal: ['Vòng Developer ↔ Reviewer luôn kết thúc: đạt, hoặc hết vòng và chuyển người.', 'The Developer ↔ Reviewer loop always ends: passed, or out of rounds and handed to a person.'],
          learn: [
            ['Chỉ gửi lại phát hiện mức blocker/major có hành động cụ thể', 'Only send back blocker/major findings with a concrete action'],
            ['Đếm vòng trong Workflow API, không trong bộ nhớ hay comment', 'Count rounds in the Workflow API, not in memory or comments'],
          ],
          ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết vòng sửa có giới hạn giữa Developer và Reviewer: <dán code>. Đừng viết lại. Tìm trường hợp vòng lặp không kết thúc, trường hợp đếm vòng sai khi service restart, và trường hợp sửa xong lại phá test khác mà không bị phát hiện.', 'I wrote the bounded Developer ↔ Reviewer fix loop myself: <paste code>. Do not rewrite it. Find cases where it never ends, where the round count goes wrong after a service restart, and where a fix breaks another test without being caught.']]],
          res: [['Anthropic: Building effective agents', 'https://www.anthropic.com/engineering/building-effective-agents']],
          pr: [
            ['Lưu số vòng sửa trong WorkflowExecution', 'Store the fix-round count in WorkflowExecution'],
            ['Lọc phát hiện hành động được và gửi cho Developer', 'Filter actionable findings and send them to the Developer'],
            ['Hết vòng: chuyển trạng thái chờ người và thông báo', 'Out of rounds: move to a waiting-for-human state and notify'],
          ],
          done: [
            ['Test: vòng sửa dừng đúng sau N lần kể cả khi service restart giữa chừng', 'Test: the fix loop stops after N rounds even if the service restarts mid-way'],
          ],
        },
      ],
    },
    {
      id: 's6', title: ['Tích hợp end-to-end của POC', 'POC end-to-end integration'],
      items: [
        {
          id: 's6-1', kind: 'build', h: '4–5',
          t: ['Nối trọn luồng Jira → n8n → Workflow API → BA → duyệt → Developer → build/test → Reviewer → PR với correlation ID', 'Connect the whole Jira → n8n → Workflow API → BA → approval → Developer → build/test → Reviewer → PR flow with correlation IDs'],
          goal: ['Một ticket đi hết luồng; mọi log và lời gọi mang cùng correlation ID.', 'One ticket travels the whole flow; every log and call carries the same correlation ID.'],
          learn: [
            ['Truyền correlation ID qua header giữa n8n, API, agent và runner', 'Pass the correlation ID through headers between n8n, the API, agents and the runner'],
            ['Tạo PR idempotent: phản hồi bị mất thì tìm PR theo nhánh trước khi tạo lại', 'Idempotent PR creation: if the response is lost, look the PR up by branch before creating it again'],
            ['Giữ bằng chứng build/test gắn với PR', 'Keep build/test evidence linked to the PR'],
          ],
          ai: [['Claude Code', ['Nối các phần', 'Wire the parts together'], ['Nối các service: n8n gọi Workflow API, API gọi agent-service theo trạng thái, mọi request mang header `X-Correlation-Id` và ghi vào log có cấu trúc. Tạo PR idempotent: trước khi tạo, tìm PR mở theo nhánh; nếu đã có thì dùng lại. Thân PR có link tới yêu cầu đã duyệt, kế hoạch, bằng chứng build/test và phát hiện review.', 'Wire the services: n8n calls the Workflow API, the API calls agent-service by state, every request carries an `X-Correlation-Id` header that goes into structured logs. Make PR creation idempotent: before creating, look up an open PR by branch and reuse it. The PR body links the approved requirement, the plan, build/test evidence and review findings.']]],
          res: [['GitHub REST API: pulls', 'https://docs.github.com/rest/pulls/pulls']],
          pr: [
            ['Thêm middleware đọc và truyền correlation ID', 'Add middleware that reads and forwards the correlation ID'],
            ['Nối n8n với từng trạng thái của Workflow API', 'Connect n8n to each Workflow API state'],
            ['Chạy một ticket thật từ đầu tới PR', 'Run one real ticket from start to PR'],
          ],
          done: [
            ['Một ticket Jira đi trọn luồng tới PR', 'One Jira ticket completes the flow up to the PR'],
            ['Lọc log theo correlation ID thấy toàn bộ lần chạy', 'Filtering logs by correlation ID shows the whole run'],
          ],
        },
        {
          id: 's6-2', kind: 'eval', h: '3–4',
          t: ['Viết test lỗi end-to-end: service chết giữa chừng, webhook trùng, timeout, mất phản hồi khi tạo PR', 'Write end-to-end failure tests: service crash mid-run, duplicate webhook, timeout, lost response on PR creation'],
          goal: ['Lỗi hiện rõ, lần chạy lỗi tìm được và chạy lại an toàn.', 'Failures are visible, failed runs can be found and safely retried.'],
          learn: [
            ['Cố tình gây lỗi để chứng minh hệ thống hồi phục', 'Cause failures on purpose to prove recovery'],
            ['Không coi timeout là thao tác bên kia đã thất bại', 'Do not treat a timeout as a remote failure'],
          ],
          ai: [['ChatGPT', ['Lập kế hoạch thử lỗi', 'Plan the failure tests'], ['Lập bảng kịch bản thử lỗi cho luồng Jira → PR của tôi gồm: n8n gửi webhook hai lần, agent-service chết khi đang code, timeout khi gọi GitHub tạo PR nhưng PR đã được tạo, Jira lỗi khi cập nhật sau khi PR xong. Mỗi kịch bản ghi cách gây lỗi, kết quả mong đợi và cách kiểm tra.', 'Make a failure-test table for my Jira → PR flow: n8n sends the webhook twice, agent-service dies while coding, the GitHub create-PR call times out but the PR was created, Jira fails to update after the PR is done. For each: how to cause it, expected result, how to check.']]],
          res: [['.NET testing', MS + '/dotnet/core/testing/']],
          pr: [
            ['Viết từng kịch bản thành test hoặc script có thể chạy lại', 'Turn each scenario into a repeatable test or script'],
            ['Sửa chỗ nào tạo tác động phụ trùng hoặc mất dấu lần chạy', 'Fix anything that creates duplicate side effects or loses track of a run'],
          ],
          done: [
            ['Cả 4 kịch bản cho kết quả đúng mong đợi', 'All 4 scenarios behave as expected'],
            ['Không kịch bản nào tạo PR trùng hay lần chạy trùng', 'No scenario creates a duplicate PR or run'],
          ],
        },
        {
          id: 's6-3', kind: 'gate', h: '2',
          t: ['Chốt POC: chứng minh đủ tiêu chí hoàn thành và ghi lại bằng chứng', 'Close the POC: prove every exit criterion and record the evidence'],
          goal: ['Có một trang bằng chứng cho từng tiêu chí POC, không chỉ một bản demo.', 'There is an evidence page for each POC criterion, not just a demo.'],
          learn: [
            ['Tiêu chí hoàn thành là thứ kiểm tra được, không phải cảm giác', 'Exit criteria are checkable, not a feeling'],
            ['Ghi lại cả phần chưa làm được để làm đầu vào cho P2', 'Record what is not done as input for P2'],
          ],
          ai: [['Claude', ['Rà bằng chứng', 'Review the evidence'], ['Đây là trang bằng chứng POC của tôi: <dán nội dung>. Đối chiếu với 9 tiêu chí POC (một ticket chạy trọn, trạng thái và lịch sử được lưu, có correlation ID, đầu ra agent được kiểm tra, bằng chứng build/test được giữ, PR được tạo, người kiểm soát merge, lỗi hiện rõ và retry được, không lộ runner hay credential production). Tiêu chí nào bằng chứng còn yếu?', 'Here is my POC evidence page: <paste>. Check it against the 9 POC criteria (one ticket completes, state and history persist, correlation IDs, validated agent output, retained build/test evidence, PR created, human controls merge, failures visible and retryable, no unrestricted runner or production credentials). Which criteria have weak evidence?']]],
          res: [['Anthropic docs', 'https://docs.anthropic.com/']],
          pr: [
            ['Viết `docs/poc-evidence.md`: mỗi tiêu chí một mục có link log, ảnh, test', 'Write `docs/poc-evidence.md`: one section per criterion with links to logs, screenshots, tests'],
            ['Demo luồng cho một người khác xem', 'Demo the flow to someone else'],
            ['Ghi danh sách điểm yếu chuyển sang P2', 'List the weak points carried into P2'],
          ],
          done: [
            ['Một ticket Jira chạy trọn luồng, trạng thái và lịch sử được lưu, mỗi lần chạy có correlation ID', 'One Jira ticket completes the flow; state and history persist; every run has a correlation ID'],
            ['Đầu ra agent được kiểm tra, bằng chứng build/test được giữ, PR được tạo và người kiểm soát merge', 'Agent output is validated, build/test evidence is kept, the PR is created and a human controls the merge'],
            ['Lỗi hiện rõ và retry an toàn; không lộ runner không giới hạn hay credential production', 'Failures are visible and safely retryable; no unrestricted runner or production credentials are exposed'],
          ],
        },
      ],
    },
  ],
};
