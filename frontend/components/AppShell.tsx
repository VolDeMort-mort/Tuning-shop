"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { useAuth } from "@/components/auth";
import { Loading } from "@/components/ui";
import { roleNames } from "@/lib/format";
import type { Role } from "@/lib/types";

const publicPaths = ["/login/", "/track/"];

/** Розділи, доступні ролі (FR-01). Сервер перевіряє роль для кожного запиту незалежно від цього меню. */
const sections: { href: string; label: string; roles: Role[] }[] = [
  { href: "/orders/", label: "Поточні замовлення", roles: ["Manager", "Director"] },
  { href: "/orders/completed/", label: "Завершені", roles: ["Manager", "Director"] },
  { href: "/orders/new/", label: "Оформити замовлення", roles: ["Manager"] },
  { href: "/clients/", label: "Клієнти й автомобілі", roles: ["Manager", "Director"] },
  { href: "/master/", label: "Мої роботи", roles: ["Master"] },
];

function normalize(path: string) {
  return path.endsWith("/") ? path : path + "/";
}

function allowed(path: string, role: Role): boolean {
  if (path === "/") return true;
  if (path.startsWith("/master/")) return role === "Master";
  if (path.startsWith("/orders/new/")) return role === "Manager";
  return role !== "Master";
}

export function AppShell({ children }: { children: ReactNode }) {
  const { user, loading, logout } = useAuth();
  const pathname = normalize(usePathname());
  const router = useRouter();
  const isPublic = publicPaths.includes(pathname);

  useEffect(() => {
    if (!loading && !user && !isPublic) router.replace("/login/");
  }, [loading, user, isPublic, router]);

  return (
    <div className="min-h-full bg-zinc-50">
      <header className="border-b border-zinc-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-5 gap-y-2 px-4 py-3">
          <Link href={user ? "/" : "/login/"} className="font-semibold">
            Тюнінг-ательє
          </Link>
          {user && (
            <nav className="flex flex-wrap gap-3 text-sm">
              {sections
                .filter((s) => s.roles.includes(user.role))
                .map((s) => (
                  <Link
                    key={s.href}
                    href={s.href}
                    className={pathname === s.href ? "font-semibold text-zinc-900" : "text-zinc-600 hover:text-zinc-900"}
                  >
                    {s.label}
                  </Link>
                ))}
            </nav>
          )}
          <div className="ml-auto flex items-center gap-3 text-sm">
            {user ? (
              <>
                <span className="text-zinc-600">
                  {user.fullName} · {roleNames[user.role]}
                </span>
                <button
                  className="text-zinc-600 underline hover:text-zinc-900"
                  onClick={async () => {
                    await logout();
                    router.replace("/login/");
                  }}
                >
                  Вийти
                </button>
              </>
            ) : (
              <Link href="/track/" className="text-zinc-600 hover:text-zinc-900">
                Відстежити замовлення
              </Link>
            )}
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        {isPublic ? (
          children
        ) : loading || !user ? (
          <Loading />
        ) : allowed(pathname, user.role) ? (
          children
        ) : (
          <p className="text-sm text-rose-700">Ця функція недоступна для вашої ролі.</p>
        )}
      </main>
    </div>
  );
}
