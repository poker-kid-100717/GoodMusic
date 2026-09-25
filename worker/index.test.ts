import { describe, expect, it, vi } from "vitest";

// The real class extends a Workers-runtime Durable Object; only the
// request forwarding is under test here.
vi.mock("@cloudflare/containers", () => ({ Container: class {} }));

const { default: worker } = await import("./index");

function createEnv() {
  const forwarded: Request[] = [];
  const env = {
    API: {
      getByName: vi.fn(() => ({
        fetch: async (req: Request) => {
          forwarded.push(req);
          return new Response("ok");
        },
      })),
    },
    MONGODB_URI: "mongodb+srv://example",
    JWT_KEY: "key",
  };
  return { env: env as any, forwarded };
}

describe("worker", () => {
  it.each(["/", "/swagger/v1/swagger.json", "/api/Artist", "/health"])("forwards %s to the API container", async (path) => {
    const { env, forwarded } = createEnv();

    await worker.fetch(new Request(`https://music.example.com${path}`), env);

    expect(forwarded).toHaveLength(1);
    expect(new URL(forwarded[0].url).pathname).toBe(path);
    expect(env.API.getByName).toHaveBeenCalledWith("api");
  });

  it("keeps method, body and Authorization, and adds forwarded headers", async () => {
    const { env, forwarded } = createEnv();

    await worker.fetch(
      new Request("https://music.example.com/api/Artist", {
        method: "POST",
        headers: { Authorization: "Bearer abc", "CF-Connecting-IP": "203.0.113.7", "content-type": "application/json" },
        body: JSON.stringify({ name: "Queen" }),
      }),
      env,
    );

    const req = forwarded[0];
    expect(req.method).toBe("POST");
    expect(await req.json()).toEqual({ name: "Queen" });
    expect(req.headers.get("Authorization")).toBe("Bearer abc");
    expect(req.headers.get("X-Forwarded-Proto")).toBe("https");
    expect(req.headers.get("X-Forwarded-For")).toBe("203.0.113.7");
  });
});
