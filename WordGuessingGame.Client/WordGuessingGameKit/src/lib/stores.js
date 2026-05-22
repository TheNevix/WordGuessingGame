import { writable } from 'svelte/store';
import { browser } from '$app/environment';

export function isTokenExpired(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.exp * 1000 < Date.now();
  } catch { return true; }
}

export function formatCountdown(secs) {
  const m = Math.floor(secs / 60).toString().padStart(2, '0');
  const s = (secs % 60).toString().padStart(2, '0');
  return `${m}:${s}`;
}

// Safe localStorage read — returns fallback on server
function ls(key, fallback = '') {
  if (!browser) return fallback;
  return localStorage.getItem(key) ?? fallback;
}
function lsJson(key, fallback) {
  if (!browser) return fallback;
  try { return JSON.parse(localStorage.getItem(key) ?? 'null') ?? fallback; } catch { return fallback; }
}

// ── User ──────────────────────────────────────────────────────
export const username      = writable('');
export const profilePicUrl = writable('');
export const bannerColor   = writable('#5b21b6');
export const userTags      = writable([]);
export const activeTag     = writable(null);
export const isGuest       = writable(false);
export const opponentLeft  = writable(false);

// Hydrate user stores from localStorage on client mount
export function hydrateFromStorage() {
  if (!browser) return;
  username.set(ls('username'));
  profilePicUrl.set(ls('profilePicUrl'));
  bannerColor.set(ls('bannerColor', '#5b21b6'));
  userTags.set(lsJson('userTags', []));
  activeTag.set(localStorage.getItem('activeTag'));
}

// ── Private lobby ─────────────────────────────────────────────
export const privateLobbyLink  = writable('');
export const privateLobbyError = writable('');
export const linkExpiresIn     = writable(0);

// ── Matchmaking ───────────────────────────────────────────────
export const isWaiting      = writable(false);
export const matchData      = writable(null);
export const matchCountdown = writable(0);
export const isRematch      = writable(false);

// ── Ranked ────────────────────────────────────────────────────
export const isRankedGame      = writable(false);
export const rankedSeriesScore = writable({ p1: 0, p2: 0 });
export const rankedSeriesOver  = writable(null);
export const rankedRoundWinner = writable(null);
export const rankedStats       = writable(null);
export const leaderboard       = writable([]);
export const guessTimerActive  = writable(false);
export const guessTimerSecs    = writable(30);
export const rankTransition    = writable(null);
export const matchFoundToast   = writable(false);
export const gameMode          = writable('quick');

// ── Game ──────────────────────────────────────────────────────
export const gameStarted     = writable(false);
export const gameInformation = writable(null);
export const logMessages     = writable([]);
export const letters         = writable([]);
export const chatMessage     = writable('');
export const winnerMessage   = writable(null);
export const isWon           = writable(false);
export const currentTurn     = writable(null);
export const rematchCount    = writable(0);
export const hasVotedRematch = writable(false);

// ── Hub ───────────────────────────────────────────────────────
export const connection = writable(null);
