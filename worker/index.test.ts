import { describe, expect, it, vi } from "vitest";

// The real class extends a Workers-runtime Durable Object; only the
// request routing is under test here.
vi.mock("@cloudflare/containers", () => ({ Container: class {} }));

// The Next.js handler is a build output; stand in for it.
const nextFetch = vi.fn(async () => new Response("next"));
vi.mock("./next-handler", () => ({ default: { fetch: nextFetch } }));

const { default: worker, isApiPath } = await import("./index");

function createEnv() {
  const forwarded: Request[] = [];
  const env = {
    API: {
      getByName: vi.fn(() => ({
        fetch: async (req: Request) => {
          forwarded.push(req);
          return new Response("api");
        },
      })),
    },
    MONGODB_URI: "mongodb+srv://example",
    JWT_KEY: "key",
  };
  return { env: env as any, forwarded };
}

const ctx = {} as ExecutionContext;

describe("worker", () => {
  it.each(["/api/Artist", "/api/User/me", "/health", "/swagger", "/swagger/index.html", "/swagger/v1/swagger.json"])(
    "forwards %s to the API container",
    async (path) => {
      const { env, forwarded } = createEnv();

      const response = await worker.fetch(new Request(`https://music.example.com${path}`), env, ctx);

      expect(await response.text()).toBe("api");
      expect(forwarded).toHaveLength(1);
      expect(new URL(forwarded[0].url).pathname).toBe(path);
      expect(env.API.getByName).toHaveBeenCalledWith("api");
    },
  );

  it.each(["/", "/artists", "/artists/abc", "/songs?q=nude", "/login", "/apis", "/swaggerish"])(
    "serves %s from the Next.js app",
    async (path) => {
      const { env, forwarded } = createEnv();
      nextFetch.mockClear();

      const response = await worker.fetch(new Request(`https://music.example.com${path}`), env, ctx);

      expect(await response.text()).toBe("next");
      expect(nextFetch).toHaveBeenCalledTimes(1);
      expect(forwarded).toHaveLength(0);
    },
  );

  it("keeps method, body and Authorization, and adds forwarded headers", async () => {
    const { env, forwarded } = createEnv();

    await worker.fetch(
      new Request("https://music.example.com/api/Artist", {
        method: "POST",
        headers: { Authorization: "Bearer abc", "CF-Connecting-IP": "203.0.113.7", "content-type": "application/json" },
        body: JSON.stringify({ name: "Queen" }),
      }),
      env,
      ctx,
    );

    const req = forwarded[0];
    expect(req.method).toBe("POST");
    expect(await req.json()).toEqual({ name: "Queen" });
    expect(req.headers.get("Authorization")).toBe("Bearer abc");
    expect(req.headers.get("X-Forwarded-Proto")).toBe("https");
    expect(req.headers.get("X-Forwarded-For")).toBe("203.0.113.7");
  });

  it("matches API paths exactly, not by prefix", () => {
    expect(isApiPath("/api/Music")).toBe(true);
    expect(isApiPath("/api")).toBe(false);
    expect(isApiPath("/healthy")).toBe(false);
  });
});
