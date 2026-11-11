import type { NextConfig } from "next";
import { PHASE_DEVELOPMENT_SERVER } from "next/constants";

// Під час розробки запити /api/* проксуються на сервер ASP.NET Core (одне джерело — спільні cookie сесії).
// Для випуску клієнт збирається як статичний експорт (out/) і роздається тим самим сервером із wwwroot.
export default function config(phase: string): NextConfig {
  if (phase === PHASE_DEVELOPMENT_SERVER) {
    const backend = process.env.BACKEND_URL ?? "http://localhost:5080";
    return {
      async rewrites() {
        return [{ source: "/api/:path*", destination: `${backend}/api/:path*` }];
      },
    };
  }
  return { output: "export", trailingSlash: true };
}
