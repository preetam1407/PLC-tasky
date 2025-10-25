import { useCallback, useEffect, useMemo, useState } from "react";
import classNames from "classnames";
import {
  TaskDto,
  createTask,
  deleteTask,
  listTasks,
  toggleTask,
  updateTask
} from "./api";

type Filter = "all" | "active" | "completed";

const FILTER_STORAGE_KEY = "tasky.v1.filter";

const FILTER_LABELS: Record<Filter, string> = {
  all: "All Tasks",
  active: "Active",
  completed: "Completed"
};

function resolveStoredFilter(): Filter {
  if (typeof window === "undefined") return "all";
  const stored = window.localStorage.getItem(FILTER_STORAGE_KEY);
  return stored === "active" || stored === "completed" ? stored : "all";
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString();
}

export default function App() {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [filter, setFilter] = useState<Filter>(() => resolveStoredFilter());
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [newTask, setNewTask] = useState("");
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null);

  const fetchTasks = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await listTasks({
        status: filter,
        search: search.trim() ? search.trim() : undefined
      });
      setTasks(items);
    } catch (err) {
      console.error(err);
      setError("Unable to load tasks. Check that the API is running on https://localhost:7403.");
    } finally {
      setLoading(false);
    }
  }, [filter, search]);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  useEffect(() => {
    if (typeof window !== "undefined") {
      window.localStorage.setItem(FILTER_STORAGE_KEY, filter);
    }
  }, [filter]);

  const summary = useMemo(() => {
    const total = tasks.length;
    const completed = tasks.filter((t) => t.isCompleted).length;
    return {
      total,
      completed,
      remaining: total - completed
    };
  }, [tasks]);

  const handleCreateTask = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const description = newTask.trim();
    if (!description) return;

    setCreating(true);
    try {
      const created = await createTask(description);
      setTasks((prev) => [created, ...prev]);
      setNewTask("");
    } catch (err) {
      console.error(err);
      setError("Unable to create the task. Please retry.");
    } finally {
      setCreating(false);
    }
  };

  const handleToggle = async (task: TaskDto) => {
    setBusyTaskId(task.id);
    try {
      await toggleTask(task.id);
      setTasks((prev) =>
        prev.map((t) => (t.id === task.id ? { ...t, isCompleted: !t.isCompleted } : t))
      );
    } catch (err) {
      console.error(err);
      setError("Unable to update the task. Please retry.");
    } finally {
      setBusyTaskId(null);
    }
  };

  const handleDelete = async (task: TaskDto) => {
    if (!window.confirm(`Delete "${task.description}"?`)) {
      return;
    }
    setBusyTaskId(task.id);
    try {
      await deleteTask(task.id);
      setTasks((prev) => prev.filter((t) => t.id !== task.id));
    } catch (err) {
      console.error(err);
      setError("Unable to delete the task. Please retry.");
    } finally {
      setBusyTaskId(null);
    }
  };

  const handleInlineEdit = async (task: TaskDto, description: string) => {
    const trimmed = description.trim();
    if (!trimmed || trimmed === task.description) return;

    setBusyTaskId(task.id);
    try {
      await updateTask(task.id, { description: trimmed, isCompleted: task.isCompleted });
      setTasks((prev) =>
        prev.map((t) => (t.id === task.id ? { ...t, description: trimmed } : t))
      );
    } catch (err) {
      console.error(err);
      setError("Unable to rename the task. Please retry.");
    } finally {
      setBusyTaskId(null);
    }
  };

  return (
    <div className="app-shell">
      <header className="top-bar">
        <div className="hero-copy">
          <span className="brand-badge">Tasky v1</span>
          <h1>Today's tasks</h1>
          <p className="subtitle">Plan, capture, and complete what matters most right now.</p>
        </div>
        <div className="summary-pill" aria-live="polite">
          <span className="value">{summary.remaining}</span>
          <span className="label">Remaining</span>
        </div>
      </header>

      <section className="summary-grid" aria-label="Task overview">
        <article className="summary-card">
          <span className="summary-label">All tasks</span>
          <span className="summary-value">{summary.total}</span>
          <span className="summary-subtext">Across every filter</span>
        </article>
        <article className="summary-card">
          <span className="summary-label">Completed</span>
          <span className="summary-value">{summary.completed}</span>
          <span className="summary-subtext">Keep the streak going</span>
        </article>
        <article className="summary-card">
          <span className="summary-label">Active</span>
          <span className="summary-value">{summary.remaining}</span>
          <span className="summary-subtext">Focus for today</span>
        </article>
      </section>

      <form className="task-form" onSubmit={handleCreateTask}>
        <label className="sr-only" htmlFor="task-input">
          Task description
        </label>
        <input
          id="task-input"
          className="app-input"
          placeholder='Add a new task (e.g. "Review PR #42")'
          value={newTask}
          onChange={(event) => setNewTask(event.target.value)}
          disabled={creating}
          autoComplete="off"
        />
        <button type="submit" className="primary-button" disabled={creating}>
          {creating ? "Adding..." : "Add Task"}
        </button>
      </form>

      <div className="toolbar">
        <div className="filters" role="group" aria-label="Filter tasks">
          {(Object.keys(FILTER_LABELS) as Filter[]).map((key) => (
            <button
              key={key}
              type="button"
              className={classNames("filter-button", { active: filter === key })}
              onClick={() => setFilter(key)}
            >
              {FILTER_LABELS[key]}
            </button>
          ))}
        </div>
        <input
          type="search"
          className="search-field"
          placeholder="Search tasks"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          aria-label="Search tasks"
        />
      </div>

      {error && (
        <p role="alert" className="error-alert">
          {error}
        </p>
      )}

      {loading ? (
        <div className="empty-state">Loading tasks...</div>
      ) : tasks.length === 0 ? (
        <div className="empty-state">
          No tasks yet. Add your first task above to get started!
        </div>
      ) : (
        <div className="task-list">
          {tasks.map((task) => (
            <article
              key={task.id}
              className={classNames("task-item", { completed: task.isCompleted })}
            >
              <label className="task-checkbox">
                <input
                  type="checkbox"
                  checked={task.isCompleted}
                  onChange={() => handleToggle(task)}
                  disabled={busyTaskId === task.id}
                  aria-label={task.isCompleted ? "Mark task as active" : "Mark task as completed"}
                />
              </label>
              <div className="task-body">
                <InlineEditableText
                  value={task.description}
                  disabled={busyTaskId === task.id}
                  className={classNames("task-title", { completed: task.isCompleted })}
                  inputClassName="task-title-input"
                  onSubmit={(value) => handleInlineEdit(task, value)}
                />
                <span className="task-meta">Added {formatDate(task.createdAtUtc)}</span>
              </div>
              <div className="task-controls">
                <button
                  type="button"
                  className="danger-button"
                  onClick={() => handleDelete(task)}
                  disabled={busyTaskId === task.id}
                >
                  Delete
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

interface InlineEditableTextProps {
  value: string;
  onSubmit: (value: string) => void;
  disabled?: boolean;
  className?: string;
  inputClassName?: string;
}

function InlineEditableText({
  value,
  onSubmit,
  disabled,
  className,
  inputClassName
}: InlineEditableTextProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  const handleSubmit = () => {
    const trimmed = draft.trim();
    if (trimmed !== value.trim()) {
      onSubmit(trimmed);
    }
    setEditing(false);
  };

  if (!editing) {
    return (
      <p
        className={className}
        onDoubleClick={() => !disabled && setEditing(true)}
        style={{ cursor: disabled ? "not-allowed" : "text" }}
      >
        {value}
      </p>
    );
  }

  return (
    <input
      type="text"
      value={draft}
      onChange={(event) => setDraft(event.target.value)}
      onBlur={handleSubmit}
      onKeyDown={(event) => {
        if (event.key === "Enter") {
          handleSubmit();
        } else if (event.key === "Escape") {
          setDraft(value);
          setEditing(false);
        }
      }}
      autoFocus
      disabled={disabled}
      className={classNames("task-title-input", inputClassName)}
    />
  );
}
