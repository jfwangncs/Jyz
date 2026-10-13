import { requestClient } from '#/api/request';

export interface QueryParams {
  pageIndex?: number;
  pageSize?: number;
  searchKey?: string;
  status?: number;
  startDate?: string;
  endDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
}

// ========== User ==========

export interface UserRecord {
  id: number;
  userName: string;
  nickName?: string;
  realName?: string;
  phone?: string;
  avatar?: string;
  gender?: number;
  province?: string;
  city?: string;
  country?: string;
  lastLoginTime: string;
  createdTime: string;
  status: number;
}

export interface CreateUserParams {
  userName: string;
  password: string;
  nickName?: string;
  realName?: string;
  phone?: string;
}

export interface UpdateUserParams {
  nickName?: string;
  realName?: string;
  phone?: string;
  avatar?: string;
  gender?: number;
  province?: string;
  city?: string;
  country?: string;
}

export async function getUserListApi(params: QueryParams) {
  return requestClient.get<PagedResult<UserRecord>>('/user', { params });
}

export async function createUserApi(data: CreateUserParams) {
  return requestClient.post('/user', data);
}

export async function updateUserApi(id: number, data: UpdateUserParams) {
  return requestClient.put(`/user/${id}`, data);
}

export async function deleteUserApi(id: number) {
  return requestClient.delete(`/user/${id}`);
}

export async function toggleUserStatusApi(id: number) {
  return requestClient.put(`/user/${id}/status`);
}
