import { get } from 'svelte/store';
import {
  page, username, profilePicUrl, isGuest, isWaiting, gameStarted, inviteCode
} from './stores.js';
import { isTokenExpired } from './stores.js';
import { setLocale } from './i18n.js';

export const API_BASE = import.meta.env.VITE_API_BASE;

export async function tryRefreshToken() {
  const storedRefresh = localStorage.getItem("refreshToken");
  if (!storedRefresh) return;
  try {
    const res = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: storedRefresh })
    });
    if (res.ok) {
      const data = await res.json();
      username.set(data.username);
      localStorage.setItem("token", data.token);
      localStorage.setItem("username", data.username);
      if (data.refreshToken) localStorage.setItem("refreshToken", data.refreshToken);
      if (data.profilePictureUrl) {
        profilePicUrl.set(data.profilePictureUrl);
        localStorage.setItem("profilePicUrl", data.profilePictureUrl);
      }
      if (data.language) setLocale(data.language);
      page.set("dashboard");
    } else {
      localStorage.removeItem("token");
      localStorage.removeItem("username");
      localStorage.removeItem("refreshToken");
    }
  } catch { /* network error — stay on login */ }
}

// Returns on success; throws Error with message on failure.
export async function handleLogin(loginUsername, loginPassword, rememberMe) {
  const res = await fetch(`${API_BASE}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: loginUsername.trim(), password: loginPassword, rememberMe })
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message ?? "Login failed.");

  isGuest.set(false);
  username.set(data.username);
  localStorage.setItem("token", data.token);
  localStorage.setItem("username", data.username);
  if (data.refreshToken) localStorage.setItem("refreshToken", data.refreshToken);
  const pfp = data.profilePictureUrl ?? "";
  profilePicUrl.set(pfp);
  if (pfp) localStorage.setItem("profilePicUrl", pfp);
  else localStorage.removeItem("profilePicUrl");
  if (data.language) setLocale(data.language);
  page.set(get(inviteCode) ? "join-private" : "dashboard");
}

// Returns on success; throws Error with message on failure.
export async function handleRegister(regUsername, regEmail, regPassword) {
  const res = await fetch(`${API_BASE}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: regUsername.trim(), email: regEmail.trim(), password: regPassword })
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data.message ?? "Registration failed.");
  localStorage.setItem("token", data.token);
}

export async function handleLogout() {
  const storedRefresh = localStorage.getItem("refreshToken");
  if (storedRefresh) {
    try {
      await fetch(`${API_BASE}/api/auth/revoke`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: storedRefresh })
      });
    } catch { /* ignore */ }
  }
  username.set("");
  isGuest.set(false);
  isWaiting.set(false);
  gameStarted.set(false);
  profilePicUrl.set("");
  localStorage.removeItem("token");
  localStorage.removeItem("username");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("profilePicUrl");
  page.set("login");
}

// Throws on failure.
export async function saveLanguage(langCode) {
  const token = localStorage.getItem("token");
  if (!token) return;
  // Map "en" → 0, "nl" → 1
  const langMap = { nl: 0, en: 1 };
  const res = await fetch(`${API_BASE}/api/user/language`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({ language: langMap[langCode] ?? 0 })
  });
  if (!res.ok) throw new Error("Failed to save language.");
  setLocale(langCode);
}

// Returns { gamesPlayed, wins, winRate, streak } or throws on failure.
export async function fetchStats() {
  const token = localStorage.getItem("token");
  if (!token) throw new Error("Not authenticated.");
  const res = await fetch(`${API_BASE}/api/user/stats`, {
    headers: { "Authorization": `Bearer ${token}` }
  });
  if (!res.ok) throw new Error("Failed to fetch stats.");
  return res.json();
}

// Throws on failure.
export async function saveAvatar(avatarUrl) {
  const token = localStorage.getItem("token");
  if (!token) return;
  const res = await fetch(`${API_BASE}/api/user/avatar`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({ profilePictureUrl: avatarUrl || null })
  });
  if (!res.ok) throw new Error("Failed to save avatar.");
  profilePicUrl.set(avatarUrl);
  if (avatarUrl) localStorage.setItem("profilePicUrl", avatarUrl);
  else localStorage.removeItem("profilePicUrl");
}
