import {
  PropsWithChildren,
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState
} from "react";
import { api, authApi } from "../api/client";

interface AuthContextValue {
  token: string | null;
  email: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const TOKEN_KEY = "tasky.v2.token";
const EMAIL_KEY = "tasky.v2.email";

function applyTokenHeader(token: string | null) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`;
  } else {
    delete api.defaults.headers.common.Authorization;
  }
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [token, setToken] = useState<string | null>(() => {
    if (typeof window === "undefined") return null;
    const stored = window.localStorage.getItem(TOKEN_KEY);
    applyTokenHeader(stored);
    return stored;
  });
  const [email, setEmail] = useState<string | null>(() =>
    typeof window !== "undefined" ? window.localStorage.getItem(EMAIL_KEY) : null
  );

  const logout = useCallback(() => {
    setToken(null);
    setEmail(null);
    applyTokenHeader(null);
    if (typeof window !== "undefined") {
      window.localStorage.removeItem(TOKEN_KEY);
      window.localStorage.removeItem(EMAIL_KEY);
    }
  }, []);

  useEffect(() => {
    applyTokenHeader(token);
    if (typeof window !== "undefined") {
      if (token) {
        window.localStorage.setItem(TOKEN_KEY, token);
      } else {
        window.localStorage.removeItem(TOKEN_KEY);
      }
    }
  }, [token]);

  useEffect(() => {
    if (email && typeof window !== "undefined") {
      window.localStorage.setItem(EMAIL_KEY, email);
    }
  }, [email]);

  useEffect(() => {
    const interceptor = api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          logout();
        }
        return Promise.reject(error);
      }
    );
    return () => {
      api.interceptors.response.eject(interceptor);
    };
  }, [logout]);

  const login = useCallback(
    async (emailAddress: string, password: string) => {
      const result = await authApi.login(emailAddress, password);
      applyTokenHeader(result.token);
      setToken(result.token);
      setEmail(emailAddress);
    },
    []
  );

  const register = useCallback(
    async (emailAddress: string, password: string) => {
      await authApi.register(emailAddress, password);
      await login(emailAddress, password);
    },
    [login]
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      email,
      isAuthenticated: Boolean(token),
      login,
      register,
      logout
    }),
    [token, email, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used inside <AuthProvider>");
  }
  return ctx;
}
