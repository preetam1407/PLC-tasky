import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

type LoginForm = {
  email: string;
  password: string;
};

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { isSubmitting }
  } = useForm<LoginForm>({
    defaultValues: {
      email: "",
      password: ""
    }
  });

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      await login(values.email, values.password);
      const redirectTo =
        (location.state as { from?: { pathname?: string } } | undefined)?.from?.pathname ?? "/";
      navigate(redirectTo, { replace: true });
    } catch (err) {
      console.error(err);
      setError("Login failed. Please verify your credentials.");
    }
  });

  return (
    <>
      <h1>Welcome back</h1>
      <p className="muted">Use your credentials to access the mini project manager.</p>

      {error && (
        <p role="alert" style={{ color: "salmon", fontWeight: 600 }}>
          {error}
        </p>
      )}

      <form onSubmit={onSubmit}>
        <label>
          Email
          <input type="email" placeholder="you@example.com" required {...register("email")} />
        </label>

        <label>
          Password
          <input type="password" placeholder="••••••••" required {...register("password")} />
        </label>

        <button className="primary-button" type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Signing in…" : "Continue"}
        </button>
      </form>

      <footer>
        <span className="muted">New here?</span>{" "}
        <Link to="/auth/register" style={{ color: "#c7d2fe" }}>
          Create an account
        </Link>
      </footer>
    </>
  );
}
