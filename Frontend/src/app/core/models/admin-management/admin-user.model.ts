export interface AdminUser {
  id: number;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastPasswordChangedAt?: string;
}

export interface CreateAdminRequest {
  email: string;
  password: string;
}

export interface ToggleAdminStatusRequest {
  isActive: boolean;
}
