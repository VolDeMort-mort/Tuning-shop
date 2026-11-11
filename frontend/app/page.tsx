"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { homeFor, useAuth } from "@/components/auth";
import { Loading } from "@/components/ui";

/** Початкова сторінка перенаправляє працівника до розділу його ролі. */
export default function Home() {
  const { user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (user) router.replace(homeFor(user));
  }, [user, router]);

  return <Loading />;
}
