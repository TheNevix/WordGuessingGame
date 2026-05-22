import { goto } from '$app/navigation';
import {
  username, profilePicUrl, bannerColor, userTags, activeTag, isGuest, isWaiting, gameStarted
} from '$lib/stores.js';
import { setLocale } from '$lib/i18n.js';

// All API calls go through SvelteKit proxy routes — no direct .NET URL in browser
const API = '';

export async function tryRefreshToken() {
  try {
    const res = await fetch(`${API}/api/auth/refresh`, { method: 'POST' });
    if (res.ok) {
      const data = await res.json();
      _applyUserData(data);
      goto('/dashboard');
    }
  } catch { /* network error */ }
}

export async function handleLogin(loginUsername, loginPassword, rememberMe) {
  const res = await fetch(`${API}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: loginUsername.trim(), password: loginPassword, rememberMe })
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message ?? 'Login failed.');
  _applyUserData(data);
  const invite = new URLSearchParams(window.location.search).get('invite');
  goto(invite ? `/join?invite=${invite}` : '/dashboard');
}

export async function handleRegister(regUsername, regEmail, regPassword) {
  const res = await fetch(`${API}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: regUsername.trim(), email: regEmail.trim(), password: regPassword })
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message ?? 'Registration failed.');
  _applyUserData(data);
  goto('/dashboard');
}

export async function handleLogout() {
  try { await fetch(`${API}/api/auth/logout`, { method: 'POST' }); } catch { }
  username.set(''); isGuest.set(false); isWaiting.set(false); gameStarted.set(false);
  profilePicUrl.set(''); bannerColor.set('#5b21b6'); userTags.set([]); activeTag.set(null);
  ['username', 'profilePicUrl', 'bannerColor', 'userTags', 'activeTag'].forEach(k => localStorage.removeItem(k));
  goto('/login');
}

function _applyUserData(data) {
  isGuest.set(false);
  if (data.username)          { username.set(data.username);       localStorage.setItem('username', data.username); }
  if (data.profilePictureUrl) { profilePicUrl.set(data.profilePictureUrl); localStorage.setItem('profilePicUrl', data.profilePictureUrl); }
  else                        { profilePicUrl.set(''); localStorage.removeItem('profilePicUrl'); }
  const bc = data.bannerColor ?? '#5b21b6';
  bannerColor.set(bc); localStorage.setItem('bannerColor', bc);
  const tags = data.tags ?? [];
  userTags.set(tags); localStorage.setItem('userTags', JSON.stringify(tags));
  if (data.activeTag) { activeTag.set(data.activeTag); localStorage.setItem('activeTag', data.activeTag); }
  else                { activeTag.set(null); localStorage.removeItem('activeTag'); }
  if (data.language) setLocale(data.language);
}

export async function saveLanguage(langCode) {
  const langMap = { nl: 0, en: 1 };
  await fetch(`${API}/api/user/language`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ language: langMap[langCode] ?? 0 })
  });
  setLocale(langCode);
}

export async function saveBannerColor(color) {
  await fetch(`${API}/api/user/banner-color`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ bannerColor: color })
  });
  bannerColor.set(color); localStorage.setItem('bannerColor', color);
}

export async function setActiveTag(tagName) {
  try {
    await fetch(`${API}/api/user/active-tag`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ activeTag: tagName ?? null })
    });
  } catch { }
  if (tagName) { activeTag.set(tagName); localStorage.setItem('activeTag', tagName); }
  else         { activeTag.set(null); localStorage.removeItem('activeTag'); }
}

export async function fetchRankedStats() {
  const res = await fetch(`${API}/api/ranked/stats`);
  if (!res.ok) return null;
  return res.json();
}

export async function fetchRankedHistory() {
  const res = await fetch(`${API}/api/ranked/history`);
  if (!res.ok) return [];
  return res.json();
}

export async function fetchLeaderboard() {
  const res = await fetch(`${API}/api/ranked/leaderboard`);
  if (!res.ok) return [];
  return res.json();
}

export async function fetchChallenges() {
  const res = await fetch(`${API}/api/user/challenges`);
  if (!res.ok) throw new Error('Failed to fetch challenges.');
  return res.json();
}

export async function claimChallenge(challengeId) {
  const res = await fetch(`${API}/api/user/challenges/${challengeId}/claim`, { method: 'POST' });
  if (!res.ok) { const d = await res.json().catch(() => ({})); throw new Error(d.message ?? 'Failed to claim.'); }
  return res.json();
}

export async function saveAvatar(avatarUrl) {
  await fetch(`${API}/api/user/avatar`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ profilePictureUrl: avatarUrl || null })
  });
  profilePicUrl.set(avatarUrl);
  if (avatarUrl) localStorage.setItem('profilePicUrl', avatarUrl);
  else           localStorage.removeItem('profilePicUrl');
}
