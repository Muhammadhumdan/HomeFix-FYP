import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Port 5173 is allowed by the API's CORS policy (appsettings.json -> Cors:AllowedOrigins)
export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true }
});
