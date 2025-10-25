import axios from "axios";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "/api/v1";

export interface TaskDto {
  id: string;
  description: string;
  isCompleted: boolean;
  createdAtUtc: string;
}

export interface PaginatedTaskResponse {
  total: number;
  page: number;
  pageSize: number;
  items: TaskDto[];
}

const client = axios.create({
  baseURL: API_BASE,
  headers: {
    "Content-Type": "application/json"
  }
});

export async function listTasks(params?: {
  status?: "all" | "active" | "completed";
  search?: string;
}) {
  const response = await client.get<PaginatedTaskResponse>("/tasks", {
    params: {
      status: params?.status,
      search: params?.search,
      pageSize: 200
    }
  });
  return response.data.items;
}

export async function createTask(description: string) {
  const response = await client.post<TaskDto>("/tasks", { description });
  return response.data;
}

export async function updateTask(id: string, payload: { description: string; isCompleted: boolean }) {
  await client.put(`/tasks/${id}`, payload);
}

export async function toggleTask(id: string) {
  await client.patch(`/tasks/${id}/toggle`);
}

export async function deleteTask(id: string) {
  await client.delete(`/tasks/${id}`);
}
