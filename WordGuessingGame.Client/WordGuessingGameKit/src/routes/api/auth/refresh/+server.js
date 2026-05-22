import { API_BASE } from '$env/static/private';
import { json } from '@sveltejs/kit';

export async function POST({ cookies }) {
  const refreshToken = cookies.get('refresh');
  if (!refreshToken) return json({ error: 'No refresh token' }, { status: 401 });

  const res = await fetch(`${API_BASE}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  });

  const data = await res.json();

  if (res.ok) {
    const cookieOpts = { httpOnly: true, secure: true, sameSite: 'lax', path: '/' };
    if (data.token) cookies.set('session', data.token, { ...cookieOpts, maxAge: 60 * 60 * 24 });
    if (data.refreshToken) cookies.set('refresh', data.refreshToken, { ...cookieOpts, maxAge: 60 * 60 * 24 * 30 });
    const { token, refreshToken: rt, ...userdata } = data;
    return json(userdata, { status: 200 });
  }

  cookies.delete('session', { path: '/' });
  cookies.delete('refresh', { path: '/' });
  return json(data, { status: res.status });
}
