/**
 * chart-data.model.ts
 * Data interfaces for the reusable AnalyticsChartComponent.
 * Supports Bar, Line, and Donut chart types.
 * Angular 22+ — zero third-party dependencies.
 */

/** One data point fed into any chart type */
export interface ChartDataPoint {
  /** X-axis label (bar/line) or segment name (donut) */
  label: string;
  /** Primary value — bar height, line Y, or donut slice size */
  value: number;
  /** Optional secondary value — used for dual-bar charts */
  value2?: number;
  /** Optional per-segment color override (donut) */
  color?: string;
  /** Optional tooltip/title text override */
  tooltip?: string;
}

/** Chart type discriminator */
export type ChartType = 'bar' | 'line' | 'donut';

/** Legend item */
export interface ChartLegendItem {
  label: string;
  color: string;
}

/** Full configuration object passed to AnalyticsChartComponent */
export interface ChartConfig {
  /** Chart rendering type */
  type: ChartType;
  /** Card header title */
  title: string;
  /** Emoji icon shown in card header */
  icon?: string;
  /** Short subtitle shown below the chart */
  subtitle?: string;
  /** Primary color (hex) */
  primaryColor: string;
  /** Secondary color (hex) — for dual-bar value2 column */
  secondaryColor?: string;
  /** Value suffix e.g. '%' */
  valueSuffix?: string;
  /** Value prefix e.g. 'Rs.' */
  valuePrefix?: string;
  /** Legend items shown below chart */
  legendItems?: ChartLegendItem[];
  /** Donut center label */
  centerLabel?: string;
  /** Donut center sublabel */
  centerSub?: string;
  /** Donut gauge value (0-100) — if set, renders as a radial gauge */
  gaugePercent?: number;
  /** If true, bar value2 stacks inside value bar instead of side-by-side */
  stacked?: boolean;
}
