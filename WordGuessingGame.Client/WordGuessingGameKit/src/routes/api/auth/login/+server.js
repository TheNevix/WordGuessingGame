import { API_BASE } from '$env/static/private';
import { json } from '@sveltejs/kit';

export async function POST({ request, cookies }) {
  const body = await request.json();
  const res = await fetch(`${API_BASE}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });

  const data = await res.json();

  if (res.ok) {
    const cookieOpts = { httpOnly: true, secure: true, sameSite: 'lax', path: '/' };
    if (data.token) cookies.set('session', data.token, { ...cookieOpts, maxAge: 60 * 60 * 24 });
    if (data.refreshToken) cookies.set('refresh', data.refreshToken, { ...cookieOpts, maxAge: 60 * 60 * 24 * 30 });
    const { token, refreshToken, ...userdata } = data;
    return json(userdata, { status: 200 });
  }

  return json(data, { status: res.status });
}
