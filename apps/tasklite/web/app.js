let apiBaseUrl = "";

const errorEl = document.getElementById("error");
const userIdInput = document.getElementById("user-id");
const projectSelect = document.getElementById("project-select");
const taskTableBody = document.getElementById("task-tbody");
const createTaskForm = document.getElementById("create-task-form");
const taskTitleInput = document.getElementById("task-title");
const taskAssigneeInput = document.getElementById("task-assignee");
const taskDueInput = document.getElementById("task-due");
const taskCountBadge = document.getElementById("task-count");

function getUserId() {
  return userIdInput.value.trim();
}

function clearError() {
  errorEl.innerHTML = "";
  errorEl.style.display = "none";
}

function showError(problem) {
  clearError();
  const title = problem.title || "An error occurred";
  let html = `<strong>⚠️ ${escapeHtml(title)}</strong>`;

  if (problem.detail) {
    html += `<div style="margin-top: 4px;">${escapeHtml(problem.detail)}</div>`;
  }

  if (problem.errors && typeof problem.errors === "object") {
    html += "<ul>";
    for (const [field, messages] of Object.entries(problem.errors)) {
      if (Array.isArray(messages)) {
        for (const msg of messages) {
          html += `<li><strong>${escapeHtml(field)}</strong>: ${escapeHtml(msg)}</li>`;
        }
      } else {
        html += `<li><strong>${escapeHtml(field)}</strong>: ${escapeHtml(messages)}</li>`;
      }
    }
    html += "</ul>";
  }

  errorEl.innerHTML = html;
  errorEl.style.display = "block";
}

function escapeHtml(str) {
  if (!str) return "";
  return String(str)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function updateTaskCount(count) {
  if (taskCountBadge) {
    taskCountBadge.textContent = `${count} ${count === 1 ? "task" : "tasks"}`;
  }
}

async function loadProjects() {
  clearError();
  const userId = getUserId();
  if (!userId) {
    projectSelect.innerHTML = "";
    taskTableBody.innerHTML = "";
    updateTaskCount(0);
    return;
  }

  try {
    const res = await fetch(`${apiBaseUrl}/projects`, {
      headers: {
        "X-User-Id": userId,
      },
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({ title: `HTTP ${res.status}` }));
      showError(err);
      projectSelect.innerHTML = "";
      taskTableBody.innerHTML = "";
      updateTaskCount(0);
      return;
    }

    const projects = await res.json();
    projectSelect.innerHTML = "";

    if (projects.length === 0) {
      const opt = document.createElement("option");
      opt.value = "";
      opt.textContent = "-- No projects found --";
      projectSelect.appendChild(opt);
      taskTableBody.innerHTML = "";
      updateTaskCount(0);
      return;
    }

    for (const p of projects) {
      const opt = document.createElement("option");
      opt.value = p.id;
      opt.textContent = `${p.name} (#${p.id})`;
      projectSelect.appendChild(opt);
    }

    await loadTasks(projectSelect.value);
  } catch (ex) {
    showError({ title: "Network error", detail: ex.message });
  }
}

async function loadTasks(projectId) {
  clearError();
  if (!projectId) {
    taskTableBody.innerHTML = "";
    updateTaskCount(0);
    return;
  }

  const userId = getUserId();
  try {
    const res = await fetch(`${apiBaseUrl}/projects/${projectId}/tasks`, {
      headers: {
        "X-User-Id": userId,
      },
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({ title: `HTTP ${res.status}` }));
      showError(err);
      taskTableBody.innerHTML = "";
      updateTaskCount(0);
      return;
    }

    const tasks = await res.json();
    renderTasks(tasks);
    updateTaskCount(tasks.length);
  } catch (ex) {
    showError({ title: "Network error", detail: ex.message });
  }
}

function renderTasks(tasks) {
  taskTableBody.innerHTML = "";

  if (tasks.length === 0) {
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 5;
    td.style.textAlign = "center";
    td.style.color = "#94a3b8";
    td.style.padding = "32px";
    td.textContent = "No tasks yet. Create one below!";
    tr.appendChild(td);
    taskTableBody.appendChild(tr);
    return;
  }

  for (const t of tasks) {
    const tr = document.createElement("tr");

    // Id
    const tdId = document.createElement("td");
    tdId.className = "task-id";
    tdId.textContent = `#${t.id}`;
    tr.appendChild(tdId);

    // Title
    const tdTitle = document.createElement("td");
    tdTitle.className = "task-title-cell";
    tdTitle.textContent = t.title;
    tr.appendChild(tdTitle);

    // Status Badge
    const tdStatus = document.createElement("td");
    const statusClass = (t.status || "Todo").toLowerCase();
    tdStatus.innerHTML = `<span class="status-badge status-${statusClass}">${escapeHtml(t.status)}</span>`;
    tr.appendChild(tdStatus);

    // Assignee
    const tdAssignee = document.createElement("td");
    if (t.assignee) {
      const initial = t.assignee.charAt(0).toUpperCase();
      tdAssignee.innerHTML = `
        <div class="assignee-cell">
          <span class="avatar">${escapeHtml(initial)}</span>
          <span>${escapeHtml(t.assignee)}</span>
        </div>
      `;
    } else {
      tdAssignee.innerHTML = `<span class="empty-val">—</span>`;
    }
    tr.appendChild(tdAssignee);

    // Due Date
    const tdDue = document.createElement("td");
    if (t.dueDate) {
      tdDue.textContent = t.dueDate;
    } else {
      tdDue.innerHTML = `<span class="empty-val">—</span>`;
    }
    tr.appendChild(tdDue);

    taskTableBody.appendChild(tr);
  }
}

async function handleCreateTask(e) {
  e.preventDefault();
  clearError();

  const projectId = projectSelect.value;
  if (!projectId) {
    showError({ title: "Validation error", detail: "Please select a project first." });
    return;
  }

  const userId = getUserId();
  const payload = {
    title: taskTitleInput.value,
    assignee: taskAssigneeInput.value.trim() || null,
    dueDate: taskDueInput.value || null,
  };

  try {
    const res = await fetch(`${apiBaseUrl}/projects/${projectId}/tasks`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-User-Id": userId,
      },
      body: JSON.stringify(payload),
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({ title: `HTTP ${res.status}` }));
      showError(err);
      return;
    }

    createTaskForm.reset();
    await loadTasks(projectId);
  } catch (ex) {
    showError({ title: "Network error", detail: ex.message });
  }
}

async function init() {
  const savedUserId = localStorage.getItem("tasklite_user_id") || "alice";
  userIdInput.value = savedUserId;

  userIdInput.addEventListener("change", () => {
    localStorage.setItem("tasklite_user_id", userIdInput.value.trim());
    loadProjects();
  });

  projectSelect.addEventListener("change", () => {
    loadTasks(projectSelect.value);
  });

  createTaskForm.addEventListener("submit", handleCreateTask);

  try {
    const configRes = await fetch("config.json");
    const config = await configRes.json();
    apiBaseUrl = config.apiBaseUrl;
    await loadProjects();
  } catch (ex) {
    showError({
      title: "Configuration error",
      detail: "Failed to load config.json: " + ex.message,
    });
  }
}

init();
