import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

type RegisterForm = {
  email: string;
  password: string;
  confirmPassword: string;
};

export default function RegisterPage() {
  const { register: registerUser } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  const {
    register: formRegister,
    handleSubmit,
    watch,
    formState: { isSubmitting }
  } = useForm<RegisterForm>({
    defaultValues: {
      email: "",
      password: "",
      confirmPassword: ""
    }
  });

  const onSubmit = handleSubmit(async (values) => {
    if (values.password !== values.confirmPassword) {
      setError("Passwords do not match.");
      return;
    }
    setError(null);
    try {
      await registerUser(values.email, values.password);
      navigate("/", { replace: true });
    } catch (err) {
      console.error(err);
      setError("Registration failed. Try a different email address.");
    }
  });

  return (
    <>
      <h1>Create an account</h1>
      <p className="muted">Start managing your projects after a quick registration.</p>

      {error && (
        <p role="alert" style={{ color: "salmon", fontWeight: 600 }}>
          {error}
        </p>
      )}

      <form onSubmit={onSubmit}>
        <label>
          Email
          <input type="email" placeholder="you@example.com" required {...formRegister("email")} />
        </label>

        <label>
          Password
          <input
            type="password"
            placeholder="At least 8 characters"
            minLength={8}
            required
            {...formRegister("password")}
          />
        </label>

        <label>
          Confirm password
          <input
            type="password"
            placeholder="Repeat your password"
            required
            {...formRegister("confirmPassword", {
              validate: (value) => value === watch("password") || "Passwords do not match."
            })}
          />
        </label>

        <button className="primary-button" type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Creating…" : "Create account"}
        </button>
      </form>

      <footer>
        <span className="muted">Already have an account?</span>{" "}
        <Link to="/auth/login" style={{ color: "#c7d2fe" }}>
          Sign in
        </Link>
      </footer>
    </>
  );
}
