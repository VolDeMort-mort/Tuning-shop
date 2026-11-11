"use client";

import { useCallback, useEffect, useState } from "react";

/** Завантаження даних сторінки з повторним читанням після змін. */
export function useLoad<T>(load: (() => Promise<T>) | null) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<unknown>(null);

  const reload = useCallback(async () => {
    if (!load) return;
    try {
      setData(await load());
      setError(null);
    } catch (err) {
      setError(err);
    }
  }, [load]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- завантаження даних під час відкриття сторінки
    reload();
  }, [reload]);

  return { data, setData, error, reload };
}
