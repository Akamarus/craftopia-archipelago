# Diagnostic Collector v1.5.2

Adds field-only capture of active mission category definitions and claimed state,
and the current Anubis Soul Orb exchange UI counters. Observes successful category
claims and Anubis exchange button calls without changing native actions or rewards.
No recurring scene scan was added.

Collector tests and Release build pass; all 22 observer signatures were checked
against the installed assembly. Live F9 captures on October 16 show 165 category
definitions, complete membership for 125 active missions, and Anubis exchange
counter `[11,0,0]`, matching reward 12 being next. No successful new Anubis exchange
has yet been captured. Missing/truncated evidence remains explicitly unknown.

Installed DLL SHA-256:
`9822D70A7E24575E5F7E2B5EBEE2741F886B664E30175A8239338F5BD807E1A7`
