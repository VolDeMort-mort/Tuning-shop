"use client";

import { useCallback } from "react";
import { useLoad } from "@/components/useLoad";
import { api } from "@/lib/api";
import type { Employee } from "@/lib/types";

/** Перелік працівників із роллю «Майстер» (FR-08); доступний Менеджеру. */
export function useMasters(enabled: boolean): Employee[] {
  const load = useCallback(() => api.get<Employee[]>("/api/employees/masters"), []);
  return useLoad(enabled ? load : null).data ?? [];
}
