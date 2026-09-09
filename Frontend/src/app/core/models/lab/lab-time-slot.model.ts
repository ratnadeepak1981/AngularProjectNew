export interface LabTimeSlot {
  id: number;
  labId: number;
  labName?: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
  displayOrder: number;
  isAvailable?: boolean;
}

export interface CreateLabTimeSlotDto {
  labId: number;
  startTime: string;
  endTime: string;
  displayOrder: number;
}

export interface UpdateLabTimeSlotDto {
  startTime: string;
  endTime: string;
  isActive: boolean;
  displayOrder: number;
}
