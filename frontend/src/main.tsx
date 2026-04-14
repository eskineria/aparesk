import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import 'simplebar-react/dist/simplebar.min.css'
import '@/assets/scss/app.scss'
import './i18n'
import App from './App.tsx'
import { LayoutProvider } from '@/context/useLayoutContext'
import { HelmetProvider } from 'react-helmet-async'
import { AuthProvider } from '@/context/AuthContext'
import { BrandingProvider } from '@/context/BrandingContext'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <HelmetProvider>
        <BrandingProvider>
          <AuthProvider>
            <LayoutProvider>
              <Suspense fallback={<div className="d-flex justify-content-center align-items-center vh-100"><div className="spinner-border text-primary" role="status"></div></div>}>
                <App />
              </Suspense>
            </LayoutProvider>
          </AuthProvider>
        </BrandingProvider>
      </HelmetProvider>
    </BrowserRouter>
  </StrictMode>,
)
