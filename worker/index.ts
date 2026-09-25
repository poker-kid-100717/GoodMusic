import { Container } from "@cloudflare/containers";

import nextHandler from "./next-handler";

export interface Env {
  API: DurableObjectNamespace<GoodMusicApi>;
  ASSETS: Fetcher;
  // Worker secrets (see README "Deployment").
  MONGODB_URI: string;
  JWT_KEY: string;
}

/**
 * The ASP.NET Core API as a Cloudflare Container. It sleeps after 10
 * minutes idle and starts on the next request; MongoDB Atlas holds all
 * state, so the container is disposable.
 */
export class GoodMusicApi extends Container<Env> {
  defaultPort = 8080;
  sleepAfter = "10m";
  pingEndpoint = "localhost/health";

  constructor(ctx: DurableObjectState<{}>, env: Env) {
    super(ctx, env);
    this.envVars = {
      ASPNETCORE_ENVIRONMENT: "Production",
      MongoDB__ConnectionString: env.MONGODB_URI,
      Jwt__Key: env.JWT_KEY,
    };
  }
}

/** Paths the API answers directly: the REST API, its docs, and the health check. */
export function isApiPath(pathname: string): boolean {
  return (
    pathname.startsWith("/api/") ||
    pathname === "/health" ||
    pathname === "/swagger" ||
    pathname.startsWith("/swagger/")
  );
}

function forwardToApi(request: Request, env: Env): Promise<Response> {
  const url = new URL(request.url);

  // TLS ends here, so tell the API the original scheme and client IP.
  const headers = new Headers(request.headers);
  headers.set("X-Forwarded-Proto", url.protocol.slice(0, -1));
  const clientIp = request.headers.get("CF-Connecting-IP");
  if (clientIp) headers.set("X-Forwarded-For", clientIp);

  // One API instance is plenty for this catalog.
  return env.API.getByName("api").fetch(new Request(request, { headers }));
}

export default {
  async fetch(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
    const { pathname } = new URL(request.url);
    if (isApiPath(pathname)) {
      return forwardToApi(request, env);
    }
    // Everything else is the Next.js site. Its server components reach the
    // API through the same API binding, without leaving Cloudflare.
    return nextHandler.fetch(request, env, ctx);
  },
} satisfies ExportedHandler<Env>;
