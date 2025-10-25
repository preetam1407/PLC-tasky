import axios from "axios";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "/api/v1";

export interface AuthResponse {
  token: string;
}

export interface ProjectResponse {
  id: string;
  title: string;
  description: string | null;
  createdAtUtc: string;
}

export interface TaskResponse {
  id: string;
  title: string;
  dueDate: string | null;
  isCompleted: boolean;
  createdAtUtc: string;
}

export interface ProjectDetailResponse extends ProjectResponse {
  tasks: TaskResponse[];
}

export interface ScheduleInput {
  startDate?: string;
  endDate?: string;
  dailyCapacity?: number;
  workingDays?: string[];
}

export interface DayPlan {
  date: string;
  taskIds: string[];
}

export interface ScheduleResponse {
  projectId: string;
  generatedAtUtc: string;
  days: DayPlan[];
}

export const api = axios.create({
  baseURL: API_BASE,
  headers: {
    "Content-Type": "application/json"
  }
});

export const authApi = {
  register: (email: string, password: string) =>
    api.post<void>("/auth/register", { email, password }),
  login: (email: string, password: string) =>
    api.post<AuthResponse>("/auth/login", { email, password }).then((res) => res.data)
};

export const projectApi = {
  list: () => api.get<ProjectResponse[]>("/projects").then((res) => res.data),
  create: (payload: { title: string; description?: string | null }) =>
    api.post<ProjectResponse>("/projects", payload).then((res) => res.data),
  update: (projectId: string, payload: { title: string; description?: string | null }) =>
    api.put<void>(`/projects/${projectId}`, payload),
  remove: (projectId: string) => api.delete<void>(`/projects/${projectId}`),
  get: (projectId: string) =>
    api.get<ProjectDetailResponse>(`/projects/${projectId}`).then((res) => res.data),
  schedule: (projectId: string, payload: ScheduleInput) =>
    api.post<ScheduleResponse>(`/projects/${projectId}/schedule`, payload).then((res) => res.data)
};

export const taskApi = {
  create: (projectId: string, payload: { title: string; dueDate?: string | null }) =>
    api.post<TaskResponse>(`/projects/${projectId}/tasks`, payload).then((res) => res.data),
  update: (taskId: string, payload: { title: string; dueDate?: string | null; isCompleted: boolean }) =>
    api.put<void>(`/tasks/${taskId}`, payload),
  toggle: (taskId: string) => api.patch<void>(`/tasks/${taskId}/toggle`),
  remove: (taskId: string) => api.delete<void>(`/tasks/${taskId}`)
};
