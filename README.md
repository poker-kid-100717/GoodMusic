# GoodMusic

[![CI](https://github.com/poker-kid-100717/GoodMusic/actions/workflows/ci.yml/badge.svg)](https://github.com/poker-kid-100717/GoodMusic/actions/workflows/ci.yml)

GoodMusic is a music catalog API (artists, songs, composers and user accounts) built on **ASP.NET Core 10 and MongoDB**. It uses an N-tier layout, and the MongoDB layer is written around documents rather than translated from a relational schema.

The API docs (Swagger UI) are the landing page. Register, then click **Authorize** and paste the token to try the write endpoints.

## Architecture

```
MyMusic.API        Controllers, request/response contracts, validation, JWT auth
   │
MyMusic.Services   Business rules (e.g. keep song → artist data consistent)
   │
MyMusic.Core       Models + repository/service interfaces — no framework or database dependencies
   ▲
MyMusic.Mongo.Db   MongoDB repositories, BSON class maps, indexes
```

`Core` knows nothing about MongoDB. The string ↔ ObjectId mapping, camelCase field names and indexes are all registered in `MyMusic.Mongo.Db`, so the domain models are plain C# classes.

### Document design decisions

| Decision | Instead of | Why |
|---|---|---|
| Songs store `artistId` and a copy of `artistName` | Looking up the artist for every song (`$lookup`, or a second query) | Listing songs is the hot path and reads far outnumber artist renames. A rename updates the copies with one `UpdateMany` (using the index on `artistId`) in the same transaction. |
| Deleting an artist that still has songs returns **409** | Cascading deletes, or leaving orphaned songs | MongoDB has no foreign keys. Artists keep a `songCount`, and deletion is a single `DeleteOne` that only matches when it's zero. |
| Usernames are stored lowercase, with a **unique index** | Checking before inserting | The index settles concurrent sign-ups for the same name. A duplicate-key error becomes a 409. |
| Ids are strings in C# and `ObjectId`s in MongoDB, references included | `ObjectId` in the domain model | The domain stays storage-agnostic. A malformed id returns 404 or an empty list, never a 500. |
| Song writes, their artist counters and artist renames each run in one **transaction** | Separate single-document writes with compensation | These touch several documents, and concurrent requests could leave counts or name copies out of step. Snapshot transactions make overlapping writes conflict, and the driver retries the loser. Core only sees an `ITransactionRunner` port. |

### Security

- **Passwords** are hashed with ASP.NET Core Identity's `PasswordHasher` (PBKDF2), and old hashes are upgraded on the next login.
- **JWT signing key** comes from configuration (`Jwt__Key`) and is never committed. Outside Development the app refuses to start without one.
- **Access:** reads are public. Creating, updating and deleting catalog data needs a token. Account endpoints only act on the signed-in user (`/api/User/me`), and there is no way to list or change other users.

## API

| Method | Route | Auth |
|---|---|---|
| `GET` | `/api/Artist`, `/api/Artist/{id}` | — |
| `POST` `PUT` `DELETE` | `/api/Artist[/{id}]` | Bearer |
| `GET` | `/api/Music`, `/api/Music/{id}`, `/api/Music/artist/{artistId}` | — |
| `POST` `PUT` `DELETE` | `/api/Music[/{id}]` | Bearer |
| `GET` | `/api/Composer`, `/api/Composer/{id}` | — |
| `POST` `PUT` `DELETE` | `/api/Composer[/{id}]` | Bearer |
| `POST` | `/api/User/register`, `/api/User/authenticate` → `{ token, expiresAt, user }` | — |
| `GET` `PUT` `DELETE` | `/api/User/me` | Bearer |
| `GET` | `/health` (pings MongoDB) | — |

Validation failures return standard `400` problem details, keyed by field.

## Running locally

Requires the .NET 10 SDK and Docker.

```bash
docker compose up -d                       # MongoDB 8 single-node replica set on localhost:27017
dotnet run --project MyMusic.API           # http://localhost:5080 — Swagger UI
```

## Tests

```bash
dotnet test        # integration tests: the real API against a MongoDB 8 replica set in Docker (Testcontainers)
npm ci && npm test # Cloudflare Worker routing
```

The API tests cover:
- **Auth:** anonymous writes are rejected; registration, case-insensitive login and duplicate usernames.
- **Data consistency:** a song's artist name follows a rename, deleting an artist that still has songs is refused, and concurrent creates, moves, deletes and renames leave counts and names exact.
- **Bad input:** validation problems, and malformed or unknown ids.
- **Storage:** references are stored as ObjectIds, passwords are hashed, and the username index is unique.
- **Other:** composer CRUD, managing your own account, and the health check.

## Deployment

The whole app runs on Cloudflare, backed by MongoDB Atlas:

```
Browser / API client ──► Cloudflare Worker (worker/index.ts)
                             └──► Cloudflare Container: ASP.NET Core 10 API ──► MongoDB Atlas
```

- **Deploy:** `wrangler deploy` builds the root `Dockerfile`, pushes the image and deploys the Worker.
- **Container:** runs as a non-root user and keeps no local state. It sleeps after 10 minutes without traffic and starts on the next request.
- **CI:** on push to `master`, CI builds and tests, then deploys. Afterwards it smoke-tests `/health`, a public read, and checks that an anonymous write gets `401`.

One-time setup:

1. Create a free MongoDB Atlas cluster and a database user. Allow network access from anywhere (Cloudflare egress IPs aren't fixed), then copy the `mongodb+srv://` connection string.
2. Subscribe to the Cloudflare Workers Paid plan, which Containers requires.
3. Add these GitHub repository secrets:

| Secret | Value |
|---|---|
| `CLOUDFLARE_API_TOKEN` | API token using the "Edit Cloudflare Workers" template |
| `CLOUDFLARE_ACCOUNT_ID` | Workers & Pages → Account ID |
| `MONGODB_URI` | Atlas connection string |
| `JWT_KEY` | A long random string, e.g. `openssl rand -base64 48` |
