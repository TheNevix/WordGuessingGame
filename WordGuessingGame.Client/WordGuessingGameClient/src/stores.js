import { writable } from 'svelte/store';

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

const _storedToken = localStorage.getItem("token");
const _inviteCode  = new URLSearchParams(window.location.search).get('invite') ?? '';

function getInitialPage() {
  if (_inviteCode) return 'join-private';
  if (_storedToken && !isTokenExpired(_storedToken)) return 'dashboard';
  return 'login';
}

// ── Navigation ────────────────────────────────────────────────
export const page = writable(getInitialPage());

// ── User ──────────────────────────────────────────────────────
export const username      = writable(localStorage.getItem("username") ?? "");
export const profilePicUrl = writable(localStorage.getItem("profilePicUrl") ?? "");
export const bannerColor   = writable(localStorage.getItem("bannerColor") ?? "#5b21b6");
export const userTags      = writable(JSON.parse(localStorage.getItem("userTags") ?? "[]"));
export const activeTag     = writable(localStorage.getItem("activeTag") ?? null);
export const isGuest       = writable(false);
export const opponentLeft  = writable(false);

// ── Private lobby ─────────────────────────────────────────────
export const inviteCode        = writable(_inviteCode);
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
export const rankedSeriesOver  = writable(null);   // null | { winner, p1RPChange, p2RPChange, ... }
export const rankedRoundWinner = writable(null);   // username of who won the last round
export const rankedStats       = writable(null);   // from API
export const leaderboard       = writable([]);
export const guessTimerActive  = writable(false);
export const guessTimerSecs    = writable(30);
export const rankTransition    = writable(null); // { oldTier, newTier, direction: 'up'|'down' } | null
export const matchFoundToast   = writable(false);
export const gameMode          = writable('quick'); // 'quick' | 'ranked' | 'private'

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
