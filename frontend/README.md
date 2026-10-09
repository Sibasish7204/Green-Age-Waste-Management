# Green Age Frontend

React 18, Vite, and TypeScript client for the Green Age Waste Collection demo.

## Run

```powershell
npm install
npm run dev
```

Vite runs at `http://localhost:5173` and uses `http://localhost:5080/api` by default. Copy `.env.example` to `.env.local` to override the API address or configure `VITE_GOOGLE_MAPS_API_KEY`. Without a Maps key, the address and coordinate picker uses browser geolocation when available.

Sign in with `deliver@demo.com` or `collector@demo.com`; both use `Demo@123`.

## Checks

```powershell
npm test
npm run build
```
