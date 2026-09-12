export interface AdminUser {
  id: number;
  email: string;
  fullName?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastPasswordChangedAt?: string;
}

export interface CreateAdminRequest {
  email: string;
  fullName?: string;
  password: string;
}

export interface ToggleAdminStatusRequest {
  isActive: boolean;
}
