// Типи повідомлень програмного інтерфейсу; відповідають backend/src/TuningShop.Api/Contracts/Contracts.cs.

export type Role = "Manager" | "Master" | "Director";
export type OrderStatus = "New" | "InProgress" | "Ready" | "Issued" | "Cancelled";
export type WorkStatus = "Planned" | "InProgress" | "Done";
export type PartStatus = "Awaiting" | "Received";
export type WorkCategory =
  | "BodyKit"
  | "Suspension"
  | "Exhaust"
  | "Lighting"
  | "ChipTuning"
  | "Detailing"
  | "Other";

export interface ApiErrorBody {
  code: string;
  message: string;
  fields?: Record<string, string> | null;
  details?: string[] | null;
}

export interface CurrentUser {
  id: number;
  fullName: string;
  login: string;
  role: Role;
}

export interface Employee {
  id: number;
  fullName: string;
  role: Role;
}

export interface Client {
  id: number;
  fullName: string;
  phone: string;
  email: string | null;
}

export interface Car {
  id: number;
  clientId: number;
  make: string;
  model: string;
  year: number;
  vin: string;
  plateNumber: string;
}

export interface OrderSummary {
  id: number;
  number: string;
  createdAt: string;
  status: OrderStatus;
  statusName: string;
  carId: number;
  carTitle: string;
  plateNumber: string;
  clientId: number;
  clientName: string;
  doneCount: number;
  workCount: number;
  progress: string;
  allWorksDone: boolean;
  issuedAt: string | null;
  cancelReason: string | null;
}

export interface ClientDetails extends Client {
  cars: Car[];
  orders: OrderSummary[];
}

export interface CarDetails {
  car: Car;
  owner: Client;
  activeOrderId: number | null;
  activeOrderNumber: string | null;
  orders: OrderSummary[];
}

export interface Part {
  id: number;
  name: string;
  quantity: number;
  status: PartStatus;
  statusName: string;
  expectedAt: string | null;
  receivedAt: string | null;
}

export interface WorkItem {
  id: number;
  category: WorkCategory;
  description: string;
  estimatedCost: number;
  status: WorkStatus;
  statusName: string;
  statusChangedAt: string;
  masterId: number | null;
  masterName: string | null;
  allPartsReceived: boolean;
  parts: Part[];
}

export interface StatusChange {
  changedAt: string;
  fromStatus: OrderStatus | null;
  fromName: string | null;
  toStatus: OrderStatus;
  toName: string;
  employeeName: string;
  comment: string | null;
}

export interface OrderDetails {
  id: number;
  number: string;
  createdAt: string;
  clientRequest: string;
  status: OrderStatus;
  statusName: string;
  issuedAt: string | null;
  cancelReason: string | null;
  client: Client;
  car: Car;
  works: WorkItem[];
  doneCount: number;
  workCount: number;
  progress: string;
  allWorksDone: boolean;
  totalEstimate: number;
  canEditWorks: boolean;
  allowedActions: ("startWork" | "markReady" | "issue" | "cancel")[];
  history: StatusChange[];
}

export interface MasterWork {
  workId: number;
  orderId: number;
  orderNumber: string;
  carTitle: string;
  plateNumber: string;
  category: WorkCategory;
  description: string;
  status: WorkStatus;
  statusName: string;
  statusChangedAt: string;
  orderProgress: string;
  parts: Part[];
}

export interface Tracking {
  number: string;
  carTitle: string;
  plateNumber: string;
  status: OrderStatus;
  statusName: string;
  doneCount: number;
  workCount: number;
  progress: string;
  works: {
    category: WorkCategory;
    description: string;
    status: WorkStatus;
    statusName: string;
    parts: Omit<Part, "id">[];
  }[];
}

export interface WorkInput {
  category: WorkCategory | "";
  description: string;
  estimatedCost: string;
  masterId: string;
}
