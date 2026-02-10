### Review follow-ups (2025‑10)

- Overview
  - Source: Architecture/code review
  - Status: In progress

### High priority
- [ ] Fix register endpoint to return safe DTO (not `Entity.User`). File: `src/Server/Program.fs`
- [ ] Add DB unique indexes for `users.email` and `user_profiles.profile_slug`. Files: migrations
- [x] Implement real API in `src/Client/src/ApiClient.fs` (replace stubs with HTTP calls)
- [ ] Harden cookie config for prod (Secure=Always, SameSite, CSRF strategy). File: `src/Server/Program.fs`
- [ ] Replace `failwith` auth errors with 401/403. File: `src/Server/Program.fs`

### Medium priority
- [ ] Validate link URL/platform and profile fields server‑side
- [ ] Transactional `saveLinks`; consider diffing vs. wholesale replace
- [ ] Rate‑limit and cache headers for `/api/public/preview/:slug`
- [ ] Basic security headers middleware

### Nice to have
- [ ] Tests: Elmish update functions; backend unit/integration (slug, auth, links)
- [ ] CI improvements for migrations and seeding
- [ ] Nginx static assets cache/gzip

### Links
- Tracking issue: #<id> (optional)
- Related issues: #<id>, #<id>
