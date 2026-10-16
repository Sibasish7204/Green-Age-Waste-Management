# Green Age Frontend

React 18, Vite, and TypeScript client for the Green Age Waste Collection demo.

## Run

```powershell
npm install
npm run dev
```

Vite runs at `http://localhost:5173` and uses `http://localhost:5080/api` by default. Copy `.env.example` to `.env.local` to override the API address or configure `VITE_GOOGLE_MAPS_API_KEY`. Without a Maps key, the address and coordinate picker uses browser geolocation when available.

The non-bio seller flow compares hardcoded nearby kabadiwala rates, creates a handover QR, and keeps each signed-in user's ten latest sale records in browser storage. Collector QR scans and confirmations are also local to the collector's browser. These demo records are not synchronized between devices.

## Municipal Bio Collection

The separate React app for household registration and ward collector collection is in `bio-collection/`. Run it in another terminal:

```powershell
cd bio-collection
npm install
npm run dev
```

It runs at `http://localhost:5174` and is available to devices on the same network. Copy `bio-collection/.env.example` to `bio-collection/.env.local` to configure the API URL. Household and collector accounts register and log in through the API. The app keeps the signed-in session on the device across restarts; the default API access token expires after 30 days, and users can sign out at any time. For a phone on the same network as the API, set `VITE_API_BASE_URL` to the API server's reachable address (for example, `http://192.168.1.20:5080/api`) and allow the app's origin in the API's `Cors:Origins` configuration.

Households generate short-lived, one-time QR tokens. Collectors scan and confirm them to record each visit in the API. Their monthly route log shows unique households covered, total visits, and repeat visits per household. The history is stored centrally, so it remains available when the collector signs in on another device.

Sign in with `deliver@demo.com` or `collector@demo.com`; both use `Demo@123`.

## Checks

```powershell
npm test
npm run build
```
