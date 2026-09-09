import { LabTimeSlot } from './lab-time-slot.model';

export interface Lab {
  id: number;
  name: string;
  labType: 'Computer' | 'Science' | string;
  capacity: number;
  isActive?: boolean;
  totalRows?: number;
  totalColumns?: number;
  requiresSeatSelection?: boolean;
  seatsCount?: number;
  seatsBuilt?: number;
  timeSlots?: LabTimeSlot[];
}

