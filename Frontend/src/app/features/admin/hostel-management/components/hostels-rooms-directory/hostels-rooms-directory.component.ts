import { Component, EventEmitter, Input, Output, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TabComponent, TabItem } from '../../../../../shared/components/tab-component/tab.component';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../../shared/components/data-table/models/table-column.model';
import { HostelManagementService, HostelBuilding, HostelRoom } from '../../services/hostel-management.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { SystemSettingsService } from '../../../../../core/services/system-settings.service';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';

@Component({
  selector: 'app-hostels-rooms-directory',
  standalone: true,
  imports: [CommonModule, FormsModule, TabComponent, DataTableComponent, ActionButtonComponent],
  templateUrl: './hostels-rooms-directory.component.html',
  styleUrl: './hostels-rooms-directory.component.css',
})
export class HostelsRoomsDirectoryComponent {
  private readonly hostelService = inject(HostelManagementService);
  private readonly systemSettings = inject(SystemSettingsService, { optional: true });
  private readonly toast = inject(ToastService);

  // Reactive Input Signals for Hostels and Selected Hostel ID
  private readonly _hostels = signal<HostelBuilding[]>([]);
  private readonly _selectedHostelId = signal<number>(0);

  @Input() set hostels(val: HostelBuilding[]) {
    this._hostels.set(val || []);
  }
  get hostels(): HostelBuilding[] {
    return this._hostels();
  }

  @Input() set selectedHostelId(val: number) {
    this._selectedHostelId.set(val || 0);
  }
  get selectedHostelId(): number {
    return this._selectedHostelId();
  }

  @Input() isLoading = false;

  @Output() hostelTabChange = new EventEmitter<number>();
  @Output() createRoom = new EventEmitter<number>();
  @Output() editRoom = new EventEmitter<HostelRoom>();
  @Output() refreshed = new EventEmitter<void>();
  @Output() triggerConfirm = new EventEmitter<{ title: string; message: string; icon: string; variant: 'primary' | 'danger' | 'warning'; action: () => void }>();

  // Create Hostel Modal Signals
  public readonly isCreateHostelModalOpen = signal<boolean>(false);
  public readonly newHostelName = signal<string>('');
  public readonly isCreatingHostel = signal<boolean>(false);

  // Edit Hostel Modal Signals
  public readonly isEditHostelModalOpen = signal<boolean>(false);
  public readonly editHostelId = signal<number>(0);
  public readonly editHostelName = signal<string>('');
  public readonly isUpdatingHostel = signal<boolean>(false);

  public readonly roomColumns: TableColumn<any>[] = [
    { key: 'formattedRoomNumber', header: 'Room Number', sortable: true, filterable: true },
    { key: 'formattedMaxCapacity', header: 'Max Capacity', sortable: true, filterable: true },
    { key: 'formattedOccupancyStatus', header: 'Occupancy Status', sortable: true, filterable: true },
    { key: 'actions', header: 'Actions', sortable: false, filterable: false, type: 'actions', align: 'right' },
  ];

  public readonly roomSearchTerm = signal<string>('');
  public readonly roomCapacityFilter = signal<string>('');
  public readonly roomOccupancyFilter = signal<string>('');
  public readonly roomSortColumn = signal<string>('roomNumber');
  public readonly roomSortAsc = signal<boolean>(true);
  public readonly roomPage = signal<number>(1);
  public readonly roomPageSize = signal<number>(this.systemSettings?.defaultPageSize() || 5);
  public readonly columnFilters = signal<Record<string, string[]>>({});

  public readonly hostelBuildingTabs = computed<TabItem[]>(() => {
    return this._hostels().map((h) => ({
      id: h.id.toString(),
      label: h.name,
      icon: '🏢',
      count: h.rooms?.length || 0,
    }));
  });

  public readonly selectedHostel = computed<HostelBuilding | null>(() => {
    const list = this._hostels();
    const targetId = this._selectedHostelId();
    if (list.length === 0) return null;
    return list.find((h) => h.id === targetId) || list[0] || null;
  });

  public readonly selectedHostelTotalCapacity = computed(() => {
    const h = this.selectedHostel();
    return h?.rooms?.reduce((acc, r) => acc + (r.maxCapacity || 0), 0) || 0;
  });

  public readonly selectedHostelCurrentOccupancy = computed(() => {
    const h = this.selectedHostel();
    return h?.rooms?.reduce((acc, r) => acc + (r.currentOccupancy ?? (r as any).CurrentOccupancy ?? 0), 0) || 0;
  });

  public readonly filteredRooms = computed(() => {
    const h = this.selectedHostel();
    if (!h || !h.rooms) return [];
    let list = h.rooms;
    const search = this.roomSearchTerm().toLowerCase().trim();
    const capFilter = this.roomCapacityFilter();
    const occFilter = this.roomOccupancyFilter();

    if (search) {
      list = list.filter((r) => r.roomNumber.toLowerCase().includes(search));
    }
    if (capFilter) {
      const capNum = parseInt(capFilter, 10);
      if (capNum >= 3) {
        list = list.filter((r) => r.maxCapacity >= 3);
      } else if (capNum > 0) {
        list = list.filter((r) => r.maxCapacity === capNum);
      }
    }
    if (occFilter === 'vacant') {
      list = list.filter((r) => {
        const occ = r.currentOccupancy ?? (r as any).CurrentOccupancy ?? 0;
        return r.maxCapacity - occ > 0;
      });
    } else if (occFilter === 'full') {
      list = list.filter((r) => {
        const occ = r.currentOccupancy ?? (r as any).CurrentOccupancy ?? 0;
        return r.maxCapacity - occ <= 0;
      });
    }

    const colFilters = this.columnFilters();
    Object.keys(colFilters).forEach((col) => {
      const allowed = colFilters[col];
      if (Array.isArray(allowed) && allowed.length > 0) {
        list = list.filter((r: any) => {
          let val = '';
          if (col === 'formattedRoomNumber') val = 'Room ' + r.roomNumber;
          else if (col === 'formattedMaxCapacity') val = r.maxCapacity + ' Students';
          else if (col === 'formattedOccupancyStatus') {
            const occ = r.currentOccupancy ?? (r as any).CurrentOccupancy ?? 0;
            val = 'CAPACITY CHECKED (' + occ + ' / ' + r.maxCapacity + ')';
          }
          return allowed.includes(val);
        });
      }
    });

    const col = this.roomSortColumn();
    const asc = this.roomSortAsc();
    return [...list].sort((a: any, b: any) => {
      let valA = a[col] ?? '';
      let valB = b[col] ?? '';
      if (col === 'currentOccupancy') {
        valA = a.currentOccupancy ?? a.CurrentOccupancy ?? 0;
        valB = b.currentOccupancy ?? b.CurrentOccupancy ?? 0;
      }
      if (valA < valB) return asc ? -1 : 1;
      if (valA > valB) return asc ? 1 : -1;
      return 0;
    });
  });

  public readonly formattedRooms = computed(() => {
    const list = this.filteredRooms();
    return list.map((r) => {
      const occ = r.currentOccupancy ?? (r as any).CurrentOccupancy ?? 0;
      return {
        ...r,
        formattedRoomNumber: `Room ${r.roomNumber}`,
        formattedMaxCapacity: `${r.maxCapacity} Students`,
        formattedOccupancyStatus: `CAPACITY CHECKED (${occ} / ${r.maxCapacity})`,
      };
    });
  });

  public readonly pagedRooms = this.formattedRooms;

  onHostelTabChange(tabId: string): void {
    const id = parseInt(tabId, 10);
    this._selectedHostelId.set(id);
    this.roomPage.set(1);
    this.hostelTabChange.emit(id);
  }

  onRoomSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    let col = event.column;
    if (col === 'formattedRoomNumber') col = 'roomNumber';
    if (col === 'formattedMaxCapacity') col = 'maxCapacity';
    if (col === 'formattedOccupancyStatus') col = 'currentOccupancy';
    this.roomSortColumn.set(col);
    this.roomSortAsc.set(event.direction === 'asc');
  }

  onRoomFilterChange(filters: Record<string, string[]>): void {
    this.columnFilters.set(filters);
  }

  // Create Hostel Modal Actions
  openCreateHostelModal(): void {
    this.newHostelName.set('');
    this.isCreateHostelModalOpen.set(true);
  }

  closeCreateHostelModal(): void {
    this.isCreateHostelModalOpen.set(false);
    this.newHostelName.set('');
  }

  submitCreateHostel(): void {
    const name = this.newHostelName().trim();
    if (!name) {
      this.toast.error('Hostel building name is required.');
      return;
    }
    this.isCreatingHostel.set(true);
    this.hostelService.createHostel(name).subscribe({
      next: () => {
        this.toast.success(`Hostel building "${name}" created successfully.`);
        this.isCreatingHostel.set(false);
        this.closeCreateHostelModal();
        this.refreshed.emit();
      },
      error: (err: any) => {
        this.toast.error(err?.error?.message || 'Failed to create hostel.');
        this.isCreatingHostel.set(false);
      },
    });
  }

  // Edit Hostel Modal Actions
  openEditHostelModal(hostel: HostelBuilding): void {
    this.editHostelId.set(hostel.id);
    this.editHostelName.set(hostel.name);
    this.isEditHostelModalOpen.set(true);
  }

  closeEditHostelModal(): void {
    this.isEditHostelModalOpen.set(false);
  }

  submitEditHostel(): void {
    const id = this.editHostelId();
    const name = this.editHostelName().trim();
    if (!id || !name) {
      this.toast.error('Please enter a valid hostel building name.');
      return;
    }
    this.isUpdatingHostel.set(true);
    this.hostelService.updateHostel(id, name).subscribe({
      next: () => {
        this.toast.success(`Hostel updated to "${name}" successfully.`);
        this.isUpdatingHostel.set(false);
        this.closeEditHostelModal();
        this.refreshed.emit();
      },
      error: (err: any) => {
        this.toast.error(err?.error?.message || 'Failed to update hostel.');
        this.isUpdatingHostel.set(false);
      },
    });
  }

  // Deactivate Hostel Action
  deactivateHostel(hostel: HostelBuilding): void {
    this.triggerConfirm.emit({
      title: 'Deactivate Hostel Building',
      message: `Are you sure you want to deactivate ${hostel.name}? Associated rooms will be locked for allocations.`,
      icon: '🏢',
      variant: 'danger',
      action: () => {
        this.hostelService.deleteHostel(hostel.id).subscribe({
          next: () => {
            this.toast.success(`Hostel "${hostel.name}" deactivated.`);
            this.refreshed.emit();
          },
          error: (err: any) => this.toast.error(err?.error?.message || 'Failed to deactivate hostel.'),
        });
      },
    });
  }

  // Deactivate Room Action
  deactivateRoom(room: HostelRoom): void {
    this.triggerConfirm.emit({
      title: 'Deactivate Room',
      message: `Are you sure you want to deactivate Room ${room.roomNumber}?`,
      icon: '🚪',
      variant: 'danger',
      action: () => {
        this.hostelService.deleteRoom(room.id).subscribe({
          next: () => {
            this.toast.success(`Room ${room.roomNumber} deactivated.`);
            this.refreshed.emit();
          },
          error: (err: any) => this.toast.error(err?.error?.message || 'Failed to deactivate room.'),
        });
      },
    });
  }

  // Reactivate Room Action
  reactivateRoom(room: HostelRoom): void {
    this.triggerConfirm.emit({
      title: 'Reactivate Room',
      message: `Are you sure you want to reactivate Room ${room.roomNumber}?`,
      icon: '🔄',
      variant: 'primary',
      action: () => {
        this.hostelService.updateRoom(room.id, room.roomNumber, room.maxCapacity, true).subscribe({
          next: () => {
            this.toast.success(`Room ${room.roomNumber} reactivated successfully.`);
            this.refreshed.emit();
          },
          error: (err: any) => this.toast.error(err?.error?.message || 'Failed to reactivate room.'),
        });
      },
    });
  }
}
