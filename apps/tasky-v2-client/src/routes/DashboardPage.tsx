import { useCallback, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { ProjectResponse, projectApi } from "../api/client";
import { useAuth } from "../context/AuthContext";

type ProjectForm = {
  title: string;
  description: string;
};

export default function DashboardPage() {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [projects, setProjects] = useState<ProjectResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyProjectId, setBusyProjectId] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { isSubmitting }
  } = useForm<ProjectForm>({
    defaultValues: {
      title: "",
      description: ""
    }
  });

  const loadProjects = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const list = await projectApi.list();
      setProjects(list);
    } catch (err) {
      console.error(err);
      setError("Unable to load projects. Ensure the API is running on https://localhost:7302.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      loadProjects();
    }
  }, [isAuthenticated, loadProjects]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      const created = await projectApi.create({
        title: values.title,
        description: values.description || null
      });
      setProjects((prev) => [created, ...prev]);
      reset();
    } catch (err) {
      console.error(err);
      setError("Unable to create project. Try again.");
    }
  });

  const handleOpen = (project: ProjectResponse) => {
    navigate(`/projects/${project.id}`);
  };

  const handleRename = async (project: ProjectResponse) => {
    const nextTitle = window.prompt("Project title", project.title);
    if (!nextTitle || nextTitle.trim().length < 3) return;
    const nextDescription = window.prompt("Project description", project.description ?? "") ?? "";

    setBusyProjectId(project.id);
    try {
      await projectApi.update(project.id, {
        title: nextTitle.trim(),
        description: nextDescription.trim() || null
      });
      setProjects((prev) =>
        prev.map((p) =>
          p.id === project.id
            ? { ...p, title: nextTitle.trim(), description: nextDescription.trim() || null }
            : p
        )
      );
    } catch (err) {
      console.error(err);
      setError("Unable to update project.");
    } finally {
      setBusyProjectId(null);
    }
  };

  const handleDelete = async (project: ProjectResponse) => {
    if (!window.confirm(`Delete project "${project.title}" and all of its tasks?`)) return;
    setBusyProjectId(project.id);
    try {
      await projectApi.remove(project.id);
      setProjects((prev) => prev.filter((p) => p.id !== project.id));
    } catch (err) {
      console.error(err);
      setError("Unable to delete project.");
    } finally {
      setBusyProjectId(null);
    }
  };

  const completed = projects.length;

  return (
    <div className="page-container">
      <section className="card insight-banner">
        <div>
          <span className="badge neutral">Welcome back</span>
          <h2 style={{ margin: "0.75rem 0 0.45rem", fontSize: "1.9rem", letterSpacing: "-0.01em" }}>
            Build something remarkable today
          </h2>
          <p className="muted" style={{ maxWidth: "480px" }}>
            Organize projects, track progress, and let the smart scheduler suggest the next best move.
            Everything stays in sync across your team.
          </p>
        </div>
        <div className="metric-grid" role="presentation" style={{ maxWidth: "320px" }}>
          <article className="metric-card">
            <span className="label">Projects</span>
            <span className="value">{projects.length}</span>
            <span className="hint">Active workspaces</span>
          </article>
          <article className="metric-card">
            <span className="label">Completed</span>
            <span className="value">{completed}</span>
            <span className="hint">Archived in the last session</span>
          </article>
        </div>
      </section>

      <section className="card">
        <div className="page-section-header">
          <div>
            <span className="badge">New</span>
            <h2 style={{ margin: "0.6rem 0 0.4rem" }}>Create a project</h2>
            <p className="muted">Group related work and share it with your teammates in seconds.</p>
          </div>
        </div>

        {error && (
          <p role="alert" className="error-banner">
            {error}
          </p>
        )}

        <form onSubmit={onSubmit} aria-label="Create a project">
          <label>
            Title
            <input
              type="text"
              placeholder="Launch Day"
              minLength={3}
              maxLength={100}
              required
              {...register("title")}
            />
          </label>

          <label>
            Description <span className="muted">(optional)</span>
            <textarea placeholder="Quick summary, scope, notes..." maxLength={500} {...register("description")} />
          </label>

          <div className="form-actions">
            <button className="ghost-button" type="button" onClick={() => reset()} disabled={isSubmitting}>
              Reset
            </button>
            <button className="primary-button" type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creating..." : "Create project"}
            </button>
          </div>
        </form>
      </section>

      <section className="card">
        <header className="page-section-header">
          <div>
            <h2 style={{ margin: 0 }}>Projects</h2>
            <p className="muted">{projects.length} total</p>
          </div>
          <button className="ghost-button" type="button" onClick={loadProjects} disabled={loading}>
            Refresh
          </button>
        </header>

        {loading ? (
          <p className="muted">Loading projects...</p>
        ) : projects.length === 0 ? (
          <p className="muted">No projects yet. Create one above to get started.</p>
        ) : (
          <div className="grid projects">
            {projects.map((project) => (
              <article key={project.id} className="card project-card" role="listitem">
                <header>
                  <div>
                    <h3>{project.title}</h3>
                    <time className="muted" dateTime={project.createdAtUtc}>
                      Created {new Date(project.createdAtUtc).toLocaleDateString()}
                    </time>
                  </div>
                  <span className="chip">Active</span>
                </header>
                <p className="muted">{project.description || "No description provided."}</p>
                <footer>
                  <button
                    className="primary-button"
                    type="button"
                    onClick={() => handleOpen(project)}
                  >
                    Open
                  </button>
                  <button
                    className="ghost-button"
                    type="button"
                    onClick={() => handleRename(project)}
                    disabled={busyProjectId === project.id}
                  >
                    Rename
                  </button>
                  <button
                    className="danger-button"
                    type="button"
                    onClick={() => handleDelete(project)}
                    disabled={busyProjectId === project.id}
                  >
                    Delete
                  </button>
                </footer>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
