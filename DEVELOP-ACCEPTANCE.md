# Explicit native Develop acceptance

This is a Development-build-only QA entry, separate from the normal game and its SDK. It is prepared tooling, not evidence that a deployed backend has passed. It permits only `http://127.0.0.1:3170`. Root must first approve the deployed revisions and supply the owned PRO project and private app key. No release or public endpoint is supported.

Build the real game and Hello with `SeagullFeaturesSetup.BuildQa`. Its isolated product names start with `Seagull QA `; each participant needs a distinct generated product/profile. Launch that player's executable with `-seagull-develop-acceptance=/absolute/private/manifest.json`. Do not put keys or credentials in command arguments or source. A normal build, an absent flag or a rejected manifest starts no QA command.

The private manifest contains `stage: "develop"`, `rootApproved: true`, `baseUrl: "http://127.0.0.1:3170"`, canonical UUID `runId` and `projectId`, `appApiKey`, the exact 40-character lowercase `serverCommit` and `dashboardCommit`, an existing absolute `controlDirectory`, and `allowWrites`. Create separate owned directories with mode0700 and use umask077. Keep the manifest outside the command directory. Existing private guest/session files belong to that same isolated profile.

Place a complete command atomically in `<controlDirectory>/<canonical-UUID>.json`. Every command has matching `id` and fixed `action`. Results are private `<controlDirectory>/results/<id>.json`; they may contain the original guest recovery credential. Never print or commit them, or attach them as public evidence. Keep only sanitized IDs, statuses, revisions, payload/source hashes and timings in the report.

- `createGuest`, with `confirm: true`, explicitly calls the real SDK once; existing identities require `restoreSaved`. Creation never substitutes for failed restoration.
- `feature` uses the actual game Feature Lab: `feature`, `operation`, `argumentsJson` contain its existing typed arguments. Supply `confirm: true`, including for explicitly reviewed read actions. Its `output` is the actual UI result; `invoked` alone does not prove a successful business operation. Durable SDK originals remain authoritative.
- `loadJson` / `loadBinary` return the actual Cloud Save revision as a decimal string and exact bytes as Base64. A missing revision stays unknown.
- `saveJson` / `saveBinary` require `confirm: true`, original canonical `expectedRevision` and exact `dataBase64`. They call the real conditional SDK save once, report HTTP status/conflict, and never rebase or retry a stale command.
- `guide`, with `feature` equal to `groups`, `items`, `matches` or `shared-objects`, invokes the complete copied public guide's explicit Read method in the existing session. Record the first actual HTTP response and elapsed seconds, separately from DTO/business validation. This measures an existing installation/session, not a fresh installation within five minutes.
- `capture` saves a private screenshot; check it for credentials before publishing. `quit` closes this owned player.

A permanent `.started` file is flushed before dispatch. A restart never redispatches such a command automatically, even if the result was lost. It reports `UNKNOWN_OUTCOME_ORIGINAL_RETAINED`; reconcile through the actual SDK sample and authoritative reads before an explicit new command. The original command and SDK journal remain unchanged. Partial `.next` results stop processing. Final ACK behavior remains in the existing SDK samples.

For TASK-1024, restore the original Unity guest credential in the owned Godot QA profile, compare the exact same player UUID, load R in both native clients, then verify JSON R→R+1, stale409, binary R+1→R+2, stale409, reload and JSON R+2→R+3. Preserve original bytes/revisions; never infer an empty save from an unreadable response. Publish actual Develop results only after the explicit Go and successful assertions.

`SeagullFeaturesSetup.Preflight` compiles this entry with the complete native project and checks exact-target/approval/permanent-file gates without a manifest, session or HTTP request. Provider source CI and earlier synthetic transport tests remain separate evidence.
