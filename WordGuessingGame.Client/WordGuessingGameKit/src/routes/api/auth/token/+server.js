import { json } from '@sveltejs/kit';

// Returns the session JWT so SignalR (browser WebSocket) can authenticate
export async function GET({ cookies }) {
  const token = cookies.get('session') ?? '';
  return json({ token });
}
