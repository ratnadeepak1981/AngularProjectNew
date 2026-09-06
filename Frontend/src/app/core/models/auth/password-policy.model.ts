export interface PasswordPolicy {
  minLength: number;
  complexityTier: 'basic' | 'medium' | 'strong' | 'strict' | string;
  expiryDays: number;
  reuseHistoryLimit: number;
  otpValidityMinutes: number;
  maxFailedLogins: number;
  lockoutDurationMinutes: number;
}
