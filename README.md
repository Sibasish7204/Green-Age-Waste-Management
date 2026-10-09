# Green Age Waste Collection

A demo platform connecting waste deliverers with local collectors. The React application and .NET API are kept in separate top-level folders.

## Projects

- `frontend/`: React 18, Vite, TypeScript, React Router, TanStack Query, Axios, Tailwind CSS.
- `backend/`: .NET 8 solution with API, Core, Infrastructure, Services, and xUnit projects.

## Run

Start the API from `backend/`:

```powershell
dotnet run --project src/GreenAge.Api
```

It serves `http://localhost:5080`; Swagger is available at `/swagger` in Development. The JSON provider seeds 13 categories and demo accounts on first launch.

Start the UI from `frontend/` in another terminal:

```powershell
npm install
npm run dev
```

Vite serves `http://localhost:5173`. Set `VITE_GOOGLE_MAPS_API_KEY` to enable Google Maps; the address and coordinate form remains available without a key. API URL defaults to `http://localhost:5080/api`.

Demo accounts use password `Demo@123`:

- `deliver@demo.com`
- `collector@demo.com`

Build and test with `dotnet test backend/GreenAge.slnx` and, from `frontend/`, `npm test` and `npm run build`.
