// AI engineering + architecture additions. New ids only, so existing progress is kept.
const MS = 'https://learn.microsoft.com';
const ANT_AGENTS = ['Anthropic: Building effective agents', 'https://www.anthropic.com/engineering/building-effective-agents'];

module.exports = {
  items: [
    {
      phase: 'p0', ms: 's0b', before: 's0b-4', item: {
        id: 's0b-5', kind: 'learn', h: '2–3',
        t: ['Lập bộ khung tài liệu kiến trúc: C4, ADR, thuộc tính chất lượng và sơ đồ tuần tự', 'Set up the architecture documentation kit: C4, ADRs, quality attributes and sequence diagrams'],
        goal: ['Mọi quyết định kiến trúc từ đây về sau đều có chỗ ghi và có mẫu để viết.', 'Every architecture decision from here on has a place and a template.'],
        learn: [
          ['Mô hình C4: Context, Container, Component, Code; vẽ đủ 3 tầng đầu là đủ cho hầu hết hệ thống', 'The C4 model: Context, Container, Component, Code; the first 3 levels cover most systems'],
          ['ADR: bối cảnh, quyết định, phương án đã cân nhắc, hệ quả; ADR không sửa, chỉ thay bằng ADR mới', 'ADRs: context, decision, options considered, consequences; never edit an ADR, supersede it'],
          ['Thuộc tính chất lượng của hệ thống AI: đúng, an toàn, truy vết được, chi phí, độ trễ, độ tin cậy', 'Quality attributes of an AI system: correctness, safety, traceability, cost, latency, reliability'],
          ['Sơ đồ tuần tự cho luồng có nhiều bên và có chờ người duyệt', 'Sequence diagrams for multi-party flows with human waits'],
        ],
        ai: [['Claude', ['Phản biện kiến trúc mục tiêu', 'Critique the target architecture'], ['Đây là sơ đồ C4 tầng Context và Container cho nền tảng AI delivery của tôi: <dán mô tả hoặc Mermaid>. Hãy chỉ ra: container nào đang ôm quá nhiều trách nhiệm, chỗ nào AI đang được giao quyền quyết định mà đáng lẽ code tất định phải giữ, và 5 thuộc tính chất lượng nên xếp ưu tiên thế nào cho giai đoạn POC.', 'Here is the C4 Context and Container view of my AI delivery platform: <paste description or Mermaid>. Point out: which container holds too many responsibilities, where AI is given decisions that deterministic code should keep, and how to rank 5 quality attributes for the POC stage.']]],
        res: [['The C4 model', 'https://c4model.com/'], ['ADR templates', 'https://adr.github.io/']],
        pr: [
          ['Tạo `docs/decisions/0000-template.md` theo mẫu ADR', 'Create `docs/decisions/0000-template.md` from an ADR template'],
          ['Vẽ C4 Context và Container của nền tảng mục tiêu bằng Mermaid trong `docs/architecture/`', 'Draw the C4 Context and Container views of the target platform in Mermaid under `docs/architecture/`'],
          ['Viết bảng thuộc tính chất lượng xếp ưu tiên cho POC, Pilot, Production', 'Write a quality-attribute table ranked for POC, Pilot and Production'],
        ],
        done: [
          ['Sơ đồ C4 hai tầng và mẫu ADR nằm trong repo', 'The two-level C4 diagram and the ADR template are in the repo'],
          ['ADR đầu tiên ghi quyết định "n8n + .NET trước, LangGraph/Temporal khi có lý do"', 'The first ADR records "n8n + .NET first, LangGraph/Temporal when justified"'],
        ],
      },
    },
    {
      phase: 'p1', ms: 's2', item: {
        id: 's2-5', kind: 'build', h: '3–4',
        t: ['Dựng bộ khung đánh giá (eval harness) từ sớm: tập vàng, chấm theo rubric và chạy lại khi đổi prompt', 'Build an early eval harness: a golden set, rubric scoring and reruns whenever a prompt changes'],
        goal: ['Mỗi lần đổi prompt hay model, một lệnh cho ra bảng điểm so với lần trước.', 'Every prompt or model change gets a score table against the previous run with one command.'],
        learn: [
          ['Eval-driven development: viết ca đánh giá trước khi sửa prompt, như viết test trước khi sửa code', 'Eval-driven development: write eval cases before changing a prompt, like tests before code'],
          ['Ba kiểu chấm: so khớp chính xác hoặc theo schema, kiểm tra bằng code, LLM-as-judge có rubric', 'Three scoring styles: exact or schema match, code-based checks, LLM-as-judge with a rubric'],
          ['Sai lệch của LLM chấm: thiên vị câu dài, thiên vị vị trí, tự chấm chính mình', 'LLM-judge biases: length bias, position bias, self-preference'],
          ['Ghi model, prompt version, nhiệt độ và seed để kết quả so sánh được', 'Record model, prompt version, temperature and seed so results are comparable'],
        ],
        ai: [['Claude', ['Viết rubric chấm', 'Write the scoring rubric'], ['Viết rubric chấm đầu ra AI BA (summary, userStory, acceptanceCriteria, openQuestions, assumptions) theo thang 1–5 cho 4 tiêu chí: AC kiểm chứng được, không bịa thông tin, hỏi đúng khi thiếu dữ liệu, đúng phạm vi. Mỗi mức có mô tả cụ thể và một ví dụ. Thêm hướng dẫn cho LLM chấm để giảm thiên vị câu dài.', 'Write a 1–5 rubric for AI BA output (summary, userStory, acceptanceCriteria, openQuestions, assumptions) on 4 criteria: verifiable ACs, no invented facts, asks when data is missing, stays in scope. Each level has a concrete description and an example. Add judge instructions that reduce length bias.']]],
        res: [['Anthropic docs: evaluation', 'https://docs.anthropic.com/'], ['OpenAI evals guide', 'https://platform.openai.com/docs/']],
        pr: [
          ['Tạo `tests/evaluation/harness` đọc ca từ thư mục, chạy agent, chấm, xuất bảng Markdown', 'Create `tests/evaluation/harness` that reads cases from a folder, runs the agent, scores and prints a Markdown table'],
          ['Viết 10 ca vàng đầu tiên cho BA kèm đáp án mong đợi', 'Write the first 10 golden cases for the BA with expected answers'],
          ['Đổi prompt BA sang v2 và so điểm với v1', 'Move the BA prompt to v2 and compare scores with v1'],
        ],
        done: [
          ['Một lệnh chạy toàn bộ ca và in bảng điểm theo tiêu chí', 'One command runs all cases and prints scores per criterion'],
          ['Có so sánh điểm prompt v1 và v2 được lưu lại', 'A v1 vs v2 prompt score comparison is saved'],
        ],
      },
    },
    {
      phase: 'p1', ms: 's4', before: 's4-2', item: {
        id: 's4-6', kind: 'learn', h: '2–3',
        t: ['Học các pattern agentic và chọn pattern cho từng vai trò BA, Developer, Reviewer, QA', 'Learn agentic patterns and choose one for each role: BA, Developer, Reviewer, QA'],
        goal: ['Mỗi agent có pattern được chọn có lý do, và phần nào chỉ cần workflow tất định thì không dùng agent.', 'Each agent has a deliberately chosen pattern, and parts that only need a deterministic workflow use no agent.'],
        learn: [
          ['Workflow khác agent: workflow đi theo đường định sẵn, agent tự chọn bước', 'Workflows vs agents: a workflow follows a set path, an agent picks its own steps'],
          ['Prompt chaining, routing, parallelization, orchestrator-workers, evaluator-optimizer', 'Prompt chaining, routing, parallelization, orchestrator-workers, evaluator-optimizer'],
          ['ReAct, plan-and-execute, reflection: lợi ích và chi phí token', 'ReAct, plan-and-execute, reflection: benefits and token cost'],
          ['Chọn pattern đơn giản nhất đạt chất lượng; độ phức tạp phải được đánh giá chứng minh', 'Pick the simplest pattern that reaches the quality bar; complexity must be justified by evals'],
        ],
        ai: [['Claude', ['Thiết kế pattern cho từng agent', 'Design patterns per agent'], ['Với 4 vai trò trong nền tảng của tôi (BA phân tích yêu cầu, Developer lập kế hoạch rồi sửa code, Reviewer tìm lỗi, QA sinh kịch bản test), hãy đề xuất pattern phù hợp nhất cho từng vai trò trong số: prompt chaining, routing, parallelization, orchestrator-workers, evaluator-optimizer, ReAct, plan-and-execute. Giải thích lý do, rủi ro, chi phí, và phần nào nên là workflow tất định thay vì agent.', 'For the 4 roles in my platform (BA analyses requirements, Developer plans then edits code, Reviewer finds defects, QA writes test scenarios), propose the best pattern for each from: prompt chaining, routing, parallelization, orchestrator-workers, evaluator-optimizer, ReAct, plan-and-execute. Explain the reason, risks, cost, and which parts should be a deterministic workflow instead of an agent.']]],
        res: [ANT_AGENTS, ['ReAct paper', 'https://arxiv.org/abs/2210.03629']],
        pr: [
          ['Đọc lại "Building effective agents", ghi tóm tắt 5 pattern workflow', 'Reread "Building effective agents" and summarise the 5 workflow patterns'],
          ['Vẽ sơ đồ pattern cho từng vai trò', 'Draw the pattern for each role'],
          ['Ghi ADR "Agent patterns per role"', 'Write the ADR "Agent patterns per role"'],
        ],
        done: [
          ['Có ADR chọn pattern cho 4 vai trò kèm lý do và rủi ro', 'There is an ADR choosing a pattern for the 4 roles with reasons and risks'],
        ],
      },
    },
    {
      phase: 'p1', ms: 's4', before: 's4-2', item: {
        id: 's4-4', kind: 'eval', h: '3–4',
        t: ['So sánh SDK agent của .NET với vòng lặp tự viết trên cùng một agent nhỏ, rồi ghi ADR "Agent implementation"', 'Compare a .NET agent SDK with a hand-written loop on the same small agent, then write the "Agent implementation" ADR'],
        goal: ['Chọn cách cài đặt agent dựa trên mức kiểm soát, quan sát và chi phí đo được, không dựa trên độ phổ biến.', 'Choose how to implement agents from measured control, observability and cost, not popularity.'],
        learn: [
          ['`Microsoft.Extensions.AI`: `IChatClient`, function invocation tự động, middleware', '`Microsoft.Extensions.AI`: `IChatClient`, automatic function invocation, middleware'],
          ['Semantic Kernel / Microsoft Agent Framework: plugin, planner, agent', 'Semantic Kernel / Microsoft Agent Framework: plugins, planners, agents'],
          ['Tiêu chí so sánh: giới hạn bước, chèn kiểm tra quyền trước mỗi công cụ, audit, retry, khả năng test', 'Comparison criteria: step limits, permission checks before each tool, audit, retry, testability'],
          ['Khi nào cần LangGraph: trạng thái dạng đồ thị, chu trình có kiểm soát, checkpoint', 'When LangGraph is needed: graph state, controlled cycles, checkpoints'],
        ],
        ai: [['Claude Code', ['Làm hai bản thử', 'Build two prototypes'], ['Làm cùng một agent nhỏ "tóm tắt ticket Jira và gợi ý nhãn" theo hai cách trong `prototypes/agent-impl/`: (A) dùng `Microsoft.Extensions.AI` với function invocation tự động, (B) vòng lặp tự viết gọi `ILlmClient` và `ToolRegistry` của tôi. Cả hai có giới hạn 5 bước, kiểm tra quyền trước khi gọi công cụ, ghi audit mỗi lời gọi. Viết bảng so sánh số dòng code, chỗ chèn kiểm tra được, và độ khó test.', 'Build the same small agent "summarise a Jira ticket and suggest labels" two ways in `prototypes/agent-impl/`: (A) `Microsoft.Extensions.AI` with automatic function invocation, (B) a hand-written loop calling my `ILlmClient` and `ToolRegistry`. Both have a 5-step limit, permission checks before tool calls and audit per call. Write a comparison table of lines of code, where checks can be inserted, and how hard each is to test.']]],
        res: [['.NET AI', MS + '/dotnet/ai/'], ['Semantic Kernel', MS + '/semantic-kernel/overview/'], ['LangGraph docs', 'https://docs.langchain.com/oss/python/langgraph/overview']],
        pr: [
          ['Làm hai bản thử như prompt', 'Build the two prototypes as in the prompt'],
          ['Chạy cả hai trên 5 ticket, ghi token, thời gian, số lỗi', 'Run both on 5 tickets and record tokens, time and errors'],
          ['Viết ADR "Agent implementation" theo 7 câu hỏi checkpoint', 'Write the "Agent implementation" ADR using the 7 checkpoint questions'],
        ],
        done: [
          ['Có ADR với bảng so sánh và quyết định rõ cho nền tảng', 'There is an ADR with a comparison table and a clear decision for the platform'],
        ],
      },
    },
    {
      phase: 'p1', ms: 's4', before: 's4-2', item: {
        id: 's4-5', kind: 'core', h: '4–5',
        t: ['Tự viết AgentRuntime dùng chung: vòng lặp, ngân sách bước/thời gian/token, công cụ theo vai trò, trạng thái và audit', 'Write a shared AgentRuntime yourself: the loop, step/time/token budgets, role-based tools, state and audit'],
        goal: ['BA, Developer, Reviewer, QA chạy trên cùng một runtime; mỗi agent chỉ khác prompt, công cụ và schema đầu ra.', 'BA, Developer, Reviewer and QA run on one runtime; each agent only differs in prompt, tools and output schema.'],
        learn: [
          ['Tách định nghĩa agent (prompt, công cụ, schema, ngân sách) khỏi động cơ chạy', 'Separate the agent definition (prompt, tools, schema, budgets) from the engine'],
          ['Ngân sách là điều kiện dừng cứng: số bước, thời gian, token, chi phí', 'Budgets are hard stop conditions: steps, time, tokens, cost'],
          ['Trạng thái agent lưu được để dừng và tiếp tục (chờ người duyệt)', 'Persistable agent state to pause and resume (waiting for approval)'],
          ['Mỗi bước sinh một sự kiện có cấu trúc: dùng cho audit, trace và đánh giá', 'Every step emits a structured event: used for audit, tracing and evaluation'],
        ],
        ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết `AgentRuntime` với `AgentDefinition` (prompt, công cụ, schema đầu ra, ngân sách) và vòng lặp có lưu trạng thái: <dán code>. Đừng viết lại. Tìm: cách một agent vượt ngân sách, chỗ trạng thái lưu không đủ để tiếp tục sau khi duyệt, công cụ ngoài vai trò có thể lọt, và sự kiện nào còn thiếu để truy vết được một lần chạy.', 'I wrote `AgentRuntime` with an `AgentDefinition` (prompt, tools, output schema, budgets) and a loop with persisted state: <paste code>. Do not rewrite it. Find: ways an agent can exceed its budget, where the saved state is not enough to resume after approval, tools outside the role that could slip through, and events missing for tracing a run.']]],
        res: [ANT_AGENTS],
        pr: [
          ['Định nghĩa `AgentDefinition` và `AgentRun` (trạng thái, bước, ngân sách đã dùng)', 'Define `AgentDefinition` and `AgentRun` (state, steps, budget used)'],
          ['Viết vòng lặp dùng `ILlmClient` và `ToolRegistry`, phát sự kiện mỗi bước', 'Write the loop on `ILlmClient` and `ToolRegistry`, emitting an event per step'],
          ['Cho phép tạm dừng ở bước cần duyệt và tiếp tục từ trạng thái đã lưu', 'Allow pausing at an approval step and resuming from saved state'],
        ],
        done: [
          ['Test: vượt ngân sách bước hoặc chi phí thì dừng với lý do rõ', 'Test: exceeding the step or cost budget stops with a clear reason'],
          ['Test: tạm dừng chờ duyệt, restart service, rồi tiếp tục đúng chỗ', 'Test: pause for approval, restart the service, then resume at the right point'],
        ],
      },
    },
  ],

  milestones: [
    {
      phase: 'p1', after: 's3', ms: {
        id: 'ctx', title: ['Bổ sung · Context engineering và retrieval (RAG)', 'Extra · Context engineering and retrieval (RAG)'],
        items: [
          {
            id: 'ctx-1', kind: 'learn', h: '3',
            t: ['Học embedding, chunking, tìm kiếm lai, rerank và cách đưa ngữ cảnh vào prompt', 'Learn embeddings, chunking, hybrid search, reranking and how context goes into a prompt'],
            goal: ['Giải thích được khi nào RAG giúp, khi nào chỉ cần tìm theo từ khóa hay theo cấu trúc code.', 'You can explain when RAG helps and when keyword or code-structure search is enough.'],
            learn: [
              ['Embedding và độ tương đồng cosine; giới hạn của tìm kiếm ngữ nghĩa với tên hàm, mã lỗi', 'Embeddings and cosine similarity; limits of semantic search for function names and error codes'],
              ['Chunking theo cấu trúc (theo hàm, theo mục tài liệu) tốt hơn theo số ký tự', 'Structure-aware chunking (per function, per doc section) beats fixed-size chunks'],
              ['Tìm kiếm lai: BM25 + vector, rồi rerank', 'Hybrid search: BM25 + vectors, then reranking'],
              ['"Lost in the middle", ngân sách token, prompt caching cho phần ngữ cảnh lặp lại', '"Lost in the middle", token budgets, prompt caching for repeated context'],
            ],
            ai: [['Claude', ['Học bằng ví dụ của chính hệ thống', 'Learn with your own system'], ['Nền tảng của tôi cần đưa ngữ cảnh cho 2 agent: BA cần tài liệu yêu cầu và quyết định cũ, Developer cần code liên quan trong repo .NET + Angular. Với từng agent, hãy so sánh: tìm theo từ khóa, đồ thị symbol của code, tìm kiếm vector, tìm kiếm lai có rerank. Cách nào nên thử trước, đo bằng gì, và khi nào mới đáng thêm vector database.', 'My platform has to give context to 2 agents: the BA needs requirement docs and past decisions, the Developer needs relevant code in a .NET + Angular repo. For each agent, compare: keyword search, a code symbol graph, vector search, hybrid search with reranking. Which to try first, how to measure it, and when a vector database becomes worth adding.']]],
            res: [['Anthropic: Contextual Retrieval', 'https://www.anthropic.com/news/contextual-retrieval'], ['pgvector', 'https://github.com/pgvector/pgvector']],
            pr: [
              ['Đọc bài Contextual Retrieval và tài liệu pgvector', 'Read the Contextual Retrieval article and the pgvector docs'],
              ['Viết ghi chú: nguồn ngữ cảnh của từng agent và cách lấy dự kiến', 'Write a note: each agent\'s context sources and the planned retrieval method'],
            ],
            done: [
              ['Ghi chú nguồn ngữ cảnh nằm trong `docs/architecture/context.md`', 'The context-source note is in `docs/architecture/context.md`'],
            ],
          },
          {
            id: 'ctx-2', kind: 'core', h: '4–5',
            t: ['Tự viết ContextBuilder: thu nguồn, xếp ưu tiên, cắt theo ngân sách token và đánh dấu dữ liệu không tin cậy', 'Write a ContextBuilder yourself: gather sources, prioritise, trim to a token budget and mark untrusted data'],
            goal: ['Mọi prompt của agent được dựng qua một chỗ, không vượt ngân sách, và nội dung từ Jira/repo luôn được bọc như dữ liệu.', 'Every agent prompt is assembled in one place, never exceeds its budget, and Jira/repo content is always wrapped as data.'],
            learn: [
              ['Phần cố định (chỉ dẫn, schema) đặt đầu để tận dụng prompt caching', 'Put the fixed part (instructions, schemas) first to benefit from prompt caching'],
              ['Xếp ưu tiên: yêu cầu đã duyệt > code liên quan trực tiếp > tài liệu > lịch sử', 'Priority: approved requirement > directly related code > docs > history'],
              ['Bọc nội dung không tin cậy bằng thẻ riêng và nói rõ đó là dữ liệu, không phải chỉ dẫn', 'Wrap untrusted content in its own tags and state it is data, not instructions'],
            ],
            ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết `ContextBuilder` dựng prompt theo ngân sách token: <dán code>. Đừng viết lại. Kiểm tra: phần cắt có thể làm mất thông tin quan trọng không, thứ tự có tối ưu cho prompt caching không, nội dung không tin cậy có lúc nào bị đặt lẫn vào phần chỉ dẫn không.', 'I wrote a `ContextBuilder` that assembles prompts within a token budget: <paste code>. Do not rewrite it. Check: can trimming drop important information, is the order optimal for prompt caching, and can untrusted content ever end up inside the instructions part.']]],
            res: [['Anthropic docs: prompt caching', 'https://docs.anthropic.com/']],
            pr: [
              ['Định nghĩa `ContextSource` (loại, độ ưu tiên, độ tin cậy, nội dung)', 'Define `ContextSource` (kind, priority, trust level, content)'],
              ['Viết builder đếm token, xếp ưu tiên, cắt và bọc dữ liệu', 'Write the builder that counts tokens, prioritises, trims and wraps data'],
              ['Ghi lại nguồn nào được đưa vào mỗi prompt để truy vết', 'Record which sources went into each prompt for traceability'],
            ],
            done: [
              ['Test: ngữ cảnh lớn hơn ngân sách bị cắt theo đúng thứ tự ưu tiên', 'Test: context larger than the budget is trimmed in priority order'],
              ['Mỗi lần gọi LLM lưu danh sách nguồn ngữ cảnh đã dùng', 'Each LLM call stores the list of context sources used'],
            ],
          },
          {
            id: 'ctx-3', kind: 'build', h: '4–5',
            t: ['Làm retrieval cho code và tài liệu: chỉ mục từ khóa và symbol trước, rồi thêm tìm kiếm vector bằng pgvector', 'Build retrieval for code and docs: keyword and symbol index first, then vector search with pgvector'],
            goal: ['Có hai cách tìm chạy song song để so sánh trên cùng bộ câu hỏi.', 'Two retrieval methods run side by side to compare on the same question set.'],
            learn: [
              ['Chỉ mục symbol của code (Roslyn cho C#, TypeScript compiler cho Angular)', 'A code symbol index (Roslyn for C#, the TypeScript compiler for Angular)'],
              ['pgvector trong cùng PostgreSQL thay vì thêm một database riêng', 'pgvector inside the same PostgreSQL instead of a separate database'],
              ['Cập nhật chỉ mục khi code đổi (theo commit)', 'Refresh the index when code changes (per commit)'],
            ],
            ai: [['Claude Code', ['Dựng hai cách tìm', 'Build two retrievers'], ['Trong `services/agent-service`, thêm `ICodeRetriever` với hai bản: (A) full-text search của PostgreSQL cộng chỉ mục tên class, method lấy bằng Roslyn; (B) embedding theo từng method và từng mục tài liệu lưu trong pgvector. Thêm chế độ lai gộp điểm hai bên. Chỉ mục cập nhật theo commit SHA.', 'In `services/agent-service`, add an `ICodeRetriever` with two implementations: (A) PostgreSQL full-text search plus a class/method name index built with Roslyn; (B) per-method and per-doc-section embeddings stored in pgvector. Add a hybrid mode merging both scores. The index refreshes per commit SHA.']]],
            res: [['pgvector', 'https://github.com/pgvector/pgvector'], ['Roslyn APIs', MS + '/dotnet/csharp/roslyn-sdk/']],
            pr: [
              ['Làm bản từ khóa + symbol', 'Build the keyword + symbol version'],
              ['Làm bản vector với pgvector', 'Build the pgvector version'],
              ['Nối vào ContextBuilder cho Developer agent', 'Wire them into the ContextBuilder for the Developer agent'],
            ],
            done: [
              ['Cả hai cách trả về danh sách file/method cho cùng một câu hỏi', 'Both methods return a file/method list for the same question'],
            ],
          },
          {
            id: 'ctx-4', kind: 'eval', h: '3',
            t: ['Đo chất lượng retrieval (recall@k, MRR) và ghi ADR có cần vector database hay không', 'Measure retrieval quality (recall@k, MRR) and write the ADR on whether a vector database is needed'],
            goal: ['Quyết định dùng vector hay không dựa trên số đo, đúng tinh thần checkpoint kiến trúc của roadmap.', 'Decide on vectors from measurements, in the spirit of the roadmap\'s architecture checkpoints.'],
            learn: [
              ['recall@k: trong k kết quả đầu có file đúng không; MRR: file đúng đứng thứ mấy', 'recall@k: is the right file in the top k; MRR: at what rank'],
              ['Đo cả ảnh hưởng tới kết quả cuối (Developer làm đúng hơn không), không chỉ đo retrieval', 'Measure the end effect (does the Developer do better), not only retrieval'],
            ],
            ai: [['ChatGPT', ['Soạn bộ câu hỏi', 'Draft the question set'], ['Từ repo TaskLite của tôi (cấu trúc: <dán cây thư mục>), soạn 20 câu hỏi kiểu một Developer agent sẽ hỏi khi làm feature, mỗi câu kèm danh sách file hoặc method đúng. Trộn: tìm theo tên chính xác, theo khái niệm, theo hành vi, theo mã lỗi.', 'From my TaskLite repo (layout: <paste tree>), write 20 questions a Developer agent would ask while building a feature, each with the correct files or methods. Mix: exact-name lookups, concept lookups, behaviour lookups, error-code lookups.']]],
            res: [['Anthropic: Contextual Retrieval', 'https://www.anthropic.com/news/contextual-retrieval']],
            pr: [
              ['Chạy 3 chế độ (từ khóa, vector, lai) trên 20 câu hỏi và tính recall@5, MRR', 'Run the 3 modes (keyword, vector, hybrid) on 20 questions and compute recall@5 and MRR'],
              ['Chạy Developer agent trên 3 feature với chế độ tốt nhất và tệ nhất, so kết quả', 'Run the Developer agent on 3 features with the best and worst modes and compare'],
              ['Viết ADR "Retrieval strategy"', 'Write the "Retrieval strategy" ADR'],
            ],
            done: [
              ['Có bảng recall@5 và MRR cho 3 chế độ', 'There is a recall@5 and MRR table for the 3 modes'],
              ['ADR ghi rõ có dùng vector hay không và vì sao', 'The ADR states whether vectors are used and why'],
            ],
          },
        ],
      },
    },
    {
      phase: 'p1', after: 's5', ms: {
        id: 'mag', title: ['Bổ sung · Kiến trúc multi-agent và hợp đồng giữa agent', 'Extra · Multi-agent architecture and contracts between agents'],
        items: [
          {
            id: 'mag-1', kind: 'learn', h: '2–3',
            t: ['Học kiến trúc multi-agent: handoff, supervisor, orchestrator-workers và các kiểu hỏng thường gặp', 'Learn multi-agent architecture: handoffs, supervisors, orchestrator-workers and common failure modes'],
            goal: ['Giải thích được vì sao hệ thống của bạn dùng handoff qua artifact có schema thay vì để các agent nói chuyện tự do.', 'You can explain why your system hands off through schema-validated artifacts instead of letting agents chat freely.'],
            learn: [
              ['Handoff qua artifact (yêu cầu đã duyệt, kế hoạch, diff, phát hiện) so với hội thoại tự do giữa agent', 'Handoffs through artifacts (approved requirement, plan, diff, findings) vs free chat between agents'],
              ['Kiểu hỏng: lặp vô tận giữa hai agent, mất ngữ cảnh khi chuyển, đồng thuận sai, chi phí phình to', 'Failure modes: endless loops between two agents, context lost in handoff, false consensus, cost blow-up'],
              ['Ranh giới vai trò: mỗi agent một quyền, một schema đầu ra', 'Role boundaries: one permission set and one output schema per agent'],
            ],
            ai: [['Claude', ['Phân tích kiểu hỏng', 'Analyse failure modes'], ['Hệ thống của tôi có BA → Developer → Reviewer → (vòng sửa) → QA, điều phối bởi Workflow API, chuyển giao bằng artifact có schema. Liệt kê 6 kiểu hỏng có thể xảy ra khi các agent phối hợp, với mỗi kiểu: dấu hiệu nhận biết trong log, cách chặn bằng thiết kế, và ca đánh giá để phát hiện.', 'My system runs BA → Developer → Reviewer → (fix loop) → QA, orchestrated by the Workflow API, handing off schema-validated artifacts. List 6 failure modes in agent coordination; for each: how it shows in logs, how to prevent it by design, and an eval case to detect it.']]],
            res: [ANT_AGENTS, ['Anthropic: multi-agent research system', 'https://www.anthropic.com/engineering/multi-agent-research-system']],
            pr: [
              ['Vẽ sơ đồ tuần tự BA → Developer → Reviewer → QA với các artifact chuyển giao', 'Draw the BA → Developer → Reviewer → QA sequence diagram with the handed-off artifacts'],
              ['Liệt kê kiểu hỏng và cách chặn vào `docs/architecture/multi-agent.md`', 'List failure modes and their prevention in `docs/architecture/multi-agent.md`'],
            ],
            done: [
              ['Sơ đồ tuần tự và bảng kiểu hỏng nằm trong repo', 'The sequence diagram and failure-mode table are in the repo'],
            ],
          },
          {
            id: 'mag-2', kind: 'build', h: '3–4',
            t: ['Xây kho artifact có version và hợp đồng sự kiện giữa các agent trong `contracts/`', 'Build a versioned artifact store and event contracts between agents in `contracts/`'],
            goal: ['Mọi lần chuyển giao là một artifact bất biến, có schema version, truy được nguồn gốc tới lần chạy và prompt sinh ra nó.', 'Every handoff is an immutable artifact with a schema version, traceable to the run and prompt that produced it.'],
            learn: [
              ['Version hóa schema và tương thích ngược giữa phiên bản agent', 'Schema versioning and backward compatibility across agent versions'],
              ['Nguồn gốc (provenance): agent nào, model nào, prompt nào, đầu vào nào', 'Provenance: which agent, model, prompt and inputs'],
              ['Sự kiện miền: RequirementApproved, PlanProduced, ReviewCompleted…', 'Domain events: RequirementApproved, PlanProduced, ReviewCompleted…'],
            ],
            ai: [['Claude Code', ['Dựng kho artifact', 'Build the artifact store'], ['Thêm bảng `Artifact` (id, executionId, kind, schemaVersion, contentHash, content, producedBy, model, promptVersion, inputArtifactIds, createdAt), chỉ cho thêm, không cho sửa. Thêm `contracts/events/*.schema.json` cho RequirementApproved, PlanProduced, CodeChanged, ReviewCompleted, TestsGenerated. Workflow API phát sự kiện qua outbox khi artifact mới được lưu.', 'Add an append-only `Artifact` table (id, executionId, kind, schemaVersion, contentHash, content, producedBy, model, promptVersion, inputArtifactIds, createdAt). Add `contracts/events/*.schema.json` for RequirementApproved, PlanProduced, CodeChanged, ReviewCompleted, TestsGenerated. The Workflow API publishes events through the outbox when a new artifact is stored.']]],
            res: [['JSON Schema', 'https://json-schema.org/'], ['CloudEvents', 'https://cloudevents.io/']],
            pr: [
              ['Tạo bảng Artifact chỉ-thêm và các schema sự kiện', 'Create the append-only Artifact table and the event schemas'],
              ['Chuyển BA, Developer, Reviewer sang đọc và ghi qua kho artifact', 'Move the BA, Developer and Reviewer to read and write through the artifact store'],
            ],
            done: [
              ['Từ một PR truy ngược được toàn bộ chuỗi artifact tới ticket ban đầu', 'From a PR, the whole artifact chain traces back to the original ticket'],
            ],
          },
          {
            id: 'mag-3', kind: 'eval', h: '3',
            t: ['So sánh một agent làm hết với chuỗi nhiều agent chuyên vai trò về chất lượng, chi phí và thời gian', 'Compare one do-everything agent with a chain of role-specific agents on quality, cost and time'],
            goal: ['Có số liệu cho thấy chia vai trò có đáng chi phí tăng thêm không.', 'You have numbers showing whether splitting roles is worth the extra cost.'],
            learn: [
              ['Nhiều agent không mặc nhiên tốt hơn; đo bằng bộ đánh giá', 'More agents are not automatically better; measure with the eval set'],
            ],
            ai: [['ChatGPT', ['Thiết kế thí nghiệm', 'Design the experiment'], ['Thiết kế thí nghiệm so sánh (A) một agent nhận ticket và tự làm từ phân tích tới code và tự review, với (B) chuỗi BA → Developer → Reviewer của tôi, trên 5 ticket. Số đo gồm: tỷ lệ AC đạt, số lỗi reviewer người tìm thêm, token, chi phí, thời gian. Nêu cách tránh so sánh thiên vị.', 'Design an experiment comparing (A) one agent that takes a ticket and does analysis, code and self-review, with (B) my BA → Developer → Reviewer chain, on 5 tickets. Measures: AC pass rate, extra defects a human reviewer finds, tokens, cost, time. Explain how to avoid a biased comparison.']]],
            res: [['Anthropic: multi-agent research system', 'https://www.anthropic.com/engineering/multi-agent-research-system']],
            pr: [
              ['Chạy cả hai cách trên 5 ticket bằng eval harness', 'Run both approaches on 5 tickets with the eval harness'],
              ['Ghi bảng kết quả và kết luận vào ADR', 'Record the results table and conclusion in an ADR'],
            ],
            done: [
              ['Có bảng so sánh chất lượng, chi phí, thời gian và kết luận', 'There is a quality, cost and time comparison with a conclusion'],
            ],
          },
        ],
      },
    },
    {
      phase: 'p2', after: 's8', ms: {
        id: 'gw', title: ['Bổ sung · LLM gateway, định tuyến model, guardrails và chi phí', 'Extra · LLM gateway, model routing, guardrails and cost'],
        items: [
          {
            id: 'gw-1', kind: 'learn', h: '2–3',
            t: ['Học kiến trúc LLM gateway: định tuyến model, cache, ngân sách, fallback và guardrails vào/ra', 'Learn LLM gateway architecture: model routing, caching, budgets, fallbacks and input/output guardrails'],
            goal: ['Vẽ được một gateway đứng giữa mọi agent và mọi nhà cung cấp, và biết nó thực thi những chính sách nào.', 'You can draw a gateway between every agent and every provider and know which policies it enforces.'],
            learn: [
              ['Định tuyến theo loại việc: model nhỏ cho phân loại và trích xuất, model mạnh cho suy luận và code', 'Route by task type: small models for classification and extraction, strong models for reasoning and code'],
              ['Prompt caching của nhà cung cấp và cache theo nội dung của bạn', 'Provider prompt caching and your own content-based cache'],
              ['Ngân sách theo lần chạy, theo nhóm, theo ngày; dừng cứng khi hết', 'Budgets per run, per team, per day; hard stop when exhausted'],
              ['Guardrails vào (injection, PII) và ra (schema, bí mật bị lộ, nội dung bị cấm)', 'Input guardrails (injection, PII) and output guardrails (schema, leaked secrets, forbidden content)'],
            ],
            ai: [['Claude', ['Thiết kế gateway', 'Design the gateway'], ['Thiết kế một LLM gateway nội bộ bằng .NET cho nền tảng của tôi: các agent chỉ gọi gateway, gateway gọi nhà cung cấp. Liệt kê chức năng (định tuyến, retry, fallback, cache, ngân sách, guardrails, ghi chi phí, trace), thứ tự xử lý của một request, chỗ nào chặn cứng, chỗ nào chỉ cảnh báo, và rủi ro biến gateway thành điểm hỏng duy nhất.', 'Design an internal .NET LLM gateway for my platform: agents only call the gateway, the gateway calls providers. List its functions (routing, retry, fallback, caching, budgets, guardrails, cost logging, tracing), the processing order of a request, where it blocks hard vs only warns, and the risk of the gateway becoming a single point of failure.']]],
            res: [['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/'], ['.NET AI', MS + '/dotnet/ai/']],
            pr: [
              ['Vẽ sơ đồ thành phần và thứ tự xử lý request của gateway', 'Draw the gateway\'s components and request pipeline'],
              ['Viết ADR: gateway tự viết hay dùng sản phẩm có sẵn', 'Write an ADR: build the gateway or use an existing product'],
            ],
            done: [
              ['Có sơ đồ gateway và ADR', 'There is a gateway diagram and an ADR'],
            ],
          },
          {
            id: 'gw-2', kind: 'core', h: '4–5',
            t: ['Tự viết bộ định tuyến model theo loại việc với ngân sách theo lần chạy và theo nhóm', 'Write a task-based model router yourself, with per-run and per-team budgets'],
            goal: ['Mỗi lời gọi được gửi tới model rẻ nhất đạt chất lượng, và không lần chạy nào vượt ngân sách.', 'Each call goes to the cheapest model that meets the quality bar, and no run exceeds its budget.'],
            learn: [
              ['Bảng định tuyến là cấu hình, gắn với kết quả đánh giá chất lượng', 'The routing table is configuration, tied to quality evaluation results'],
              ['Ngân sách kiểm tra trước khi gọi, ghi sau khi gọi, không đếm trùng khi retry', 'Check budgets before the call, record after, never double count on retry'],
            ],
            ai: [['Claude', ['Review sau khi tự viết', 'Review after you write it'], ['Tôi tự viết `ModelRouter` và `BudgetGuard`: <dán code>. Đừng viết lại. Tìm: cách một lần chạy vượt ngân sách khi gọi song song, chỗ retry bị tính tiền hai lần, và trường hợp định tuyến sang model rẻ làm hỏng việc mà không bị phát hiện.', 'I wrote a `ModelRouter` and `BudgetGuard` myself: <paste code>. Do not rewrite them. Find: how a run could exceed its budget with parallel calls, where a retry is charged twice, and cases where routing to a cheap model breaks the task without being noticed.']]],
            res: [['Anthropic docs: models overview', 'https://docs.anthropic.com/']],
            pr: [
              ['Phân loại các lời gọi hiện có: phân loại, trích xuất, phân tích, code, review', 'Classify existing calls: classification, extraction, analysis, code, review'],
              ['Viết router đọc bảng định tuyến và BudgetGuard theo lần chạy và nhóm', 'Write the router reading the routing table, and a per-run and per-team BudgetGuard'],
              ['Đưa mọi agent đi qua router', 'Route every agent through it'],
            ],
            done: [
              ['Test: lần chạy chạm ngân sách bị dừng với lý do rõ', 'Test: a run that hits its budget stops with a clear reason'],
              ['Mỗi lời gọi ghi model được chọn và lý do định tuyến', 'Each call records the chosen model and routing reason'],
            ],
          },
          {
            id: 'gw-3', kind: 'build', h: '3–4',
            t: ['Thêm pipeline guardrails: phát hiện injection, che PII và bí mật ở đầu vào, kiểm tra schema và rò rỉ ở đầu ra', 'Add a guardrails pipeline: injection detection, PII and secret redaction on input, schema and leak checks on output'],
            goal: ['Dữ liệu nhạy cảm không đi tới nhà cung cấp, và đầu ra chứa bí mật hay vượt chính sách bị chặn trước khi dùng.', 'Sensitive data never reaches the provider, and output containing secrets or breaking policy is blocked before use.'],
            learn: [
              ['Guardrail là lớp phòng thủ, không thay cho phân quyền công cụ', 'Guardrails are a defense layer, not a replacement for tool permissions'],
              ['Bộ phân loại nhỏ hoặc quy tắc regex cho bí mật và PII', 'Small classifiers or regex rules for secrets and PII'],
            ],
            ai: [['Claude Code', ['Cài guardrails', 'Implement guardrails'], ['Trong LLM gateway, thêm pipeline guardrails: đầu vào che email, số điện thoại, token dạng `ghp_`/`github_pat_`/khóa API và gắn cờ nội dung giống prompt injection bằng một model phân loại nhỏ; đầu ra kiểm tra schema, quét bí mật, và chặn nếu đầu ra yêu cầu gọi công cụ không có trong vai trò. Mỗi lần chặn ghi lý do và gửi sự kiện.', 'In the LLM gateway, add a guardrails pipeline: on input redact emails, phone numbers, `ghp_`/`github_pat_`/API-key-shaped tokens and flag prompt-injection-like content with a small classifier model; on output validate the schema, scan for secrets, and block if it asks for a tool outside the role. Each block records a reason and emits an event.']]],
            res: [['OWASP Top 10 for LLM Applications', 'https://genai.owasp.org/']],
            pr: [
              ['Cài guardrails vào và ra trong gateway', 'Implement input and output guardrails in the gateway'],
              ['Chạy lại các ca injection của `s2-4` và `s3-4` qua gateway', 'Rerun the `s2-4` and `s3-4` injection cases through the gateway'],
            ],
            done: [
              ['Không có token hay email nào trong log gửi tới nhà cung cấp', 'No token or email appears in the payloads sent to the provider'],
              ['Đầu ra chứa bí mật bị chặn và có sự kiện', 'Output containing a secret is blocked and emits an event'],
            ],
          },
          {
            id: 'gw-4', kind: 'eval', h: '3',
            t: ['Thí nghiệm đánh đổi chi phí và chất lượng trên việc thật, cập nhật bảng định tuyến', 'Run a cost vs quality trade-off experiment on real tasks and update the routing table'],
            goal: ['Bảng định tuyến dựa trên số đo: chi phí mỗi việc giảm mà điểm đánh giá không giảm quá ngưỡng.', 'The routing table rests on measurements: cost per task drops without the eval score dropping below threshold.'],
            learn: [
              ['Đường cong Pareto chi phí – chất lượng', 'The cost–quality Pareto curve'],
            ],
            ai: [['ChatGPT', ['Đọc kết quả', 'Read the results'], ['Đây là kết quả chạy eval harness với 3 model cho từng loại việc (điểm, token, chi phí, độ trễ): <dán bảng>. Đề xuất bảng định tuyến mới, ngưỡng chất lượng tối thiểu cho từng loại việc, và loại việc nào nên giữ model mạnh dù đắt.', 'Here are eval-harness results for 3 models per task type (score, tokens, cost, latency): <paste table>. Propose a new routing table, a minimum quality threshold per task type, and which task types should keep the strong model despite the cost.']]],
            res: [['OpenAI evals guide', 'https://platform.openai.com/docs/']],
            pr: [
              ['Chạy eval harness cho từng loại việc với 2–3 model', 'Run the eval harness per task type with 2–3 models'],
              ['Cập nhật bảng định tuyến và ghi ADR', 'Update the routing table and write an ADR'],
            ],
            done: [
              ['Chi phí mỗi việc giảm và có bảng chứng minh điểm không giảm quá ngưỡng', 'Cost per task drops, with a table showing scores stay above threshold'],
            ],
          },
        ],
      },
    },
    {
      phase: 'p3', after: 's16', ms: {
        id: 'arch', title: ['Bổ sung · Thiết kế hệ thống AI: tài liệu kiến trúc và bài tập mở rộng quy mô', 'Extra · AI system design: architecture document and scaling kata'],
        items: [
          {
            id: 'arch-1', kind: 'build', h: '4–5',
            t: ['Viết tài liệu kiến trúc hoàn chỉnh: C4 ba tầng, luồng dữ liệu, ranh giới tin cậy, thuộc tính chất lượng và nhật ký ADR', 'Write the complete architecture document: three-level C4, data flows, trust boundaries, quality attributes and the ADR log'],
            goal: ['Một kiến trúc sư khác đọc tài liệu là hiểu hệ thống, các đánh đổi và lý do của từng quyết định.', 'Another architect can read it and understand the system, its trade-offs and the reason for each decision.'],
            learn: [
              ['Tài liệu kiến trúc theo arc42 hoặc khung tương tự', 'Architecture docs following arc42 or a similar template'],
              ['Đánh đổi phải ghi cả phương án bị loại và vì sao', 'Trade-offs must record rejected options and why'],
            ],
            ai: [['Claude', ['Đóng vai hội đồng review', 'Act as a review board'], ['Đóng vai hội đồng review kiến trúc. Đọc tài liệu kiến trúc nền tảng AI delivery của tôi: <dán>. Hỏi tôi 10 câu khó nhất về: ranh giới giữa code tất định và agent, điểm hỏng duy nhất, bảo mật runner và gateway, chi phí khi tăng 10 lần lưu lượng, và cách hệ thống biết chất lượng AI đang giảm.', 'Act as an architecture review board. Read my AI delivery platform architecture document: <paste>. Ask me the 10 hardest questions about: the boundary between deterministic code and agents, single points of failure, runner and gateway security, cost at 10x traffic, and how the system knows AI quality is dropping.']]],
            res: [['arc42', 'https://arc42.org/'], ['The C4 model', 'https://c4model.com/']],
            pr: [
              ['Viết `docs/architecture/README.md` theo arc42 rút gọn', 'Write `docs/architecture/README.md` following a trimmed arc42'],
              ['Cập nhật C4 Context, Container, Component cho agent service và Workflow API', 'Update C4 Context, Container and Component views for the agent service and Workflow API'],
              ['Liệt kê toàn bộ ADR với trạng thái hiện tại', 'List every ADR with its current status'],
            ],
            done: [
              ['Tài liệu kiến trúc có C4 ba tầng, ranh giới tin cậy và nhật ký ADR', 'The architecture doc has three-level C4, trust boundaries and the ADR log'],
              ['Trả lời được 10 câu hỏi của hội đồng review bằng tài liệu', 'The doc answers the review board\'s 10 questions'],
            ],
          },
          {
            id: 'arch-2', kind: 'eval', h: '4',
            t: ['Làm bài tập thiết kế: 10 nhóm, 200 lần chạy mỗi ngày, mô hình năng lực và chi phí, điểm nghẽn', 'Do a design kata: 10 teams, 200 runs a day, capacity and cost model, bottlenecks'],
            goal: ['Ước được số worker, chi phí LLM, chi phí hạ tầng và điểm nghẽn đầu tiên khi mở rộng.', 'You can estimate worker count, LLM cost, infrastructure cost and the first bottleneck when scaling.'],
            learn: [
              ['Ước lượng bằng số đo thật từ pilot: token mỗi lần chạy, thời gian runner, tỷ lệ retry', 'Estimate from real pilot data: tokens per run, runner time, retry rate'],
              ['Định luật Little: số job đang chạy = tốc độ đến × thời gian xử lý', 'Little\'s law: jobs in flight = arrival rate × processing time'],
              ['Rate limit của nhà cung cấp thường là điểm nghẽn trước cả CPU', 'Provider rate limits are often the bottleneck before CPU'],
            ],
            ai: [['Claude', ['Kiểm tra phép tính', 'Check the maths'], ['Đây là mô hình năng lực của tôi cho 10 nhóm, 200 lần chạy mỗi ngày, dựa trên số đo pilot: <dán giả định và phép tính>. Kiểm tra phép tính, chỉ ra giả định yếu nhất, điểm nghẽn đầu tiên, và cách kiến trúc cần thay đổi nếu tăng lên 2.000 lần chạy mỗi ngày.', 'Here is my capacity model for 10 teams and 200 runs a day, based on pilot data: <paste assumptions and maths>. Check the maths, point out the weakest assumption, the first bottleneck, and how the architecture must change at 2,000 runs a day.']]],
            res: [['Google SRE book', 'https://sre.google/sre-book/table-of-contents/']],
            pr: [
              ['Lấy số đo pilot từ dashboard', 'Pull pilot measurements from the dashboards'],
              ['Tính số worker, rate limit cần, chi phí tháng', 'Compute worker count, needed rate limits and monthly cost'],
              ['Ghi kết quả và hướng mở rộng vào tài liệu kiến trúc', 'Record the results and scaling path in the architecture doc'],
            ],
            done: [
              ['Có mô hình năng lực và chi phí cho 200 và 2.000 lần chạy mỗi ngày', 'There is a capacity and cost model for 200 and 2,000 runs a day'],
            ],
          },
        ],
      },
    },
  ],
};
