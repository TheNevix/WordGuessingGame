import { API_BASE } from '$env/static/private';

async function proxy(request, cookies, params, url, method) {
  const token = cookies.get('session');

  const target = new URL(`${API_BASE}/api/${params.path}`);
  url.searchParams.forEach((v, k) => target.searchParams.set(k, v));

  const headers = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const ct = request.headers.get('content-type');
  if (ct) headers['Content-Type'] = ct;

  const body = ['GET', 'HEAD'].includes(method) ? undefined : await request.arrayBuffer();

  const res = await fetch(target.toString(), { method, headers, body });
  const resBody = await res.arrayBuffer();

  return new Response(resBody, {
    status: res.status,
    headers: { 'content-type': res.headers.get('content-type') || 'application/json' }
  });
}

export const GET    = ({ request, cookies, params, url }) => proxy(request, cookies, params, url, 'GET');
export const POST   = ({ request, cookies, params, url }) => proxy(request, cookies, params, url, 'POST');
export const PUT    = ({ request, cookies, params, url }) => proxy(request, cookies, params, url, 'PUT');
export const DELETE = ({ request, cookies, params, url }) => proxy(request, cookies, params, url, 'DELETE');
