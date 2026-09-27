import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/client'
import { getMe, login, register, type User } from './api'

export function AuthForm({ onSignedIn }: { onSignedIn: (user: User) => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setBusy(true)
    try {
      // Register does not sign the user in, so log in straight after.
      if (mode === 'register') await register(email, password)
      await login(email, password, rememberMe)
      const user = await getMe()
      if (user) onSignedIn(user)
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) setError('Wrong email or password.')
      else if (err instanceof ApiError && err.status === 400) setError('Could not register. Is the email taken, or the password under 8 characters?')
      else setError('Something went wrong. Is the backend running?')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex w-72 flex-col gap-3">
      <h1 className="text-2xl font-semibold">{mode === 'login' ? 'Log in' : 'Register'}</h1>
      <input
        type="email"
        required
        placeholder="Email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        className="rounded border px-2 py-1"
      />
      <input
        type="password"
        required
        minLength={8}
        placeholder="Password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        className="rounded border px-2 py-1"
      />
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={rememberMe} onChange={(e) => setRememberMe(e.target.checked)} />
        Remember me
      </label>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <button disabled={busy} className="rounded bg-gray-900 py-1 text-white disabled:opacity-50">
        {mode === 'login' ? 'Log in' : 'Register'}
      </button>
      <button
        type="button"
        onClick={() => setMode(mode === 'login' ? 'register' : 'login')}
        className="text-sm underline"
      >
        {mode === 'login' ? 'Need an account? Register' : 'Have an account? Log in'}
      </button>
    </form>
  )
}
