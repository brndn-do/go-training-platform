const baseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

export class ApiError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`Request failed with status ${status}`)
    this.status = status
  }
}

let onUnauthorized: () => void = () => {}

/** Registers what to do when any request comes back 401 (the session expired mid-use). */
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

/** Calls the API with the session cookie. Resolves to the parsed JSON body, or undefined on 204. */
export async function apiFetch<T>(
  path: string,
  options: { method?: string; body?: unknown } = {},
): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: options.method ?? 'GET',
    credentials: 'include',
    headers: options.body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  })

  if (response.status === 401) onUnauthorized()
  if (!response.ok) throw new ApiError(response.status)
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
