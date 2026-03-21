import * as signalR from "@microsoft/signalr";
import { get } from 'svelte/store';
import {
  connection, page, username, isGuest,
  isWaiting, matchData, matchCountdown, isRematch,
  gameStarted, gameInformation, logMessages, letters,
  chatMessage, winnerMessage, isWon, rematchCount,
  currentTurn, hasVotedRematch,
  privateLobbyLink, privateLobbyError, linkExpiresIn,
  inviteCode
} from './stores.js';
import { isTokenExpired, formatCountdown } from './stores.js';
import { tryRefreshToken, API_BASE } from './api.js';
import { tr } from './i18n.js';

let linkCountdownInterval  = null;
let matchCountdownInterval = null;
let matchFoundTimeout      = null;

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
    page.set("game");
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
  });

  conn.on("WordGuessed", (info) => {
    winnerMessage.set(tr('game.msg.word_correct', { winner: info.winner, word: info.word }));
    isWon.set(true);
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
    isWaiting.set(true);
    gameStarted.set(false);
  });

  conn.on("MatchFound", (data) => {
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

export function sendChat(msg) {
  if (!msg.trim()) return;
  get(connection).invoke("Guess", msg);
  chatMessage.set('');
}

export function sendRematch() {
  get(connection).invoke("RematchVote");
  hasVotedRematch.set(true);
}
