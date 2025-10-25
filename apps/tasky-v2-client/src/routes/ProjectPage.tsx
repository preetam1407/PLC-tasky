import { useCallback, useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import {
  ProjectDetailResponse,
  ScheduleResponse,
  TaskResponse,
  projectApi,
  taskApi
} from "../api/client";

type TaskForm = {
  title: string;
  dueDate: string;
};

const DEFAULT_WORKING_DAYS = ["Mon", "Tue", "Wed", "Thu", "Fri"] as const;

export default function ProjectPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();

  const [project, setProject] = useState<ProjectDetailResponse | null>(null);
  const [tasks, setTasks] = useState<TaskResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null);

  const [schedule, setSchedule] = useState<ScheduleResponse | null>(null);
  const [scheduleLoading, setScheduleLoading] = useState(false);
  const [scheduleError, setScheduleError] = useState<string | null>(null);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [dailyCapacity, setDailyCapacity] = useState(3);
  const [workingDays, setWorkingDays] = useState<string[]>([...DEFAULT_WORKING_DAYS]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { isSubmitting }
  } = useForm<TaskForm>({
    defaultValues: {
      title: "",
      dueDate: ""
    }
  });

  const loadProject = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const detail = await projectApi.get(projectId);
      setProject(detail);
      setTasks(detail.tasks);
    } catch (err) {
      console.error(err);
      setError("Unable to load project. It may have been deleted.");
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    loadProject();
  }, [loadProject]);

  const taskMap = useMemo(() => new Map(tasks.map((t) => [t.id, t])), [tasks]);

  const onSubmit = handleSubmit(async (values) => {
    if (!projectId) return;
    try {
      const created = await taskApi.create(projectId, {
        title: values.title,
        dueDate: values.dueDate ? values.dueDate : null
      });
      setTasks((prev) => [created, ...prev]);
      reset();
    } catch (err) {
      console.error(err);
      setError("Unable to create task.");
    }
  });

  const handleToggle = async (task: TaskResponse) => {
    setBusyTaskId(task.id);
    try {
      await taskApi.toggle(task.id);
      setTasks((prev) =>
        prev.map((t) => (t.id === task.id ? { ...t, isCompleted: !t.isCompleted } : t))
      );
    } catch (err) {
      console.error(err);
      setError("Unable to toggle task.");
    } finally {
      setBusyTaskId(null);
    }
  };

  const handleDelete = async (task: TaskResponse) => {
    if (!window.confirm(`Delete task "${task.title}"?`)) return;
    setBusyTaskId(task.id);
    try {
      await taskApi.remove(task.id);
      setTasks((prev) => prev.filter((t) => t.id !== task.id));
    } catch (err) {
      console.error(err);
      setError("Unable to delete task.");
    } finally {
      setBusyTaskId(null);
    }
  };

  const handleEdit = async (task: TaskResponse) => {
    const nextTitle = window.prompt("Task title", task.title);
    if (!nextTitle || nextTitle.trim().length === 0) return;
    const nextDue = window.prompt(
      "Due date (YYYY-MM-DD, optional)",
      task.dueDate ? task.dueDate.substring(0, 10) : ""
    );
    const normalizedDue =
      nextDue && nextDue.trim().length > 0 ? `${nextDue.trim()}` : null;

    setBusyTaskId(task.id);
    try {
      await taskApi.update(task.id, {
        title: nextTitle.trim(),
        dueDate: normalizedDue,
        isCompleted: task.isCompleted
      });
      setTasks((prev) =>
        prev.map((t) =>
          t.id === task.id
            ? { ...t, title: nextTitle.trim(), dueDate: normalizedDue }
            : t
        )
      );
    } catch (err) {
      console.error(err);
      setError("Unable to update task.");
    } finally {
      setBusyTaskId(null);
    }
  };

  const handleBack = () => navigate("/");

  const toggleWorkingDay = (day: string) => {
    setWorkingDays((prev) =>
      prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day]
    );
  };

  const handleSchedule = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!projectId) return;
    setScheduleError(null);
    setScheduleLoading(true);
    try {
      const payload = {
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        dailyCapacity: dailyCapacity || undefined,
        workingDays: workingDays.length > 0 ? workingDays : undefined
      };
      const result = await projectApi.schedule(projectId, payload);
      setSchedule(result);
    } catch (err) {
      console.error(err);
      setScheduleError("Unable to generate schedule. Ensure tasks exist and try again.");
    } finally {
      setScheduleLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="page-container">
        <p className="muted">Loading project…</p>
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className="page-container">
        <p role="alert" style={{ color: "salmon", fontWeight: 600 }}>
          {error ?? "Project not found."}
        </p>
        <button className="primary-button" type="button" onClick={handleBack}>
          Back to dashboard
        </button>
      </div>
    );
  }

  const completedTasks = tasks.filter((t) => t.isCompleted).length;

  return (
    <div className="page-container">
      <section className="card stack">
        <header className="flex" style={{ justifyContent: "space-between", alignItems: "flex-start" }}>
          <div>
            <h2>{project.title}</h2>
            <p className="muted">{project.description || "No description provided."}</p>
            <p className="muted">
              {tasks.length} tasks · {completedTasks} completed
            </p>
          </div>
          <button type="button" onClick={handleBack}>
            ← Back
          </button>
        </header>

        {error && (
          <p role="alert" style={{ color: "salmon", fontWeight: 600 }}>
            {error}
          </p>
        )}

        <form onSubmit={onSubmit}>
          <h3>Create task</h3>
          <label>
            Title
            <input
              type="text"
              placeholder="Design API contract"
              required
              {...register("title")}
            />
          </label>
          <label>
            Due date <span className="muted">(optional)</span>
            <input type="date" {...register("dueDate")} />
          </label>
          <div className="form-actions">
            <button className="primary-button" type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Adding…" : "Add task"}
            </button>
          </div>
        </form>

        <div>
          <h3>Tasks</h3>
          {tasks.length === 0 ? (
            <p className="muted">No tasks yet. Create one above.</p>
          ) : (
            <div className="task-list">
              {tasks.map((task) => (
                <article
                  key={task.id}
                  className={`task-item ${task.isCompleted ? "completed" : ""}`}
                >
                  <div>
                    <strong>{task.title}</strong>
                    <p className="muted">
                      Created {new Date(task.createdAtUtc).toLocaleString()}
                    </p>
                    {task.dueDate && (
                      <span className="chip">
                        Due {new Date(task.dueDate).toLocaleDateString()}
                      </span>
                    )}
                  </div>
                  <div className="flex" style={{ alignItems: "center" }}>
                    <button
                      type="button"
                      onClick={() => handleToggle(task)}
                      disabled={busyTaskId === task.id}
                    >
                      {task.isCompleted ? "Mark active" : "Mark done"}
                    </button>
                    <button
                      type="button"
                      onClick={() => handleEdit(task)}
                      disabled={busyTaskId === task.id}
                    >
                      Edit
                    </button>
                    <button
                      className="danger-button"
                      type="button"
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
      </section>

      <section className="card">
        <h3>Smart Scheduler</h3>
        <p className="muted">
          Generate a recommended order for pending tasks. Configure the working days and capacity.
        </p>
        <form onSubmit={handleSchedule} className="grid">
          <div className="flex" style={{ gap: "1.5rem" }}>
            <label style={{ flex: 1 }}>
              Start date
              <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
            </label>
            <label style={{ flex: 1 }}>
              End date
              <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
            </label>
            <label style={{ width: "150px" }}>
              Daily capacity
              <input
                type="number"
                min={1}
                max={10}
                value={dailyCapacity}
                onChange={(e) => {
                  const value = Number(e.target.value);
                  setDailyCapacity(Number.isFinite(value) && value > 0 ? value : 1);
                }}
              />
            </label>
          </div>

          <fieldset style={{ border: "none", padding: 0 }}>
            <legend className="muted" style={{ marginBottom: "0.5rem" }}>
              Working days
            </legend>
            <div className="flex">
              {DEFAULT_WORKING_DAYS.map((day) => (
                <label key={day} style={{ display: "inline-flex", alignItems: "center", gap: "0.45rem" }}>
                  <input
                    type="checkbox"
                    checked={workingDays.includes(day)}
                    onChange={() => toggleWorkingDay(day)}
                  />
                  {day}
                </label>
              ))}
              <label style={{ display: "inline-flex", alignItems: "center", gap: "0.45rem" }}>
                <input
                  type="checkbox"
                  checked={workingDays.includes("Sat")}
                  onChange={() => toggleWorkingDay("Sat")}
                />
                Sat
              </label>
              <label style={{ display: "inline-flex", alignItems: "center", gap: "0.45rem" }}>
                <input
                  type="checkbox"
                  checked={workingDays.includes("Sun")}
                  onChange={() => toggleWorkingDay("Sun")}
                />
                Sun
              </label>
            </div>
          </fieldset>

          <div className="form-actions">
            <button className="primary-button" type="submit" disabled={scheduleLoading}>
              {scheduleLoading ? "Generating…" : "Generate plan"}
            </button>
          </div>
        </form>

        {scheduleError && (
          <p role="alert" style={{ color: "salmon", fontWeight: 600 }}>
            {scheduleError}
          </p>
        )}

        {schedule && (
          <div className="scheduler-result">
            <p className="muted">
              Generated {new Date(schedule.generatedAtUtc).toLocaleString()} for{" "}
              {schedule.days.length} working day(s).
            </p>
            {schedule.days.map((day) => (
              <div className="day-plan" key={day.date}>
                <strong>{new Date(day.date).toLocaleDateString()}</strong>
                <ul>
                  {day.taskIds.length === 0 ? (
                    <li className="muted">No tasks planned.</li>
                  ) : (
                    day.taskIds.map((taskId) => (
                      <li key={taskId}>{taskMap.get(taskId)?.title ?? `Task ${taskId}`}</li>
                    ))
                  )}
                </ul>
              </div>
            ))}
            {schedule.days.length === 0 && <p className="muted">No pending tasks to schedule.</p>}
          </div>
        )}
      </section>
    </div>
  );
}
