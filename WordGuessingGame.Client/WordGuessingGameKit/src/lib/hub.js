import * as signalR from "@microsoft/signalr";
import { get } from 'svelte/store';
import { goto } from '$app/navigation';
import { browser } from '$app/environment';
import { PUBLIC_SIGNALR_BASE } from '$env/static/public';
import {
  connection, username, isGuest,
  isWaiting, matchData, matchCountdown, isRematch,
  gameStarted, isReconnecting, gameInformation, logMessages, letters,
  chatMessage, winnerMessage, isWon, rematchCount,
  currentTurn, hasVotedRematch, opponentLeft,
  privateLobbyLink, privateLobbyError, linkExpiresIn,
  isRankedGame, rankedSeriesScore, rankedSeriesOver, rankedRoundWinner,
  guessTimerActive, guessTimerSecs, rankTransition, matchFoundToast, gameMode
} from '$lib/stores.js';
import { formatCountdown } from '$lib/stores.js';
import { tr } from '$lib/i18n.js';

let linkCountdownInterval  = null;
let matchCountdownInterval = null;
let matchFoundTimeout      = null;
let guessTimerInterval     = null;

export function initHub() {
  if (!browser) return null;

  // Idempotent. On a hard refresh the (game) page's onMount runs before the root
  // layout's onMount (child onMount fires before parent), so reconnectToHub may call
  // this before the layout does — and the layout must not then build a second connection.
  const existing = get(connection);
  if (existing) return existing;

  const conn = new signalR.HubConnectionBuilder()
    .withUrl(`${PUBLIC_SIGNALR_BASE}/gamehub`, {
      accessTokenFactory: async () => {
        // Fetch JWT from server-side cookie via secure endpoint
        const res = await fetch('/api/auth/token');
        if (res.ok) {
          const { token } = await res.json();
          return token ?? '';
        }
        return '';
      }
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  conn.on("GameStarted", (gameInfo) => {
    sessionStorage.setItem('activeGame', JSON.stringify({ gameInfo, matchDataSnap: get(matchData) }));
    if (matchCountdownInterval) { clearInterval(matchCountdownInterval); matchCountdownInterval = null; }
    matchCountdown.set(0);
    gameStarted.set(true);
    logMessages.set([]);
    letters.set(Array(gameInfo.currentWordLength).fill(""));
    chatMessage.set('');
    winnerMessage.set(null);
    isWon.set(false);
    rematchCount.set(0);
    hasVotedRematch.set(false);
    gameInformation.set(gameInfo);
    currentTurn.set(gameInfo.turn);
    isRankedGame.set(!!gameInfo.isRanked);
    rankedSeriesScore.set({ p1: gameInfo.player1SeriesWins ?? 0, p2: gameInfo.player2SeriesWins ?? 0 });
    rankedSeriesOver.set(null);
    if (gameInfo.isRanked) startGuessTimer();
    goto('/spel');
  });

  conn.on("RankedRoundStarted", (info) => {
    letters.set(Array(info.currentWordLength).fill(""));
    logMessages.set([]);
    winnerMessage.set(null);
    rankedRoundWinner.set(null);
    isWon.set(false);
    currentTurn.set(info.turn);
    rankedSeriesScore.set({ p1: info.player1SeriesWins, p2: info.player2SeriesWins });
    startGuessTimer();
  });

  conn.on("RankedSeriesOver", (info) => {
    stopGuessTimer();
    rankedSeriesOver.set(info);
    isWon.set(true);
    const me = get(username);
    const md = get(matchData);
    const isP1 = md?.player1 === me;
    const oldTier = isP1 ? info.player1OldTier : info.player2OldTier;
    const newTier = isP1 ? info.player1NewTier : info.player2NewTier;
    if (oldTier && newTier && oldTier !== newTier) {
      const order = ['brons','zilver','goud','platina','diamant','kampioen'];
      const direction = order.indexOf(newTier.toLowerCase()) > order.indexOf(oldTier.toLowerCase()) ? 'up' : 'down';
      rankTransition.set({ oldTier, newTier, direction });
    }
  });

  conn.on("TurnTimedOut", (info) => {
    currentTurn.set(info.nextTurn);
    stopGuessTimer();
    if (info.nextTurn === get(username)) startGuessTimer();
  });

  conn.on("Guessed", (guessInfo) => {
    const msg = tr(`game.msg.${guessInfo.messageType}`, { username: guessInfo.username, guess: guessInfo.guess });
    logMessages.update(msgs => [...msgs, msg]);
    if (guessInfo.correctGuess) {
      letters.update(ls => {
        const next = [...ls];
        for (const index of guessInfo.indexes) next[index] = guessInfo.guess.toUpperCase();
        return next;
      });
    }
    currentTurn.set(guessInfo.turn);
    if (get(isRankedGame)) {
      stopGuessTimer();
      if (guessInfo.turn === get(username)) startGuessTimer();
    }
  });

  conn.on("WordGuessed", (info) => {
    stopGuessTimer();
    winnerMessage.set(tr('game.msg.word_correct', { winner: info.winner, word: info.word }));
    if (info.isRanked) {
      rankedSeriesScore.set({ p1: info.player1SeriesWins, p2: info.player2SeriesWins });
      rankedRoundWinner.set(info.winner);
    }
    isWon.set(true);
  });

  conn.on("PrivateLobbyCreated", (code) => {
    privateLobbyLink.set(`${window.location.origin}/join?invite=${code}`);
    linkExpiresIn.set(600);
    if (linkCountdownInterval) clearInterval(linkCountdownInterval);
    linkCountdownInterval = setInterval(() => {
      linkExpiresIn.update(v => {
        if (v <= 1) { clearInterval(linkCountdownInterval); linkCountdownInterval = null; return 0; }
        return v - 1;
      });
    }, 1000);
  });

  conn.on("PrivateLobbyError", (msg) => {
    privateLobbyError.set(msg);
    isWaiting.set(false);
  });

  conn.on("Rematch", () => { rematchCount.update(n => n + 1); });

  conn.on("Disconnected", () => {
    resetGameState();
    opponentLeft.set(true);
    goto('/home');
  });

  conn.on("GameReconnected", (data) => {
    isReconnecting.set(false);
    sessionStorage.removeItem('activeGame');
    gameMode.set(data.gameMode ?? 'quick');
    gameStarted.set(true);
    isWon.set(data.isWon ?? false);
    letters.set(data.revealedLetters ?? Array(data.currentWordLength).fill(''));
    // Synthesise minimal log entries so the keyboard shows already-guessed wrong letters
    logMessages.set((data.wrongLetters ?? []).map(l => `'${l}'`));
    currentTurn.set(data.turn);
    isRankedGame.set(!!data.isRanked);
    rankedSeriesScore.set({ p1: data.player1SeriesWins ?? 0, p2: data.player2SeriesWins ?? 0 });
    // Restore the ranked turn timer with the server's remaining seconds (only sent when
    // it's our turn). Without this the countdown never reappears after a refresh.
    if (data.isRanked && !data.isWon && (data.guessSecondsLeft ?? 0) > 0 && data.turn === get(username)) {
      startGuessTimer(data.guessSecondsLeft);
    }
    gameInformation.set({
      currentWordLength: data.currentWordLength,
      player1: data.player1,
      player2: data.player2,
      turn: data.turn,
      isRanked: data.isRanked,
      player1SeriesWins: data.player1SeriesWins ?? 0,
      player2SeriesWins: data.player2SeriesWins ?? 0,
    });
    matchData.set({
      player1: data.player1,
      player2: data.player2,
      player1Pfp: data.player1Pfp,
      player2Pfp: data.player2Pfp,
      player1BannerColor: data.player1BannerColor,
      player2BannerColor: data.player2BannerColor,
      player1ActiveTag: data.player1ActiveTag,
      player1ActiveTagColor: data.player1ActiveTagColor,
      player2ActiveTag: data.player2ActiveTag,
      player2ActiveTagColor: data.player2ActiveTagColor,
    });
    // If we reconnected from the countdown screen (a refresh before round 1 started),
    // move into the game. On /spel this is a no-op. GameStarted will fill in the board
    // once the countdown elapses if the word wasn't generated yet.
    if (browser && window.location.pathname !== '/spel') goto('/spel');
  });

  conn.on("ReconnectFailed", () => {
    isReconnecting.set(false);
    resetGameState();
    goto('/home');
  });

  conn.on("MatchFound", (data) => {
    matchFoundToast.set(true);
    setTimeout(() => matchFoundToast.set(false), 2500);
    isRematch.set(get(gameStarted));
    matchData.set(data);
    matchCountdown.set(5);
    if (matchCountdownInterval) clearInterval(matchCountdownInterval);
    matchCountdownInterval = setInterval(() => {
      matchCountdown.update(v => {
        if (v <= 1) { clearInterval(matchCountdownInterval); matchCountdownInterval = null; return 0; }
        return v - 1;
      });
    }, 1000);
    if (matchFoundTimeout) clearTimeout(matchFoundTimeout);
    matchFoundTimeout = setTimeout(() => {
      matchFoundTimeout = null;
      isWaiting.set(false);
      goto('/countdown');
    }, 2000);
  });

  connection.set(conn);
  return conn;
}

export async function connectToHub(mode, inviteCodeVal, displayName) {
  if (!displayName) { alert("Please enter a display name."); return; }
  username.set(displayName);
  try {
    const conn = get(connection);
    if (conn.state === signalR.HubConnectionState.Disconnected) await conn.start();
    if (mode === 'create-private') {
      gameMode.set('private');
      conn.invoke("CreatePrivateLobby", displayName);
    } else if (mode === 'join-private') {
      gameMode.set('private');
      conn.invoke("JoinPrivateLobby", inviteCodeVal, displayName);
    } else {
      gameMode.set('quick');
      conn.invoke("RegisterName", displayName);
    }
    isWaiting.set(true);
  } catch (err) {
    console.error("Connection failed:", err);
    alert("Could not connect to hub.");
  }
}

export async function cancelWaiting() {
  if (matchFoundTimeout) { clearTimeout(matchFoundTimeout); matchFoundTimeout = null; }
  try { await get(connection).stop(); } catch { /* ignore */ }
  isWaiting.set(false);
  matchData.set(null);
  privateLobbyLink.set('');
  linkExpiresIn.set(0);
  if (linkCountdownInterval) { clearInterval(linkCountdownInterval); linkCountdownInterval = null; }
  goto(get(isGuest) ? '/lobby' : '/home');
}

function resetGameState() {
  stopGuessTimer();
  isReconnecting.set(false);
  gameStarted.set(false);
  isWaiting.set(false);
  matchData.set(null);
  isWon.set(false);
  winnerMessage.set(null);
  rematchCount.set(0);
  hasVotedRematch.set(false);
  logMessages.set([]);
  letters.set([]);
  currentTurn.set(null);
  isRankedGame.set(false);
  rankedSeriesScore.set({ p1: 0, p2: 0 });
  rankedSeriesOver.set(null);
  rankedRoundWinner.set(null);
  rankTransition.set(null);
}

function startGuessTimer(secs = 30) {
  stopGuessTimer();
  guessTimerSecs.set(secs);
  guessTimerActive.set(true);
  guessTimerInterval = setInterval(() => {
    guessTimerSecs.update(s => {
      if (s <= 1) { stopGuessTimer(); return 0; }
      return s - 1;
    });
  }, 1000);
}

function stopGuessTimer() {
  if (guessTimerInterval) { clearInterval(guessTimerInterval); guessTimerInterval = null; }
  guessTimerActive.set(false);
  guessTimerSecs.set(30);
}

export function leaveGame() {
  get(connection).invoke("LeaveGame");
  resetGameState();
  if (get(isGuest)) {
    isGuest.set(false);
    goto('/inloggen');
  } else {
    goto('/home');
  }
}

export function sendChat(msg) {
  if (!msg.trim()) return;
  get(connection).invoke("Guess", msg);
  chatMessage.set('');
}

export function sendRematch() {
  get(connection).invoke("RematchVote");
  hasVotedRematch.set(true);
}

export async function reconnectToHub(displayName) {
  try {
    // On a hard refresh the root layout's onMount (which calls initHub) has not run yet,
    // because a child component's onMount fires before its parent's. Build the connection
    // here if needed so the reconnect doesn't silently no-op.
    const conn = get(connection) ?? initHub();
    if (!conn) return;
    if (conn.state === signalR.HubConnectionState.Disconnected) await conn.start();
    // Authenticated users: server auto-reconnects in OnConnectedAsync via JWT.
    // Guests: send explicit Reconnect as fallback.
    const isAuth = !!(await fetch('/api/auth/token').then(r => r.ok ? r.json() : {}).then(d => d.token).catch(() => null));
    // The username store may not be hydrated yet on a fresh load (also done in the root
    // layout's onMount), so fall back to localStorage for the guest reconnect name.
    const name = displayName || (browser ? localStorage.getItem('username') : '') || '';
    if (!isAuth && name) await conn.invoke("Reconnect", name);
  } catch (err) {
    console.error('[RECONNECT] error:', err);
    goto('/home');
  }
}

export async function connectToRanked(displayName) {
  try {
    const conn = get(connection);
    if (conn.state === signalR.HubConnectionState.Disconnected) await conn.start();
    gameMode.set('ranked');
    conn.invoke("RegisterRanked", displayName);
    isWaiting.set(true);
  } catch (err) {
    console.error("Ranked connection failed:", err);
  }
}

export function cancelRanked() {
  try { get(connection).invoke("CancelRanked"); } catch { /* ignore */ }
  isWaiting.set(false);
}
