# Proposal: auth-jwt (Phase 5 of 7)

Phase 5 turns on authentication and authorization for the .NET 8 strangler stack. The JWT validation pipeline already exists in `Program.cs` but no token is ever issued, no endpoint is protected, and the `Pedido` state-transition path still trusts a role supplied in the request body. This change introduces a hand-rolled `Usuario` aggregate, a single `POST /auth/login` endpoint that issues an 8-hour access token, `[Authorize]` on every existing endpoint, and role extraction from the JWT claim for the one path that needs it. The result: a coherent, self-contained auth slice that closes the three `// PHASE-5` seams without dragging in legacy permission machinery or unbuilt user-management surface.

## Locked Decisions (User-Confirmed)

1. **Clean start, no legacy migration** — no live GastroGestion_Seguridad users in the .NET 8 stack. Seed an initial admin user in Development.
2. **Login endpoint only** — no self-registration or admin user management in Phase 5.
3. **8-hour token expiry** — one restaurant shift duration. Not 1 hour.
4. **Authorize all, role-check Pedido only** — all existing endpoints require `[Authorize]`; only the Pedido state-transition path role-checks via JWT claim extraction. Other endpoints require authentication only.
5. **Single catalog** — `Usuarios` table in the existing `GastroGestionDbContext`, not a separate `GastroGestion_Seguridad` context. Phase 6 technical debt.

## Proposed slicing

**2 stacked PRs (stacked-to-main).**

- **PR 1 — Auth foundation (no behavior change to existing endpoints):** `Usuario` aggregate + EF configuration + migration + login handler + contracts + JWT issuer + `POST /auth/login` + admin seeder.
- **PR 2 — Lock down (stacked on PR 1):** swap `[AllowAnonymous]` → `[Authorize]` across ~25 endpoints, role-from-claim on Pedido transition, test helpers, test migration (222 test updates).

Rationale: PR1 is self-contained and verifiable in isolation (login works, existing endpoints stay anonymous). PR2 is where the blast radius and test churn live; isolating it keeps the security-relevant diff explicit.

## Open risks

1. Test suite breaks on `[AllowAnonymous]` removal (222 tests return 401 until token helper lands).
2. `TransicionarEstadoRequest` drops the `Rol` field (internal-only breaking change).
3. Single-catalog deviation from config.yaml (Phase 6 debt).
4. Password hasher must stay in Infrastructure (Domain stays zero-dependency).
5. `JwtSecurityTokenHandler` (not `JsonWebTokenHandler`) to match existing wiring.

All risks are mitigated by the stacked-PR strategy and the test helper shipping with the protection change.
