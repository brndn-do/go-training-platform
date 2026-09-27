import { useEffect, useState } from 'react'
import { setUnauthorizedHandler } from './shared/api/client'
import { AuthForm } from './features/auth/AuthForm'
import { getMe, logout, type User } from './features/auth/api'
import { GamePage } from './features/game/GamePage'

function App() {
  // undefined = still asking the API; null = signed out.
  const [user, setUser] = useState<User | null | undefined>(undefined)

  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null))
    getMe().then(setUser, () => setUser(null))
  }, [])

  async function handleLogout() {
    await logout()
    setUser(null)
  }

  return (
    <div className="flex min-h-svh flex-col items-center gap-6 p-6 text-gray-900">
      {user && (
        <header className="flex w-full max-w-md justify-between text-sm">
          <span>{user.email}</span>
          <button onClick={handleLogout} className="underline">
            Log out
          </button>
        </header>
      )}
      {user === undefined && <p>Loading…</p>}
      {user === null && <AuthForm onSignedIn={setUser} />}
      {user && <GamePage />}
    </div>
  )
}

export default App
