import { expect, type Page, test } from "@playwright/test";

// One user walks through the catalog end to end. Names are unique per run,
// so the suite can run against a database that already has data.
const run = Date.now().toString(36);
const username = `e2e.${run}`;
const password = "Passw0rd!e2e";
const artistName = `The Testers ${run}`;
const otherArtist = `Second Band ${run}`;

test.describe.configure({ mode: "serial" });

async function signIn(page: Page, user = username, pass = password) {
  await page.goto("/login");
  await page.getByLabel("Username").fill(user);
  await page.getByLabel("Password").fill(pass);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("button", { name: "Sign out" })).toBeVisible();
}

test("anonymous visitors can browse but not edit", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Every artist, every song, one shelf." })).toBeVisible();

  await page.getByRole("link", { name: "Artists", exact: true }).first().click();
  await expect(page.getByRole("heading", { name: "Artists", level: 1 })).toBeVisible();
  await expect(page.getByLabel("New artist")).toHaveCount(0);
});

test("register, then build an artist's discography", async ({ page }) => {
  await page.goto("/register");
  await page.getByLabel("First name").fill("Ada");
  await page.getByLabel("Last name").fill("Lovelace");
  await page.getByLabel("Username").fill(username);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page.getByRole("link", { name: "Ada" })).toBeVisible();

  await page.goto("/artists");
  await page.getByLabel("New artist").fill(artistName);
  await page.getByRole("button", { name: "Add artist" }).click();
  await expect(page.getByRole("heading", { name: artistName, level: 1 })).toBeVisible();

  for (const song of ["Opening Track", "Second Single"]) {
    await page.getByLabel("New song").fill(song);
    await page.getByRole("button", { name: "Add song" }).click();
    await expect(page.getByText(song, { exact: true })).toBeVisible();
    await expect(page.getByLabel("New song")).toHaveValue("");
  }
  await expect(page.getByText("2 songs", { exact: true })).toBeVisible();
});

test("an artist with songs can't be deleted, and a rename reaches every song", async ({ page }) => {
  await signIn(page);
  await page.goto("/artists");
  await page.getByRole("link", { name: new RegExp(artistName) }).click();

  await page.getByRole("button", { name: "Delete artist" }).click();
  await page.getByRole("button", { name: "Yes, delete" }).click();
  await expect(page.getByRole("main").getByRole("alert")).toContainText("still has songs");

  const renamed = `${artistName} (Remastered)`;
  await page.getByLabel("Name", { exact: true }).fill(renamed);
  await page.getByRole("button", { name: "Rename" }).click();
  await expect(page.getByRole("heading", { name: renamed, level: 1 })).toBeVisible();

  await page.goto(`/songs?q=${encodeURIComponent(run)}`);
  await expect(page.getByRole("link", { name: renamed })).toHaveCount(2);
});

test("songs can be searched, moved to another artist and deleted", async ({ page }) => {
  await signIn(page);
  await page.goto("/artists");
  await page.getByLabel("New artist").fill(otherArtist);
  await page.getByRole("button", { name: "Add artist" }).click();
  await expect(page.getByRole("heading", { name: otherArtist, level: 1 })).toBeVisible();

  await page.goto("/songs");
  await page.getByRole("searchbox", { name: "Search songs" }).fill("Second Single");
  await expect(page).toHaveURL(/q=Second\+Single/);
  const row = page.getByRole("listitem").filter({ hasText: "Second Single" }).filter({ hasText: run });
  await expect(row).toHaveCount(1);

  await row.getByRole("button", { name: "Edit Second Single" }).click();
  await page.getByLabel("Artist").last().selectOption({ label: otherArtist });
  await page.getByRole("button", { name: "Save" }).click();
  await expect(page.getByRole("listitem").filter({ hasText: "Second Single" }).getByRole("link", { name: otherArtist })).toBeVisible();

  await page.goto("/artists");
  await expect(page.getByRole("link", { name: new RegExp(otherArtist) })).toContainText("1 song");

  const moved = page.goto("/songs?q=Second+Single");
  await moved;
  const movedRow = page.getByRole("listitem").filter({ hasText: otherArtist });
  await movedRow.getByRole("button", { name: "Delete" }).click();
  await movedRow.getByRole("button", { name: "Yes, delete" }).click();
  await expect(page.getByRole("listitem").filter({ hasText: otherArtist })).toHaveCount(0);
});

test("an artist without songs can be deleted", async ({ page }) => {
  await signIn(page);
  await page.goto("/artists");
  await page.getByRole("link", { name: new RegExp(otherArtist) }).click();
  await page.getByRole("button", { name: "Delete artist" }).click();
  await page.getByRole("button", { name: "Yes, delete" }).click();
  await expect(page).toHaveURL(/\/artists$/);
  await expect(page.getByRole("link", { name: new RegExp(otherArtist) })).toHaveCount(0);
});

test("composers can be added, edited and removed", async ({ page }) => {
  await signIn(page);
  await page.goto("/composers");
  await page.getByLabel("First name").fill("Clara");
  await page.getByLabel("Last name").fill(`Schumann ${run}`);
  await page.getByRole("button", { name: "Add composer" }).click();

  const row = page.getByRole("listitem").filter({ hasText: `Schumann ${run}` });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /^Edit/ }).click();
  await page.getByLabel("First name").last().fill("Clara Josephine");
  await page.getByRole("button", { name: "Save" }).click();
  await expect(page.getByRole("listitem").filter({ hasText: "Clara Josephine" })).toBeVisible();

  const edited = page.getByRole("listitem").filter({ hasText: `Schumann ${run}` });
  await edited.getByRole("button", { name: "Delete" }).click();
  await edited.getByRole("button", { name: "Yes, delete" }).click();
  await expect(page.getByRole("listitem").filter({ hasText: `Schumann ${run}` })).toHaveCount(0);
});

test("account: validation, password change and sign-in again", async ({ page }) => {
  await signIn(page);
  await page.goto("/account");

  await page.getByLabel("New password", { exact: true }).fill("AnotherPass1!");
  await page.getByLabel("Confirm new password").fill("does-not-match");
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("The passwords don't match.")).toBeVisible();

  await page.getByLabel("First name").fill("Augusta");
  await page.getByLabel("New password", { exact: true }).fill("AnotherPass1!");
  await page.getByLabel("Confirm new password").fill("AnotherPass1!");
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByRole("main").getByRole("status")).toContainText("Profile and password updated.");
  await expect(page.getByRole("link", { name: "Augusta" })).toBeVisible();

  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(page.getByRole("link", { name: "Sign in" })).toBeVisible();

  await page.goto("/login");
  await page.getByLabel("Username").fill(username);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("main").getByRole("alert")).toContainText("incorrect");

  await signIn(page, username, "AnotherPass1!");
});

test("the session cookie is httpOnly and editing requires it", async ({ page, context }) => {
  await signIn(page, username, "AnotherPass1!");
  const session = (await context.cookies()).find((cookie) => cookie.name === "gm_session");
  expect(session?.httpOnly).toBe(true);
  expect(await page.evaluate(() => document.cookie)).not.toContain("gm_session");

  await context.clearCookies();
  await page.goto("/account");
  await expect(page).toHaveURL(/\/login\?next=(%2F|\/)account/);
});
