import type { Metadata } from "next";
import { AppShell } from "@/components/AppShell";
import { AuthProvider } from "@/components/auth";
import "./globals.css";

export const metadata: Metadata = {
  title: "Тюнінг-ательє — управління замовленнями",
  description: "Інформаційна система управління замовленнями тюнінг-ательє, v1.0",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="uk" className="h-full antialiased">
      <body className="min-h-full">
        <AuthProvider>
          <AppShell>{children}</AppShell>
        </AuthProvider>
      </body>
    </html>
  );
}
