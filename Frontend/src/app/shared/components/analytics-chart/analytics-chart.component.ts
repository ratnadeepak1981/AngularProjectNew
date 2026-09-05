import {
  Component,
  input,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartConfig, ChartDataPoint, ChartLegendItem } from './models/chart-data.model';

/** Internal bar item with computed heights */
interface BarItem {
  label: string;
  value: number;
  value2: number;
  primaryHeight: number;
  secondaryHeight: number;
  tooltip: string;
  color: string;
}

/** Internal line/area point */
interface LinePoint {
  x: number;
  y: number;
  label: string;
  value: number;
  displayValue: string;
}

/** Internal donut segment */
interface DonutSegment {
  label: string;
  value: number;
  percentage: number;
  color: string;
  strokeDasharray: string;
  strokeDashoffset: number;
}

/** SVG viewBox constants */
const SVG_W = 500;
const SVG_H = 140;
const PAD_X = 44;
const PAD_Y = 16;
const DONUT_R = 38;
const CIRCUMFERENCE = 2 * Math.PI * DONUT_R; // 238.76

/** Default palette — cycles when data has more items than colors */
const DEFAULT_PALETTE = [
  '#3b82f6', '#10b981', '#f59e0b', '#8b5cf6',
  '#ec4899', '#06b6d4', '#6366f1', '#14b8a6',
  '#ef4444', '#84cc16',
];

@Component({
  selector: 'app-analytics-chart',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './analytics-chart.component.html',
})
export class AnalyticsChartComponent {
  // ─── Inputs ───────────────────────────────────────────────────────────────
  readonly data   = input.required<ChartDataPoint[]>();
  readonly config = input.required<ChartConfig>();

  // ─── Derived Computeds ────────────────────────────────────────────────────

  /** Resolved legend items */
  readonly legendItems = computed<ChartLegendItem[]>(() => {
    const cfg = this.config();
    if (cfg.legendItems?.length) return cfg.legendItems;
    if (cfg.type === 'bar') {
      const items: ChartLegendItem[] = [
        { label: cfg.centerLabel ?? 'Primary', color: cfg.primaryColor },
      ];
      if (cfg.secondaryColor) {
        items.push({ label: cfg.centerSub ?? 'Secondary', color: cfg.secondaryColor });
      }
      return items;
    }
    return [];
  });

  // ─── BAR CHART COMPUTED ────────────────────────────────────────────────────
  readonly barItems = computed<BarItem[]>(() => {
    const pts = this.data();
    const cfg = this.config();
    if (!pts.length) return [];
    const maxVal = Math.max(1, ...pts.map(p => p.value));
    return pts.map((p, idx) => ({
      label: p.label,
      value: p.value,
      value2: p.value2 ?? 0,
      primaryHeight: Math.round((p.value / maxVal) * 100),
      secondaryHeight: p.value2 != null
        ? (cfg.stacked
          ? Math.round((p.value2 / p.value) * 100)          // stacked: % of bar
          : Math.round((p.value2 / maxVal) * 100))           // dual: own height
        : 0,
      tooltip: p.tooltip ?? `${p.label}: ${p.value}`,
      color: p.color ?? cfg.primaryColor ?? DEFAULT_PALETTE[idx % DEFAULT_PALETTE.length],
    }));
  });

  /** Bar chart: formatted top label */
  formatBarLabel(item: BarItem): string {
    const cfg = this.config();
    const val = this.fmt(item.value, cfg);
    return item.value2 > 0 ? `${val}` : val;
  }

  // ─── LINE CHART COMPUTED ───────────────────────────────────────────────────
  readonly linePoints = computed<LinePoint[]>(() => {
    const pts = this.data();
    const cfg = this.config();
    if (!pts.length) return [];
    const usableW = SVG_W - PAD_X * 2;
    const usableH = SVG_H - PAD_Y * 2;
    const maxVal = Math.max(1, ...pts.map(p => p.value));
    const stepX = pts.length > 1 ? usableW / (pts.length - 1) : usableW / 2;
    return pts.map((p, i) => {
      const x = pts.length > 1 ? PAD_X + i * stepX : SVG_W / 2;
      const y = SVG_H - PAD_Y - (p.value / maxVal) * usableH;
      return {
        x, y,
        label: p.label,
        value: p.value,
        displayValue: this.fmt(p.value, cfg),
      };
    });
  });

  readonly linePath = computed<string>(() => {
    const pts = this.linePoints();
    if (!pts.length) return '';
    return pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
  });

  readonly lineAreaPath = computed<string>(() => {
    const pts = this.linePoints();
    if (!pts.length) return '';
    const path = this.linePath();
    const last = pts[pts.length - 1];
    const first = pts[0];
    return `${path} L ${last.x.toFixed(1)} ${SVG_H - PAD_Y} L ${first.x.toFixed(1)} ${SVG_H - PAD_Y} Z`;
  });

  readonly lineGradId = computed(() =>
    `lgrd-${this.config().title.replace(/\s+/g, '').toLowerCase().substring(0, 8)}`
  );

  // ─── DONUT CHART COMPUTED ─────────────────────────────────────────────────
  readonly donutSegments = computed<DonutSegment[]>(() => {
    const pts = this.data();
    const cfg = this.config();

    // Gauge mode: single arc from gaugePercent
    if (cfg.gaugePercent !== undefined) {
      const dash = (cfg.gaugePercent / 100) * CIRCUMFERENCE;
      return [{
        label: cfg.centerLabel ?? `${cfg.gaugePercent}%`,
        value: cfg.gaugePercent,
        percentage: cfg.gaugePercent,
        color: cfg.primaryColor,
        strokeDasharray: `${dash} ${CIRCUMFERENCE}`,
        strokeDashoffset: 0,
      }];
    }

    // Segmented donut from data
    const total = pts.reduce((s, p) => s + (p.value ?? 0), 0);
    if (total === 0) return [];
    let offset = 0;
    return pts.map((p, idx) => {
      const pct = p.value / total;
      const dash = pct * CIRCUMFERENCE;
      const seg: DonutSegment = {
        label: p.label,
        value: p.value,
        percentage: Math.round(pct * 100),
        color: p.color ?? DEFAULT_PALETTE[idx % DEFAULT_PALETTE.length],
        strokeDasharray: `${dash.toFixed(2)} ${CIRCUMFERENCE}`,
        strokeDashoffset: offset,
      };
      offset -= dash;
      return seg;
    });
  });

  readonly donutCenterLabel = computed(() => {
    const cfg = this.config();
    if (cfg.centerLabel) return cfg.centerLabel;
    if (cfg.gaugePercent !== undefined) return `${cfg.gaugePercent}%`;
    const total = this.data().reduce((s, p) => s + (p.value ?? 0), 0);
    return this.fmt(total, cfg);
  });

  readonly donutCenterSub = computed(() => this.config().centerSub ?? '');

  // ─── Helpers ──────────────────────────────────────────────────────────────
  private fmt(v: number, cfg: ChartConfig): string {
    const pre = cfg.valuePrefix ?? '';
    const suf = cfg.valueSuffix ?? '';
    return `${pre}${v}${suf}`;
  }

  fmtLabel(item: BarItem): string {
    return this.fmt(item.value, this.config());
  }

  /** Track-by for @for */
  trackIdx(_: number, __: unknown) { return _; }

  /** Expose CIRCUMFERENCE to template */
  readonly circumference = CIRCUMFERENCE;
  readonly svgW = SVG_W;
  readonly svgH = SVG_H;
}
