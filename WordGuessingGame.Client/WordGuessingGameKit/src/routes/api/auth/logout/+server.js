import { API_BASE } from '$env/static/private';
import { json } from '@sveltejs/kit';

export async function POST({ cookies }) {
  const refreshToken = cookies.get('refresh');
  if (refreshToken) {
    try {
      await fetch(`${API_BASE}/api/auth/revoke`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken })
      });
    } catch { /* ignore */ }
  }

  cookies.delete('session', { path: '/' });
  cookies.delete('refresh', { path: '/' });
  return json({ ok: true });
}
