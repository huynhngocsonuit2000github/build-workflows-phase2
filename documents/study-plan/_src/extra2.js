// Round 2: closer to real projects. Build-vs-buy ADRs, agentic code search first, and the missing production topics.
const MS = 'https://learn.microsoft.com';

module.exports = {
  // "Write it yourself" items: add a build-vs-buy check against ready-made tools (appended, so existing ticks keep their place)
  adr: {
    's1-2': 'Stateless, Elsa Workflows',
    's2-2': 'Microsoft.Extensions.AI, LiteLLM',
    's3-2': 'Microsoft.Extensions.AI function invocation, MCP',
    's4-5': 'Microsoft Agent Framework / Semantic Kernel, LangGraph',
    's7-3': 'Kubernetes Jobs, ephemeral GitHub Actions runners, gVisor / Firecracker sandboxes',
    's8-2': 'Hangfire, MassTransit, Azure Service Bus',
    'ctx-2': 'Semantic Kernel / Microsoft Agent Framework context handling',
    'gw-2': 'LiteLLM, Azure API Management AI gateway, Portkey',
  },

  // Code retrieval: agentic search is the baseline, vectors are one option to compare (mainly for docs)
  replace: {
    'ctx-3': {
      id: 'ctx-3', kind: 'build', h: '4–5',
      t: ['Làm retrieval cho code bằng tìm kiếm kiểu agent (grep, glob, đọc file, tra symbol) và retrieval vector cho tài liệu', 'Build code retrieval with agentic search (grep, glob, file reads, symbol lookup) and vector retrieval for docs'],
      goal: ['Developer agent tự tìm code bằng công cụ tìm kiếm có giới hạn; tài liệu và yêu cầu cũ tìm được theo ngữ nghĩa.', 'The Developer agent finds code itself with bounded search tools; docs and past requirements are searchable by meaning.'],
      learn: [
        ['Nhiều coding agent hiện nay tìm code bằng các bước grep, glob, đọc file thay vì chỉ mục vector: luôn khớp code mới nhất, không phải đồng bộ chỉ mục', 'Many coding agents today find code with grep, glob and file-read steps rather than a vector index: always current, no index to keep in sync'],
        ['Tra symbol (Roslyn cho C#, TypeScript compiler cho Angular) bổ sung cho grep khi cần định nghĩa và nơi gọi', 'Symbol lookup (Roslyn for C#, the TypeScript compiler for Angular) complements grep for definitions and call sites'],
        ['Vector hợp hơn cho văn bản tự nhiên: tài liệu, yêu cầu cũ, quyết định, ticket tương tự', 'Vectors suit natural-language text better: docs, past requirements, decisions, similar tickets'],
        ['Công cụ tìm kiếm cũng phải có giới hạn: số kết quả, kích thước file, chỉ trong workspace', 'Search tools need limits too: result count, file size, workspace only'],
      ],
      ai: [['Claude Code', ['Dựng công cụ tìm kiếm', 'Build the search tools'], ['Trong `services/agent-service`, thêm công cụ chỉ đọc cho Developer agent: `grep(pattern, glob)`, `glob(pattern)`, `read_file(path, startLine, endLine)`, `find_symbol(name)` dùng Roslyn. Tất cả chỉ chạy trong worktree của lần chạy, giới hạn 50 kết quả và 400 dòng mỗi lần đọc. Riêng tài liệu trong `docs/` và các yêu cầu đã duyệt: thêm `search_docs(query)` dùng embedding lưu trong pgvector.', 'In `services/agent-service`, add read-only tools for the Developer agent: `grep(pattern, glob)`, `glob(pattern)`, `read_file(path, startLine, endLine)`, `find_symbol(name)` using Roslyn. All run only inside the run\'s worktree, capped at 50 results and 400 lines per read. For docs in `docs/` and approved requirements only, add `search_docs(query)` using embeddings stored in pgvector.']]],
      res: [['Roslyn APIs', MS + '/dotnet/csharp/roslyn-sdk/'], ['pgvector', 'https://github.com/pgvector/pgvector']],
      pr: [
        ['Làm 4 công cụ tìm code có giới hạn và đăng ký cho vai trò Developer', 'Build the 4 bounded code-search tools and register them for the Developer role'],
        ['Làm `search_docs` với pgvector cho tài liệu và yêu cầu đã duyệt', 'Build `search_docs` with pgvector for docs and approved requirements'],
        ['Làm thêm một bản tìm code bằng vector để có phương án so sánh ở bước sau', 'Also build a vector-based code search as a comparison option for the next item'],
      ],
      done: [
        ['Developer agent tìm được file cần sửa bằng công cụ tìm kiếm trong 3 feature thử', 'The Developer agent finds the files to change with the search tools on 3 trial features'],
        ['`search_docs` trả về tài liệu và yêu cầu liên quan theo ngữ nghĩa', '`search_docs` returns related docs and requirements by meaning'],
      ],
    },
    'ctx-4': {
      id: 'ctx-4', kind: 'eval', h: '3',
      t: ['So sánh tìm kiếm kiểu agent, vector và lai cho code; đo recall@k và kết quả cuối, rồi ghi ADR "Retrieval strategy"', 'Compare agentic, vector and hybrid search for code; measure recall@k and end results, then write the "Retrieval strategy" ADR'],
      goal: ['Chiến lược retrieval cho code và cho tài liệu được chọn bằng số đo.', 'The retrieval strategy for code and for docs is chosen from measurements.'],
      learn: [
        ['recall@k và MRR cho retrieval; tỷ lệ hoàn thành feature cho kết quả cuối', 'recall@k and MRR for retrieval; feature completion rate for the end result'],
        ['Tìm kiếm kiểu agent tốn thêm bước và token; vector tốn công đồng bộ chỉ mục', 'Agentic search costs extra steps and tokens; vectors cost index syncing'],
      ],
      ai: [['ChatGPT', ['Soạn bộ câu hỏi', 'Draft the question set'], ['Từ repo TaskLite của tôi (cấu trúc: <dán cây thư mục>), soạn 20 câu hỏi kiểu một Developer agent sẽ hỏi khi làm feature, mỗi câu kèm file hoặc method đúng. Trộn: tìm theo tên chính xác, theo khái niệm, theo hành vi, theo mã lỗi.', 'From my TaskLite repo (layout: <paste tree>), write 20 questions a Developer agent would ask while building a feature, each with the correct files or methods. Mix: exact-name, concept, behaviour and error-code lookups.']]],
      res: [['Anthropic: Contextual Retrieval', 'https://www.anthropic.com/news/contextual-retrieval']],
      pr: [
        ['Chạy 3 cách (agent, vector, lai) trên 20 câu hỏi, tính recall@5 và MRR', 'Run the 3 approaches (agentic, vector, hybrid) on 20 questions and compute recall@5 and MRR'],
        ['Chạy Developer agent trên 3 feature với từng cách, ghi tỷ lệ hoàn thành, token, thời gian', 'Run the Developer agent on 3 features with each approach; record completion, tokens and time'],
        ['Viết ADR "Retrieval strategy" riêng cho code và cho tài liệu', 'Write the "Retrieval strategy" ADR, separately for code and for docs'],
      ],
      done: [
        ['Có bảng so sánh 3 cách về recall, tỷ lệ hoàn thành, token và thời gian', 'There is a comparison of the 3 approaches on recall, completion, tokens and time'],
        ['ADR nêu rõ cách dùng cho code và cách dùng cho tài liệu, kèm lý do', 'The ADR states the approach for code and for docs, with reasons'],
      ],
    },
  },

  items: [
    {
      phase: 'p1', ms: 's2', item: {
        id: 's2-6', kind: 'learn', h: '2',
        t: ['Đặt quy tắc dữ liệu gửi cho nhà cung cấp LLM: phân loại dữ liệu, điều khoản lưu giữ, gói doanh nghiệp', 'Set rules for data sent to LLM providers: data classification, retention terms, enterprise plans'],
        goal: ['Biết rõ dữ liệu nào được phép gửi tới model nào, và điều khoản của nhà cung cấp về lưu giữ và huấn luyện.', 'You know which data may go to which model, and the provider\'s terms on retention and training.'],
        learn: [
          ['Gói API và gói doanh nghiệp khác nhau về lưu giữ dữ liệu và việc dùng dữ liệu để huấn luyện', 'API and enterprise plans differ on data retention and use for training'],
          ['Phân loại: công khai, nội bộ, mật, dữ liệu cá nhân; mỗi mức được gửi tới đâu', 'Classification: public, internal, confidential, personal data; where each level may go'],
          ['Vùng lưu trữ dữ liệu (data residency) và yêu cầu của khách hàng', 'Data residency and customer requirements'],
          ['Code nguồn của khách hàng thường là dữ liệu mật', 'Customer source code is usually confidential data'],
        ],
        ai: [['Claude', ['Soạn chính sách', 'Draft the policy'], ['Soạn chính sách dữ liệu một trang cho nền tảng AI delivery: bảng phân loại dữ liệu (mô tả ticket, code nguồn, log, dữ liệu cá nhân trong ticket), mức nào được gửi tới nhà cung cấp LLM nào và với điều kiện gì, việc cần kiểm tra trong điều khoản nhà cung cấp (lưu giữ, huấn luyện, vùng dữ liệu), và việc che dữ liệu trước khi gửi.', 'Draft a one-page data policy for the AI delivery platform: a classification table (ticket descriptions, source code, logs, personal data in tickets), which level may go to which LLM provider and under what conditions, what to check in provider terms (retention, training, residency), and redaction before sending.']]],
        res: [['Anthropic: privacy and data usage', 'https://privacy.anthropic.com/'], ['OpenAI: enterprise privacy', 'https://openai.com/enterprise-privacy/']],
        pr: [
          ['Đọc điều khoản dữ liệu của nhà cung cấp bạn dùng', 'Read the data terms of the provider you use'],
          ['Viết `docs/security/llm-data-policy.md` với bảng phân loại', 'Write `docs/security/llm-data-policy.md` with the classification table'],
        ],
        done: [
          ['Chính sách ghi rõ dữ liệu nào được gửi tới nhà cung cấp nào', 'The policy states which data may go to which provider'],
        ],
      },
    },
    {
      phase: 'p1', ms: 's2', item: {
        id: 's2-7', kind: 'build', h: '2–3',
        t: ['Ghim version model và lập quy trình đổi model chỉ khi eval đạt', 'Pin model versions and set up a model change process gated by evals'],
        goal: ['Không có model nào tự đổi phía dưới hệ thống; mỗi lần đổi đều qua eval và có thể quay lại.', 'No model changes underneath the system by itself; every change goes through evals and can be reverted.'],
        learn: [
          ['Dùng tên model có version cụ thể, không dùng bí danh "latest"', 'Use specific versioned model names, not "latest" aliases'],
          ['Model bị ngừng hỗ trợ theo lịch của nhà cung cấp; cần theo dõi và chuyển trước hạn', 'Models are retired on the provider\'s schedule; track it and migrate early'],
          ['Đổi model là một thay đổi như đổi code: PR, eval, duyệt, có đường lùi', 'Changing a model is like changing code: a PR, evals, approval, a way back'],
        ],
        ai: [['ChatGPT', ['Soạn quy trình', 'Draft the process'], ['Soạn quy trình đổi model cho nền tảng của tôi: ai đề xuất, chạy eval harness trên những bộ ca nào, ngưỡng điểm và chi phí để chấp nhận, chạy song song bao lâu, cách quay lại model cũ, và cách theo dõi lịch ngừng hỗ trợ của nhà cung cấp.', 'Draft a model change process for my platform: who proposes it, which eval sets to run, score and cost thresholds for acceptance, how long to run in parallel, how to roll back to the old model, and how to track the provider\'s retirement schedule.']]],
        res: [['Anthropic docs: models', 'https://docs.anthropic.com/']],
        pr: [
          ['Đưa tên model có version vào cấu hình, có kiểm tra cấm bí danh', 'Put versioned model names in config, with a check that rejects aliases'],
          ['Thêm job CI chạy eval harness khi cấu hình model thay đổi', 'Add a CI job that runs the eval harness when model config changes'],
          ['Viết `docs/runbooks/model-change.md`', 'Write `docs/runbooks/model-change.md`'],
        ],
        done: [
          ['Đổi model trong cấu hình kích hoạt eval và cần duyệt trước khi merge', 'Changing the model config triggers evals and needs approval before merge'],
        ],
      },
    },
    {
      phase: 'p1', ms: 's5', before: 's5-2', item: {
        id: 's5-5', kind: 'eval', h: '3–4',
        t: ['Chọn coding agent có sẵn làm động cơ cho AI Developer: so Claude Code, Codex CLI, Copilot coding agent và agy', 'Choose a ready-made coding agent as the AI Developer engine: compare Claude Code, Codex CLI, Copilot coding agent and agy'],
        goal: ['AI Developer bọc một coding agent được chọn bằng số đo, thay vì tự xây agent viết code từ đầu.', 'The AI Developer wraps a coding agent chosen from measurements, instead of building a code-writing agent from scratch.'],
        learn: [
          ['Trong thực tế, phần viết code thường giao cho coding agent có sẵn; giá trị của nền tảng nằm ở điều phối, kiểm soát và bằng chứng', 'In practice, code writing usually goes to an existing coding agent; the platform\'s value is orchestration, control and evidence'],
          ['Tiêu chí: chất lượng trên feature thật, chạy headless, kiểm soát công cụ và quyền, đầu ra máy đọc được, chi phí, điều khoản dữ liệu', 'Criteria: quality on real features, headless mode, tool and permission control, machine-readable output, cost, data terms'],
          ['Bài học phase 1: agent hỏi lại chi tiết nhỏ, quên commit; cần prompt và kiểm tra bao quanh', 'Phase 1 lesson: agents ask about small details and forget to commit; they need prompts and checks around them'],
        ],
        ai: [['Claude', ['Thiết kế bài so sánh', 'Design the comparison'], ['Thiết kế bài so sánh 3–4 coding agent chạy headless làm động cơ cho AI Developer của tôi trên 4 feature của ứng dụng TaskLite. Số đo: AC đạt sau build/test của hệ thống, số vòng sửa sau review, token và chi phí, thời gian, mức kiểm soát công cụ và quyền, khả năng xuất kết quả có cấu trúc. Nêu cách chấm công bằng.', 'Design a comparison of 3–4 headless coding agents as the engine for my AI Developer on 4 TaskLite features. Measures: ACs passing the system\'s build/test, fix rounds after review, tokens and cost, time, control over tools and permissions, structured output. Explain how to score fairly.']]],
        res: [['Claude Code docs', 'https://code.claude.com/docs/en/overview'], ['GitHub Copilot coding agent', 'https://docs.github.com/copilot']],
        pr: [
          ['Chọn 4 feature thử và chạy từng coding agent qua cùng một lớp bọc', 'Pick 4 trial features and run each coding agent through the same wrapper'],
          ['Ghi số đo vào bảng', 'Record the measurements in a table'],
          ['Viết ADR "Coding agent for the AI Developer"', 'Write the "Coding agent for the AI Developer" ADR'],
        ],
        done: [
          ['Có ADR chọn coding agent với bảng số đo', 'There is an ADR choosing the coding agent, with a measurement table'],
          ['Lớp bọc cho phép đổi coding agent bằng cấu hình', 'The wrapper lets you switch coding agents through configuration'],
        ],
      },
    },
    {
      phase: 'p3', ms: 's16', before: 's16-2', item: {
        id: 's16-4', kind: 'eval', h: '3',
        t: ['Áp dụng quy ước OpenTelemetry cho GenAI và đánh giá công cụ quan sát LLM có sẵn (build hay mua)', 'Adopt the OpenTelemetry GenAI semantic conventions and evaluate ready-made LLM observability tools (build vs buy)'],
        goal: ['Thuộc tính trace của lời gọi LLM theo chuẩn chung, và quyết định tự dựng dashboard hay dùng công cụ quan sát LLM có sẵn.', 'LLM call trace attributes follow the shared standard, and you decide between your own dashboards and a ready-made LLM observability tool.'],
        learn: [
          ['GenAI semantic conventions: tên span và thuộc tính chuẩn cho model, token, công cụ, agent', 'GenAI semantic conventions: standard span names and attributes for models, tokens, tools, agents'],
          ['Công cụ quan sát LLM (ví dụ Langfuse) có sẵn trace prompt, chi phí, đánh giá, phản hồi', 'LLM observability tools (e.g. Langfuse) provide prompt traces, cost, evaluations and feedback out of the box'],
          ['Tự host hay dịch vụ: dữ liệu prompt có thể là dữ liệu mật', 'Self-hosted vs SaaS: prompt data may be confidential'],
        ],
        ai: [['ChatGPT', ['So sánh có tiêu chí', 'Compare with criteria'], ['So sánh việc tự dựng dashboard từ OpenTelemetry với việc dùng một công cụ quan sát LLM tự host (ví dụ Langfuse) cho nền tảng của tôi, theo: trace prompt và kết quả, chi phí theo lần chạy, đánh giá và phản hồi của người, dữ liệu mật, công vận hành. Kết luận nên chọn gì cho team 3–10 người.', 'Compare building dashboards from OpenTelemetry with using a self-hosted LLM observability tool (e.g. Langfuse) for my platform on: prompt and result tracing, cost per run, evaluations and human feedback, confidential data, operating effort. Conclude what a 3–10 person team should pick.']]],
        res: [['OpenTelemetry GenAI semantic conventions', 'https://opentelemetry.io/docs/specs/semconv/gen-ai/'], ['Langfuse', 'https://langfuse.com/docs']],
        pr: [
          ['Đổi tên thuộc tính span LLM theo GenAI semantic conventions', 'Rename LLM span attributes to the GenAI semantic conventions'],
          ['Thử gửi trace sang một công cụ quan sát LLM tự host', 'Try sending traces to a self-hosted LLM observability tool'],
          ['Viết ADR "LLM observability"', 'Write the "LLM observability" ADR'],
        ],
        done: [
          ['Span LLM dùng thuộc tính chuẩn GenAI', 'LLM spans use the standard GenAI attributes'],
          ['Có ADR chọn tự dựng hay dùng công cụ có sẵn', 'There is an ADR choosing build or buy'],
        ],
      },
    },
  ],
};
