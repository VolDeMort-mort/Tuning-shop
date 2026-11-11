import type { OrderStatus, PartStatus, WorkCategory, WorkStatus } from "./types";

export const categoryNames: Record<WorkCategory, string> = {
  BodyKit: "Обвіс",
  Suspension: "Підвіска",
  Exhaust: "Вихлоп",
  Lighting: "Освітлення",
  ChipTuning: "Чип-тюнінг",
  Detailing: "Детейлінг",
  Other: "Інше",
};

export const orderStatusNames: Record<OrderStatus, string> = {
  New: "Нове",
  InProgress: "В роботі",
  Ready: "Готове до видачі",
  Issued: "Видане",
  Cancelled: "Скасоване",
};

export const roleNames = { Manager: "Менеджер", Master: "Майстер", Director: "Керівник" } as const;

const dateTime = new Intl.DateTimeFormat("uk-UA", { dateStyle: "short", timeStyle: "short" });
const dateOnly = new Intl.DateTimeFormat("uk-UA", { dateStyle: "short" });
const money = new Intl.NumberFormat("uk-UA", { style: "currency", currency: "UAH", maximumFractionDigits: 2 });

export const formatDateTime = (iso: string | null) => (iso ? dateTime.format(new Date(iso)) : "—");
export const formatDate = (iso: string | null) => (iso ? dateOnly.format(new Date(iso)) : "—");
export const formatMoney = (value: number) => money.format(value);

export function statusTone(status: OrderStatus | WorkStatus | PartStatus): string {
  switch (status) {
    case "New":
    case "Planned":
    case "Awaiting":
      return "bg-sky-100 text-sky-800";
    case "InProgress":
      return "bg-amber-100 text-amber-800";
    case "Ready":
    case "Done":
    case "Received":
      return "bg-emerald-100 text-emerald-800";
    case "Issued":
      return "bg-zinc-200 text-zinc-700";
    case "Cancelled":
      return "bg-rose-100 text-rose-800";
  }
}
