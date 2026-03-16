# TODOs

## Backoffice

- [ ] Fix turbulence nav button — clicking it leads to 404; check and fix the backoffice routing
- [ ] Fix backoffice links to turbulence API endpoints (linking errors)
- [ ] Add icons to every navbar link (like hexagons already have)
- [ ] Home page: if data fetch fails, show "unable to fetch data now" instead of crashing

## API / Backend

- [ ] Apply `/clean` endpoint to wipe the database
- [ ] Fix h3/view filters — anomaly and temporal window filters are not working at all
- [ ] Clarify h3/view anomaly filter logic: AND vs OR between anomaly types
- [ ] Normalize entity and DTO field names for consistency
- [ ] Improve error handling — surface errors with more clarity to the frontend

## Infrastructure / Deployment

- [ ] Upload and deploy API to server
- [ ] Research `git fetch` vs `git pull` on server — figure out how to select a specific release (e.g. v0.4.5, v0.4.6)

## Future Features

- [ ] Study and experiment with i18n for data served to the frontend
- [ ] Explore Radzen to generate backoffice pages from data source (exifapi.db)
- [ ] Implement agents (who registered / submitted data)
- [ ] Implement voting system
