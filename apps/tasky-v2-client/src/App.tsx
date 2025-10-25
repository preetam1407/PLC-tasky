import { BrowserRouter, Link, Navigate, Outlet, Route, Routes, useLocation } from "react-router-dom";
import { AuthProvider, useAuth } from "./context/AuthContext";
import LoginPage from "./routes/LoginPage";
import RegisterPage from "./routes/RegisterPage";
import DashboardPage from "./routes/DashboardPage";
import ProjectPage from "./routes/ProjectPage";

function ProtectedLayout() {
  const { email, logout } = useAuth();
  const location = useLocation();

  return (
    <div className="layout">
      <header className="layout__header">
        <Link to="/" className="logo" aria-label="Tasky Mini Project Manager">
          Tasky <span>Mini PM</span>
        </Link>
        <nav className="layout__nav" aria-label="Primary">
          <Link to="/" className={location.pathname === "/" ? "active" : undefined}>
            Dashboard
          </Link>
        </nav>
        <div className="layout__user">
          <span>{email}</span>
          <button type="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <main className="layout__content">
        <Outlet />
      </main>
    </div>
  );
}

function PublicLayout() {
  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        <Outlet />
      </div>
    </div>
  );
}

function RequireAuth({ children }: { children: JSX.Element }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  if (!isAuthenticated) {
    return <Navigate to="/auth/login" replace state={{ from: location }} />;
  }
  return children;
}

function AppRoutes() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/auth/login" element={<LoginPage />} />
        <Route path="/auth/register" element={<RegisterPage />} />
      </Route>

      <Route
        element={
          <RequireAuth>
            <ProtectedLayout />
          </RequireAuth>
        }
      >
        <Route path="/" element={<DashboardPage />} />
        <Route path="/projects/:projectId" element={<ProjectPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AuthProvider>
  );
}
