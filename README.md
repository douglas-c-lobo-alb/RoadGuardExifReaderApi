# ExifApi — RoadGuard

A geospatial road condition monitoring platform. Ingests road survey photos, extracts GPS/EXIF metadata, indexes locations using the H3 hexagonal grid, and tracks road anomalies and turbulence metrics.

---

## Roadmap

The main ideas and direction of this roadmap came from the developer. Claude (AI) helped sharpen the details, structure the milestones, and fill in specifics through an open back-and-forth debate. It is not set in stone — priorities may shift as development progresses.

### In Progress (v0.x)

**API**
- Pagination on all list endpoints
- Consistent RFC 7807 `ProblemDetails` error responses
- Input validation with field-level errors
- Expose `RoadTurbulenceType` via `/api/introspection`

**Backoffice**
- Loading states
- Toast notifications

**Tests**
- Expand coverage across all endpoint groups

### To be Done

**Entities**
- Add `Deleted` field to entities

---

### v1.0 — Authentication & Authorization

**Identity**
- Username/password login via ASP.NET Core Identity
- Roles: `Admin`, `Operator`, `Viewer`
- First-run provisioning of initial admin account

**Backoffice**
- Login / logout pages
- All routes protected

**API**
- JWT bearer tokens
- Refresh tokens
- Rate limiting on auth endpoints

---

### v2.0 — Background Workers

**Jobs**
- Batch H3 index generation as a background service
- Image upload pipeline (EXIF → hexagon assignment) runs async
- Seed service refactored as a background job with progress tracking
- Job status polling via `/api/jobs/{id}`
