import * as signalR from "@microsoft/signalr";
import { get } from 'svelte/store';
import {
  connection, page, username, isGuest,
  isWaiting, matchData, matchCountdown, isRematch,
  gameStarted, gameInformation, logMessages, letters,
  chatMessage, winnerMessage, isWon, rematchCount,
  currentTurn, hasVotedRematch, opponentLeft,
  privateLobbyLink, privateLobbyError, linkExpiresIn,
  inviteCode,
  isRankedGame, rankedSeriesScore, rankedSeriesOver, rankedRoundWinner,
  guessTimerActive, guessTimerSecs, rankTransition, matchFoundToast
} from './stores.js';
import { isTokenExpired, formatCountdown } from './stores.js';
import { tryRefreshToken, API_BASE } from './api.js';
import { tr } from './i18n.js';

let linkCountdownInterval  = null;
let matchCountdownInterval = null;
let matchFoundTimeout      = null;
let guessTimerInterval     = null;

export function initHub() {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE}/gamehub`, {
      accessTokenFactory: async () => {
        const token = localStorage.getItem("token");
        if (!token || isTokenExpired(token)) await tryRefreshToken();
        return localStorage.getItem("token") ?? "";
      }
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  conn.on("GameStarted", (gameInfo) => {
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
    page.set("game");
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
    // Detect tier change
    const me = get(username);
    const md = get(matchData);
    const isP1 = md?.player1 === me;
    const oldTier = isP1 ? info.player1OldTier : info.player2OldTier;
    const newTier = isP1 ? info.player1NewTier : info.player2NewTier;
    if (oldTier && newTier && oldTier !== newTier) {
      const order = ['scribbler','reader','wordsmith','scholar','sage','oracle'];
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
    const msgKey = `game.msg.${guessInfo.messageType}`;
    const msg = tr(msgKey, { username: guessInfo.username, guess: guessInfo.guess });
    logMessages.update(msgs => [...msgs, msg]);
    if (guessInfo.correctGuess) {
      letters.update(ls => {
        const next = [...ls];
        for (const index of guessInfo.indexes) {
          next[index] = guessInfo.guess.toUpperCase();
        }
        return next;
      });
    }
    currentTurn.set(guessInfo.turn);
    // Reset guess timer if ranked and now my turn
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
      isWon.set(true);
    } else {
      isWon.set(true);
    }
  });

  conn.on("PrivateLobbyCreated", (code) => {
    privateLobbyLink.set(`${window.location.origin}?invite=${code}`);
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
    page.set('dashboard');
  });

  conn.on("MatchFound", (data) => {
    matchFoundToast.set(true);
    setTimeout(() => matchFoundToast.set(false), 2500);
    isRematch.set(get(gameStarted));
    matchData.set(data);
    matchCountdown.set(5);
    // Start countdown immediately — it runs during the 2s lobby display too,
    // so the number shown on the countdown page stays in sync with the server.
    if (matchCountdownInterval) clearInterval(matchCountdownInterval);
    matchCountdownInterval = setInterval(() => {
      matchCountdown.update(v => {
        if (v <= 1) { clearInterval(matchCountdownInterval); matchCountdownInterval = null; return 0; }
        return v - 1;
      });
    }, 1000);
    // Navigate to countdown page after 2s
    if (matchFoundTimeout) clearTimeout(matchFoundTimeout);
    matchFoundTimeout = setTimeout(() => {
      matchFoundTimeout = null;
      isWaiting.set(false);
      page.set("countdown");
    }, 2000);
  });

  connection.set(conn);
  return conn;
}

// mode: 'public' | 'create-private' | 'join-private'
export async function connectToHub(mode, inviteCodeVal, displayName) {
  if (!displayName) {
    alert("Please enter a display name.");
    return;
  }
  username.set(displayName);
  try {
    const conn = get(connection);
    if (conn.state === signalR.HubConnectionState.Disconnected) {
      await conn.start();
    }
    if (mode === 'create-private') {
      conn.invoke("CreatePrivateLobby", displayName);
    } else if (mode === 'join-private') {
      conn.invoke("JoinPrivateLobby", inviteCodeVal, displayName);
    } else {
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
  page.set(get(isGuest) ? 'lobby' : 'dashboard');
}

function resetGameState() {
  stopGuessTimer();
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

function startGuessTimer() {
  stopGuessTimer();
  guessTimerSecs.set(30);
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
    page.set('login');
  } else {
    page.set('dashboard');
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

export async function connectToRanked(displayName) {
  try {
    const conn = get(connection);
    if (conn.state === signalR.HubConnectionState.Disconnected) await conn.start();
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
