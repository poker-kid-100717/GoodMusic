import { defineCloudflareConfig } from "@opennextjs/cloudflare";

// Every page reads the session cookie, so everything renders per request and
// there's nothing for an incremental cache to hold; the default is enough.
export default defineCloudflareConfig();
