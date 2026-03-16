<script>
  import { onMount } from "svelte";
  import * as signalR from "@microsoft/signalr";

  // ── Auth / page state ──────────────────────────────────────────
  // 'login' | 'register' | 'dashboard' | 'lobby'
  function isTokenExpired(token) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 < Date.now();
    } catch { return true; }
  }

  const _storedToken = localStorage.getItem("token");
  const _inviteCode = new URLSearchParams(window.location.search).get('invite') ?? '';

  let inviteCode = _inviteCode;
  let privateLobbyLink = '';
  let privateLobbyError = '';
  let connectMode = 'public'; // 'public' | 'create-private' | 'join-private'
  let linkCopied = false;
  let linkExpiresIn = 0;
  let linkCountdownInterval = null;

  let page = (() => {
    if (_inviteCode) return 'join-private';
    if (_storedToken && !isTokenExpired(_storedToken)) return 'dashboard';
    return 'login';
  })();
  let text = localStorage.getItem("username") ?? "";

  let rememberMe = false;
  let profilePicUrl = localStorage.getItem("profilePicUrl") ?? "";
  let avatarInput = profilePicUrl;
  let avatarSaved = false;
  let avatarSaving = false;

  // ── Match countdown state ───────────────────────────────────────
  let matchData = null;     // { player1, player2, player1Pfp, player2Pfp }
  let matchCountdown = 0;
  let matchCountdownInterval = null;

  let loginUsername = "";
  let loginPassword = "";
  let loginError = "";

  let regUsername = "";
  let regEmail = "";
  let regPassword = "";
  let regConfirm = "";
  let regError = "";
  let regSuccess = false;

  let isGuest = false;
  let guestName = "";

  // ── Game state ─────────────────────────────────────────────────
  let connection;
  let isWaiting = false;
  let gameStarted = false;
  let gameInformation;
  let logMessages = [];
  let letters = [];
  let chatMessage = "";
  let winnerMessage;
  let isWon = false;
  let currentTurn = null;
  let rematchCount = 0;
  let hasVotedRematch = false;
  let version = "v1.1.0";

  // ── Auth handlers ──────────────────────────────────────────────
  const API_BASE = import.meta.env.VITE_API_BASE;

  async function tryRefreshToken() {
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
        text = data.username;
        localStorage.setItem("token", data.token);
        localStorage.setItem("username", data.username);
        if (data.refreshToken) localStorage.setItem("refreshToken", data.refreshToken);
        if (data.profilePictureUrl) { profilePicUrl = data.profilePictureUrl; avatarInput = profilePicUrl; localStorage.setItem("profilePicUrl", profilePicUrl); }
        page = "dashboard";
      } else {
        localStorage.removeItem("token");
        localStorage.removeItem("username");
        localStorage.removeItem("refreshToken");
      }
    } catch { /* network error — stay on login */ }
  }

  let loginLoading = false;
  let regLoading = false;

  async function handleLogin() {
    if (!loginUsername.trim() || !loginPassword.trim()) {
      loginError = "Please fill in all fields.";
      return;
    }
    loginError = "";
    loginLoading = true;

    try {
      const res = await fetch(`${API_BASE}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: loginUsername.trim(), password: loginPassword, rememberMe })
      });

      const data = await res.json();

      if (!res.ok) {
        loginError = data.message ?? "Login failed.";
        return;
      }

      isGuest = false;
      text = data.username;
      localStorage.setItem("token", data.token);
      localStorage.setItem("username", data.username);
      if (data.refreshToken) localStorage.setItem("refreshToken", data.refreshToken);
      profilePicUrl = data.profilePictureUrl ?? "";
      avatarInput = profilePicUrl;
      if (profilePicUrl) localStorage.setItem("profilePicUrl", profilePicUrl);
      else localStorage.removeItem("profilePicUrl");
      page = inviteCode ? "join-private" : "dashboard";
    } catch {
      loginError = "Could not reach the server.";
    } finally {
      loginLoading = false;
    }
  }

  function handleGuest() {
    isGuest = true;
    text = "";
    page = "lobby";
  }

  async function handleRegister() {
    if (!regUsername.trim() || !regEmail.trim() || !regPassword.trim()) {
      regError = "Please fill in all fields.";
      return;
    }
    if (regPassword !== regConfirm) {
      regError = "Passwords do not match.";
      return;
    }
    regError = "";
    regLoading = true;

    try {
      const res = await fetch(`${API_BASE}/api/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: regUsername.trim(), email: regEmail.trim(), password: regPassword })
      });

      const data = await res.json();

      if (!res.ok) {
        regError = data.message ?? "Registration failed.";
        return;
      }

      regSuccess = true;
      localStorage.setItem("token", data.token);
      setTimeout(() => {
        regSuccess = false;
        loginUsername = regUsername;
        regUsername = regEmail = regPassword = regConfirm = "";
        page = inviteCode ? "join-private" : "login";
      }, 1500);
    } catch {
      regError = "Could not reach the server.";
    } finally {
      regLoading = false;
    }
  }

  async function handleLogout() {
    const storedRefresh = localStorage.getItem("refreshToken");
    if (storedRefresh) {
      try {
        await fetch(`${API_BASE}/api/auth/revoke`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken: storedRefresh })
        });
      } catch { /* ignore network errors on logout */ }
    }
    loginUsername = "";
    loginPassword = "";
    text = "";
    isGuest = false;
    isWaiting = false;
    gameStarted = false;
    localStorage.removeItem("token");
    localStorage.removeItem("username");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("profilePicUrl");
    profilePicUrl = "";
    avatarInput = "";
    page = "login";
  }

  // ── SignalR setup ──────────────────────────────────────────────
  onMount(async () => {
    // Auto-refresh if access token expired but refresh token exists
    const t = localStorage.getItem("token");
    if ((!t || isTokenExpired(t)) && localStorage.getItem("refreshToken")) {
      await tryRefreshToken();
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/gamehub`, {
        accessTokenFactory: async () => {
          const token = localStorage.getItem("token");
          if (!token || isTokenExpired(token)) {
            await tryRefreshToken();
          }
          return localStorage.getItem("token") ?? "";
        }
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.on("GameStarted", (gameInfo) => {
      if (matchCountdownInterval) { clearInterval(matchCountdownInterval); matchCountdownInterval = null; }
      matchCountdown = 0;
      gameStarted = true;
      logMessages = [];
      letters = [];
      chatMessage = "";
      winnerMessage = null;
      isWon = false;
      rematchCount = 0;
      hasVotedRematch = false;
      gameInformation = gameInfo;
      letters = Array(gameInfo.currentWordLength).fill("");
      currentTurn = gameInfo.turn;
      page = "game";
    });

    connection.on("Guessed", (guessInfo) => {
      logMessages = [...logMessages, guessInfo.message];
      if (guessInfo.correctGuess) {
        for (const index of guessInfo.indexes) {
          letters[index] = guessInfo.guess.toUpperCase();
        }
        letters = [...letters];
      }
      currentTurn = guessInfo.turn;
    });

    connection.on("WordGuessed", (wordGuessedInfo) => {
      winnerMessage = wordGuessedInfo.message;
      isWon = true;
    });

    connection.on("PrivateLobbyCreated", (code) => {
      privateLobbyLink = `${window.location.origin}?invite=${code}`;
      linkExpiresIn = 600;
      linkCountdownInterval = setInterval(() => {
        linkExpiresIn--;
        if (linkExpiresIn <= 0) {
          clearInterval(linkCountdownInterval);
          linkCountdownInterval = null;
        }
      }, 1000);
    });

    connection.on("PrivateLobbyError", (msg) => {
      privateLobbyError = msg;
      isWaiting = false;
    });

    connection.on("Rematch", () => { rematchCount++; });

    connection.on("Disconnected", () => {
      isWaiting = true;
      gameStarted = false;
    });

    connection.on("MatchFound", (data) => {
      matchData = data;
      matchCountdown = 5;
      isWaiting = false;
      page = "countdown";
      if (matchCountdownInterval) clearInterval(matchCountdownInterval);
      matchCountdownInterval = setInterval(() => {
        matchCountdown--;
        if (matchCountdown <= 0) {
          clearInterval(matchCountdownInterval);
          matchCountdownInterval = null;
        }
      }, 1000);
    });
  });

  async function connectToHub() {
    const displayName = isGuest ? guestName.trim() : text;
    if (!displayName) {
      alert("Please enter a display name.");
      return;
    }
    text = displayName;
    try {
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        await connection.start();
      }
      if (connectMode === 'create-private') {
        connection.invoke("CreatePrivateLobby", text);
      } else if (connectMode === 'join-private') {
        connection.invoke("JoinPrivateLobby", inviteCode, text);
      } else {
        connection.invoke("RegisterName", text);
      }
      isWaiting = true;
    } catch (err) {
      console.error("Connection failed:", err);
      alert("Could not connect to hub.");
    }
  }

  function formatCountdown(secs) {
    const m = Math.floor(secs / 60).toString().padStart(2, '0');
    const s = (secs % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  }

  async function cancelWaiting() {
    try { await connection.stop(); } catch { /* ignore */ }
    isWaiting = false;
    privateLobbyLink = '';
    linkCopied = false;
    linkExpiresIn = 0;
    if (linkCountdownInterval) { clearInterval(linkCountdownInterval); linkCountdownInterval = null; }
    connectMode = 'public';
    page = isGuest ? 'lobby' : 'dashboard';
  }

  function sendChat() {
    if (!chatMessage.trim()) return;
    connection.invoke("Guess", chatMessage);
    chatMessage = "";
  }

  function sendRematch() {
    connection.invoke("RematchVote");
    hasVotedRematch = true;
  }

  async function saveAvatar() {
    const token = localStorage.getItem("token");
    if (!token) return;
    avatarSaving = true;
    try {
      const res = await fetch(`${API_BASE}/api/user/avatar`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
        body: JSON.stringify({ profilePictureUrl: avatarInput.trim() || null })
      });
      if (res.ok) {
        profilePicUrl = avatarInput.trim();
        if (profilePicUrl) localStorage.setItem("profilePicUrl", profilePicUrl);
        else localStorage.removeItem("profilePicUrl");
        avatarSaved = true;
        setTimeout(() => (avatarSaved = false), 2500);
      }
    } catch { /* ignore */ } finally {
      avatarSaving = false;
    }
  }
</script>

<style>
  :global(*, *::before, *::after) { box-sizing: border-box; }
  :global(::-webkit-scrollbar) { width: 6px; }
  :global(::-webkit-scrollbar-track) { background: #f5f3ff; }
  :global(::-webkit-scrollbar-thumb) { background: #c4b5fd; border-radius: 999px; }
  :global(::-webkit-scrollbar-thumb:hover) { background: #7c3aed; }
  :global(body) {
    margin: 0;
    padding: 0;
    background: #f5f3ff;
    min-height: 100vh;
    font-family: 'Segoe UI', sans-serif;
  }

  /* ════════════════════════════════
     Auth layout (login / register)
  ════════════════════════════════ */
  .auth-container {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    padding: 2rem;
    box-sizing: border-box;
  }

  .card {
    background: white;
    border-radius: 20px;
    padding: 2.5rem;
    box-shadow: 0 8px 32px rgba(120, 60, 220, 0.12);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1.2rem;
    width: 100%;
    max-width: 420px;
    box-sizing: border-box;
  }

  /* ════════════════════════════════
     Dashboard layout
  ════════════════════════════════ */
  @keyframes fadeInUp {
    from { opacity: 0; transform: translateY(18px); }
    to   { opacity: 1; transform: translateY(0); }
  }

  .dashboard-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    background: #f5f3ff;
  }

  /* Navbar */
  .navbar {
    background: #5b21b6;
    border-bottom: 1px solid rgba(255,255,255,0.1);
    padding: 0 2rem;
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    position: sticky;
    top: 0;
    z-index: 10;
  }

  .navbar-brand {
    font-size: 1.2rem;
    font-weight: 700;
    color: white;
  }

  .navbar-right {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .user-badge {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: rgba(255,255,255,0.15);
    border: 1px solid rgba(255,255,255,0.25);
    border-radius: 999px;
    padding: 0.35rem 0.9rem;
    font-size: 0.9rem;
    font-weight: 600;
    color: white;
  }

  .avatar {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
    font-size: 0.85rem;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    position: relative;
  }

  .avatar::after {
    content: '';
    position: absolute;
    width: 9px;
    height: 9px;
    background: #22c55e;
    border: 2px solid #5b21b6;
    border-radius: 50%;
    bottom: -1px;
    right: -1px;
  }

  /* Hero banner */
  .hero-banner {
    background: linear-gradient(135deg, #5b21b6 0%, #7c3aed 45%, #a855f7 100%);
    padding: 2.5rem 2rem;
    display: flex;
    align-items: center;
    gap: 2rem;
    flex-wrap: wrap;
    position: relative;
    overflow: hidden;
    animation: fadeInUp 0.45s ease both;
  }

  .hero-banner::before {
    content: '';
    position: absolute;
    width: 380px;
    height: 380px;
    background: rgba(255,255,255,0.055);
    border-radius: 50%;
    top: -160px;
    right: -80px;
    pointer-events: none;
  }

  .hero-banner::after {
    content: '';
    position: absolute;
    width: 220px;
    height: 220px;
    background: rgba(255,255,255,0.04);
    border-radius: 50%;
    bottom: -100px;
    right: 280px;
    pointer-events: none;
  }

  .hero-avatar-wrap { position: relative; flex-shrink: 0; }

  .hero-avatar-ring {
    width: 76px;
    height: 76px;
    border-radius: 50%;
    background: rgba(255,255,255,0.18);
    border: 3px solid rgba(255,255,255,0.45);
    color: white;
    font-size: 2.1rem;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .online-dot {
    position: absolute;
    width: 14px;
    height: 14px;
    background: #22c55e;
    border: 2.5px solid white;
    border-radius: 50%;
    bottom: 2px;
    right: 2px;
  }

  .hero-text { flex: 1; min-width: 160px; }

  .hero-title {
    margin: 0 0 0.3rem;
    font-size: 1.7rem;
    font-weight: 800;
    color: white;
    letter-spacing: -0.01em;
  }

  .hero-subtitle { margin: 0 0 0.6rem; color: rgba(255,255,255,0.72); font-size: 0.92rem; }

  .rank-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    background: rgba(255,255,255,0.18);
    border: 1px solid rgba(255,255,255,0.3);
    border-radius: 999px;
    padding: 0.2rem 0.65rem;
    font-size: 0.75rem;
    font-weight: 700;
    color: rgba(255,255,255,0.9);
    letter-spacing: 0.03em;
  }

  .hero-stats {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
  }

  .hero-stat {
    display: flex;
    flex-direction: column;
    align-items: center;
    background: rgba(255,255,255,0.13);
    border: 1px solid rgba(255,255,255,0.18);
    border-radius: 14px;
    padding: 0.8rem 1.1rem;
    min-width: 68px;
    backdrop-filter: blur(4px);
    transition: background 0.2s;
  }

  .hero-stat:hover { background: rgba(255,255,255,0.2); }

  .hero-stat-icon { font-size: 1rem; line-height: 1; margin-bottom: 0.3rem; }

  .hero-stat-value {
    font-size: 1.45rem;
    font-weight: 800;
    color: white;
    line-height: 1;
  }

  .hero-stat-label {
    font-size: 0.68rem;
    color: rgba(255,255,255,0.65);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    margin-top: 0.2rem;
  }

  /* Dashboard main area */
  .dashboard-main {
    flex: 1;
    padding: 2rem;
    max-width: 960px;
    width: 100%;
    margin: 0 auto;
    box-sizing: border-box;
    animation: fadeInUp 0.45s ease 0.1s both;
  }

  .section-title {
    font-size: 0.75rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: #a78bfa;
    margin: 0 0 1rem;
  }

  /* Play cards */
  .play-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
    margin-bottom: 2rem;
  }

  .play-card {
    border-radius: 20px;
    padding: 2rem 1.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    cursor: pointer;
    transition: transform 0.15s, box-shadow 0.2s;
    position: relative;
    overflow: hidden;
  }

  .play-card:hover { transform: translateY(-3px); }

  .play-card-primary {
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    box-shadow: 0 8px 32px rgba(124, 58, 237, 0.35);
  }

  .play-card-secondary {
    background: white;
    border: 2px solid #d8b4fe;
    box-shadow: 0 4px 16px rgba(120, 60, 220, 0.08);
  }

  .play-card-primary:hover { box-shadow: 0 14px 44px rgba(124, 58, 237, 0.5); }
  .play-card-secondary:hover { border-color: #7c3aed; box-shadow: 0 8px 28px rgba(120, 60, 220, 0.15); }

  /* Shimmer sweep on hover for primary card */
  .play-card-primary::before {
    content: '';
    position: absolute;
    top: 0; left: -100%;
    width: 60%;
    height: 100%;
    background: linear-gradient(120deg, transparent, rgba(255,255,255,0.12), transparent);
    transition: left 0.5s ease;
    pointer-events: none;
  }
  .play-card-primary:hover::before { left: 160%; }

  /* Watermark icon */
  .play-card-watermark {
    position: absolute;
    bottom: -10px;
    right: 16px;
    font-size: 7rem;
    opacity: 0.08;
    line-height: 1;
    pointer-events: none;
    user-select: none;
  }

  .play-card-primary .play-card-watermark { opacity: 0.12; }

  .play-card-icon { font-size: 2.2rem; line-height: 1; }

  .play-card-title {
    margin: 0.4rem 0 0;
    font-size: 1.2rem;
    font-weight: 800;
  }

  .play-card-primary .play-card-title { color: white; }
  .play-card-secondary .play-card-title { color: #3b0764; }

  .play-card-desc {
    margin: 0;
    font-size: 0.88rem;
    line-height: 1.5;
  }

  .play-card-primary .play-card-desc { color: rgba(255,255,255,0.8); }
  .play-card-secondary .play-card-desc { color: #9ca3af; }

  .play-card-cta {
    margin-top: 0.75rem;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.9rem;
    font-weight: 700;
    width: fit-content;
    padding: 0.55rem 1.25rem;
    border-radius: 999px;
    border: none;
    cursor: pointer;
    transition: opacity 0.2s, transform 0.1s;
  }

  .play-card-primary .play-card-cta {
    background: white;
    color: #7c3aed;
  }

  .play-card-secondary .play-card-cta {
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
  }

  .play-card-cta:hover { opacity: 0.9; transform: translateY(-1px); }

  /* Feature card grid (coming soon) */
  .feature-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 1rem;
  }

  .feature-card {
    background: white;
    border-radius: 16px;
    padding: 1.25rem;
    box-shadow: 0 2px 12px rgba(120, 60, 220, 0.06);
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    position: relative;
    border: 2px solid transparent;
    transition: border-color 0.2s, box-shadow 0.2s, transform 0.15s;
  }

  .feature-card.dimmed { opacity: 0.55; }

  .card-icon { font-size: 1.75rem; line-height: 1; }

  .card-title {
    font-size: 0.95rem;
    font-weight: 700;
    color: #3b0764;
    margin: 0;
  }

  .card-desc {
    font-size: 0.85rem;
    color: #9ca3af;
    margin: 0;
    line-height: 1.4;
  }

  .badge-soon {
    position: absolute;
    top: 1rem;
    right: 1rem;
    background: #f3f4f6;
    color: #9ca3af;
    font-size: 0.7rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding: 0.2rem 0.5rem;
    border-radius: 999px;
  }

  /* ════════════════════════════════
     Game lobby redesign
  ════════════════════════════════ */
  @keyframes pulse-ring {
    0%   { transform: scale(1);   opacity: 0.6; }
    70%  { transform: scale(1.6); opacity: 0; }
    100% { transform: scale(1.6); opacity: 0; }
  }

  @keyframes float {
    0%, 100% { transform: translateY(0); }
    50%       { transform: translateY(-6px); }
  }

  .lobby-layout {
    min-height: 100vh;
    background: #f5f3ff;
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  .lobby-header {
    width: 100%;
    background: #5b21b6;
    padding: 0 2rem;
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .lobby-back {
    background: rgba(255,255,255,0.12);
    border: 1px solid rgba(255,255,255,0.25);
    color: white;
    border-radius: 8px;
    padding: 0.4rem 0.9rem;
    font-size: 0.88rem;
    font-weight: 600;
    cursor: pointer;
    width: auto;
    transition: background 0.2s;
  }
  .lobby-back:hover { background: rgba(255,255,255,0.22); opacity: 1; transform: none; }

  .lobby-header-title {
    font-size: 1.1rem;
    font-weight: 700;
    color: white;
  }

  .lobby-mode-badge {
    background: rgba(255,255,255,0.15);
    border: 1px solid rgba(255,255,255,0.25);
    border-radius: 999px;
    padding: 0.25rem 0.75rem;
    font-size: 0.8rem;
    font-weight: 700;
    color: white;
  }

  /* Arena */
  .lobby-arena {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 2.5rem;
    padding: 3rem 2rem 1.5rem;
    flex-wrap: wrap;
    width: 100%;
    max-width: 700px;
  }

  .player-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
    background: white;
    border-radius: 20px;
    padding: 2rem 2.5rem;
    box-shadow: 0 4px 24px rgba(120,60,220,0.1);
    border: 2px solid transparent;
    min-width: 150px;
    animation: fadeInUp 0.4s ease both;
  }

  .player-card-you {
    border-color: #a855f7;
    box-shadow: 0 4px 24px rgba(120,60,220,0.18);
    animation-delay: 0s;
  }

  .player-card-opponent {
    border-color: #e5e7eb;
    opacity: 0.7;
    animation-delay: 0.1s;
  }

  .player-card-avatar {
    width: 72px;
    height: 72px;
    border-radius: 50%;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
    font-size: 2rem;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
    animation: float 3s ease-in-out infinite;
  }

  .avatar-unknown {
    background: linear-gradient(135deg, #d1d5db, #9ca3af);
    animation: float 3s ease-in-out infinite 0.5s;
  }

  .player-card-name {
    margin: 0;
    font-size: 1.05rem;
    font-weight: 700;
    color: #3b0764;
  }

  .player-card-tag {
    font-size: 0.78rem;
    color: #a78bfa;
    font-weight: 600;
  }

  /* VS divider */
  .vs-divider {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
    flex-shrink: 0;
  }

  .vs-ring {
    width: 52px;
    height: 52px;
    border-radius: 50%;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
    font-size: 0.95rem;
    font-weight: 900;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 4px 16px rgba(124,58,237,0.4);
    letter-spacing: 0.05em;
  }

  .vs-line {
    width: 2px;
    height: 40px;
    background: linear-gradient(to bottom, #a855f7, transparent);
    display: none;
  }

  /* Name input for guest */
  .lobby-name-input {
    width: 100%;
    max-width: 360px;
    padding: 0 1rem;
    box-sizing: border-box;
  }

  /* CTA */
  .lobby-cta {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.6rem;
    padding: 1rem;
  }

  .find-match-btn {
    position: relative;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
    font-size: 1.1rem;
    font-weight: 800;
    padding: 1rem 3rem;
    border-radius: 999px;
    border: none;
    cursor: pointer;
    width: auto;
    letter-spacing: 0.03em;
    box-shadow: 0 6px 28px rgba(124,58,237,0.45);
    transition: transform 0.15s, box-shadow 0.2s;
    overflow: visible;
  }

  .find-match-btn:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 36px rgba(124,58,237,0.55);
    opacity: 1;
  }

  .find-match-pulse {
    position: absolute;
    inset: 0;
    border-radius: 999px;
    background: rgba(168,85,247,0.5);
    animation: pulse-ring 2s ease-out infinite;
    pointer-events: none;
  }

  .lobby-hint {
    margin: 0;
    font-size: 0.82rem;
    color: #a78bfa;
  }

  /* Rules strip */
  .lobby-rules {
    display: flex;
    gap: 1.5rem;
    flex-wrap: wrap;
    justify-content: center;
    padding: 1.5rem 2rem 2.5rem;
    max-width: 600px;
  }

  .rule-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.82rem;
    color: #7c3aed;
    font-weight: 500;
    background: white;
    border-radius: 999px;
    padding: 0.4rem 1rem;
    box-shadow: 0 2px 8px rgba(120,60,220,0.08);
  }

  .rule-icon { font-size: 0.9rem; }

  /* ════════════════════════════════
     Game / lobby layout
  ════════════════════════════════ */
  .game-container {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 1.5rem;
    padding: 2rem;
    box-sizing: border-box;
  }

  /* ════════════════════════════════
     Typography
  ════════════════════════════════ */
  .title {
    font-size: 1.9rem;
    font-weight: 700;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
    margin: 0;
    text-align: center;
  }

  .subtitle {
    font-size: 0.95rem;
    color: #9ca3af;
    margin: 0;
    text-align: center;
  }

  .status-text {
    color: #7c3aed;
    font-size: 1.3rem;
    font-weight: 500;
    text-align: center;
    margin: 0;
  }

  .error-text {
    color: #dc2626;
    font-size: 0.9rem;
    text-align: center;
    margin: 0;
  }

  .success-text {
    color: #16a34a;
    font-size: 0.95rem;
    font-weight: 600;
    text-align: center;
    margin: 0;
  }

  /* ════════════════════════════════
     Form
  ════════════════════════════════ */
  .field-wrapper {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
  }

  .input-group {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    width: 100%;
  }

  .input-label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #6d28d9;
    display: block;
  }

  input {
    width: 100%;
    padding: 0.8rem 1rem;
    font-size: 1rem;
    border: 2px solid #d8b4fe;
    border-radius: 10px;
    background: #faf8ff;
    color: #3b0764;
    outline: none;
    box-sizing: border-box;
    transition: border-color 0.2s, box-shadow 0.2s;
  }

  input:focus {
    border-color: #7c3aed;
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.12);
  }

  input::placeholder { color: #c4b5fd; }

  .remember-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.88rem;
    color: #6d28d9;
    cursor: pointer;
    width: 100%;
  }
  .remember-row input[type="checkbox"] {
    width: auto;
    padding: 0;
    accent-color: #7c3aed;
    cursor: pointer;
  }

  /* ════════════════════════════════
     Buttons
  ════════════════════════════════ */
  button {
    width: 100%;
    padding: 0.8rem 1.25rem;
    font-size: 1rem;
    border: none;
    border-radius: 10px;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
    font-weight: 600;
    cursor: pointer;
    box-sizing: border-box;
    transition: opacity 0.2s, transform 0.1s;
    letter-spacing: 0.02em;
  }

  button:hover    { opacity: 0.9; transform: translateY(-1px); }
  button:active   { transform: translateY(0); }
  button:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }

  .btn-ghost {
    background: transparent;
    border: 2px solid #d8b4fe;
    color: #7c3aed;
  }
  .btn-ghost:hover { background: #faf8ff; opacity: 1; }

  .btn-navbar {
    background: rgba(255,255,255,0.12);
    border: 1px solid rgba(255,255,255,0.25);
    color: white;
    width: auto;
    padding: 0.45rem 1rem;
    font-size: 0.88rem;
    border-radius: 8px;
  }
  .btn-navbar:hover { background: rgba(255,255,255,0.22); opacity: 1; }

  .btn-sm   { width: auto; padding: 0.55rem 1.1rem; font-size: 0.88rem; }
  .btn-icon { width: auto; padding: 0.55rem 1rem; font-size: 0.88rem; }

  .link-btn {
    background: none;
    border: none;
    color: #7c3aed;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
    font-size: 0.9rem;
    width: auto;
    text-decoration: underline;
  }
  .link-btn:hover  { opacity: 0.8; transform: none; }
  .link-btn:active { transform: none; }

  /* ════════════════════════════════
     Divider / link row
  ════════════════════════════════ */
  .divider {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    width: 100%;
    color: #c4b5fd;
    font-size: 0.85rem;
  }
  .divider::before, .divider::after {
    content: '';
    flex: 1;
    height: 1px;
    background: #e9d5ff;
  }

  .link-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.9rem;
    color: #9ca3af;
  }

  /* ════════════════════════════════
     Game elements
  ════════════════════════════════ */
  .word-container {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
    justify-content: center;
  }

  .letter-box {
    width: 56px;
    height: 56px;
    border: 2.5px solid #d8b4fe;
    border-radius: 10px;
    background: #faf8ff;
    color: #7c3aed;
    display: flex;
    justify-content: center;
    align-items: center;
    font-size: 1.8rem;
    font-weight: 700;
    text-transform: uppercase;
    transition: border-color 0.2s, background 0.2s;
  }

  .letter-box:not(:empty) {
    border-color: #7c3aed;
    background: #ede9fe;
  }

  .log-box {
    width: 100%;
    max-width: 560px;
    height: 180px;
    border: 2px solid #e9d5ff;
    border-radius: 12px;
    padding: 1rem;
    overflow-y: auto;
    box-sizing: border-box;
    background: white;
    box-shadow: 0 2px 12px rgba(120, 60, 220, 0.07);
  }

  .log-line {
    color: #6d28d9;
    font-size: 1rem;
    margin-bottom: 0.35rem;
    word-break: break-word;
    font-family: monospace;
  }

  .chat-wrapper {
    display: flex;
    gap: 0.75rem;
    width: 100%;
    max-width: 560px;
  }
  .chat-wrapper input  { flex: 1; width: auto; }
  .chat-wrapper button { width: auto; padding: 0.8rem 1.5rem; flex-shrink: 0; }

  .winner-text {
    font-size: 1.8rem;
    font-weight: 700;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
    text-align: center;
    margin: 0;
  }

  .invite-link-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    background: #faf8ff;
    border: 2px solid #d8b4fe;
    border-radius: 10px;
    padding: 0.6rem 0.8rem;
    box-sizing: border-box;
  }
  .invite-link-text {
    flex: 1;
    font-size: 0.8rem;
    color: #6d28d9;
    word-break: break-all;
    font-family: monospace;
  }
  .invite-link-row button { width: auto; flex-shrink: 0; }

  .rematch-info { color: #7c3aed; font-size: 1.1rem; font-weight: 500; margin: 0; }
  .voted-text   { color: #a855f7; font-size: 1rem; font-style: italic; margin: 0; }

  .version-label {
    position: fixed;
    bottom: 12px;
    right: 14px;
    color: #a78bfa;
    font-size: 0.8rem;
    opacity: 0.7;
    font-family: monospace;
    pointer-events: none;
  }

  /* ════════════════════════════════
     Mobile — disable hover effects on touch
  ════════════════════════════════ */
  @media (hover: none) {
    button:hover              { opacity: 1; transform: none; }
    .btn-ghost:hover          { background: transparent; }
    .play-card:hover          { transform: none; }
    .find-match-btn:hover     { transform: none; }
    .feature-card.active:hover {
      transform: none;
      box-shadow: 0 4px 16px rgba(120, 60, 220, 0.08);
      border-color: #d8b4fe;
    }
  }

  /* ════════════════════════════════
     Mobile — layout fixes ≤ 600px
  ════════════════════════════════ */
  @media (max-width: 600px) {
    /* ── Dashboard hero ── */
    .hero-banner {
      flex-direction: column;
      align-items: flex-start;
      padding: 1.5rem 1.25rem;
      gap: 1.25rem;
    }

    .hero-avatar-wrap { display: none; } /* hide on small — saves space */

    .hero-text { min-width: unset; }
    .hero-title { font-size: 1.35rem; }

    .hero-stats {
      width: 100%;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.6rem;
    }

    .hero-stat { min-width: unset; padding: 0.65rem 0.5rem; }
    .hero-stat-value { font-size: 1.2rem; }

    /* ── Dashboard main ── */
    .dashboard-main { padding: 1.25rem 1rem; }
    .play-grid      { grid-template-columns: 1fr; }
    .play-card      { padding: 1.5rem 1.25rem; }
    .feature-grid   { grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .feature-card   { padding: 1rem 0.85rem; }
    .card-desc      { font-size: 0.8rem; }

    /* ── Lobby ── */
    .lobby-header       { padding: 0 1rem; }
    .lobby-header-title { font-size: 0.95rem; }

    .lobby-arena {
      flex-direction: column;
      gap: 0;
      padding: 2rem 1.25rem 1rem;
    }

    .player-card         { padding: 1.25rem 2rem; flex-direction: row; gap: 1rem; min-width: unset; width: 100%; max-width: 320px; }
    .player-card-you     { border-radius: 20px 20px 6px 6px; }
    .player-card-opponent { border-radius: 6px 6px 20px 20px; opacity: 0.65; }
    .player-card-avatar  { width: 52px; height: 52px; font-size: 1.4rem; animation: none; }

    .vs-divider { flex-direction: row; padding: 0.5rem 0; width: 100%; max-width: 320px; justify-content: center; }
    .vs-ring    { width: 40px; height: 40px; font-size: 0.8rem; }
    .vs-line    { display: none; }

    .find-match-btn { padding: 0.9rem 2.5rem; font-size: 1rem; }

    .lobby-rules { gap: 0.6rem; padding: 1rem 1.25rem 2rem; }
    .rule-item   { font-size: 0.78rem; padding: 0.35rem 0.8rem; }

    /* ── Navbar ── */
    .navbar       { padding: 0 1rem; }
    .navbar-brand { font-size: 0.95rem; }
    .user-name    { display: none; }
    .user-badge   { padding: 0.3rem 0.6rem; }

    /* ── Auth / game cards ── */
    .card        { padding: 1.5rem 1.25rem; border-radius: 14px; }
    .title       { font-size: 1.5rem; }
    .winner-text { font-size: 1.4rem; }

    /* ── Game ── */
    .game-container { padding: 1rem; gap: 1rem; }
    .letter-box     { width: 42px; height: 42px; font-size: 1.35rem; border-radius: 8px; }
    .log-box        { height: 140px; font-size: 0.9rem; }
    .chat-wrapper button { padding: 0.8rem 1rem; }
  }

  /* Very small phones ≤ 360px */
  @media (max-width: 360px) {
    .feature-grid { grid-template-columns: 1fr; }
    .hero-stats   { grid-template-columns: 1fr 1fr; }
    .letter-box   { width: 36px; height: 36px; font-size: 1.1rem; }
    .play-card-cta { padding: 0.45rem 1rem; font-size: 0.82rem; }
  }

  /* ════════════════════════════════
     Avatar image
  ════════════════════════════════ */
  .avatar-img {
    width: 100%;
    height: 100%;
    border-radius: 50%;
    object-fit: cover;
  }

  .hero-avatar-img {
    width: 76px;
    height: 76px;
    border-radius: 50%;
    object-fit: cover;
    border: 3px solid rgba(255,255,255,0.45);
  }

  .player-card-avatar-img {
    width: 72px;
    height: 72px;
    border-radius: 50%;
    object-fit: cover;
  }

  /* ════════════════════════════════
     Profile section on dashboard
  ════════════════════════════════ */
  .profile-section {
    background: white;
    border-radius: 20px;
    padding: 1.5rem;
    box-shadow: 0 2px 12px rgba(120,60,220,0.07);
    margin-bottom: 2rem;
    display: flex;
    align-items: center;
    gap: 1.5rem;
    flex-wrap: wrap;
  }

  .profile-pfp-preview {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    color: white;
    font-size: 1.8rem;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    overflow: hidden;
    border: 3px solid #d8b4fe;
  }

  .profile-pfp-preview img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .profile-form {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    min-width: 200px;
  }

  .profile-form-row {
    display: flex;
    gap: 0.5rem;
    align-items: center;
  }

  .profile-form-row input {
    flex: 1;
    width: auto;
  }

  .profile-form-row button {
    width: auto;
    padding: 0.8rem 1.25rem;
    flex-shrink: 0;
  }

  /* ════════════════════════════════
     Countdown page
  ════════════════════════════════ */
  @keyframes count-bounce {
    0%, 100% { transform: scale(1); }
    50%       { transform: scale(1.15); }
  }

  .countdown-layout {
    min-height: 100vh;
    background: linear-gradient(135deg, #5b21b6, #7c3aed, #a855f7);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1.5rem;
    padding: 2rem;
    box-sizing: border-box;
  }

  .countdown-title {
    font-size: 2rem;
    font-weight: 900;
    color: white;
    margin: 0;
    letter-spacing: -0.01em;
  }

  .countdown-players {
    display: flex;
    align-items: center;
    gap: 2.5rem;
    flex-wrap: wrap;
    justify-content: center;
  }

  .countdown-player {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.65rem;
  }

  .countdown-avatar {
    width: 100px;
    height: 100px;
    border-radius: 50%;
    background: rgba(255,255,255,0.2);
    border: 4px solid rgba(255,255,255,0.5);
    color: white;
    font-size: 3rem;
    font-weight: 900;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    animation: float 3s ease-in-out infinite;
  }

  .countdown-avatar.you { border-color: #fde68a; animation-delay: 0s; }
  .countdown-avatar.opponent { animation-delay: 0.3s; }

  .countdown-avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .countdown-name {
    color: white;
    font-size: 1.05rem;
    font-weight: 700;
    margin: 0;
  }

  .countdown-you-tag {
    color: #fde68a;
    font-size: 0.78rem;
    font-weight: 600;
    margin: 0;
  }

  .countdown-vs {
    width: 56px;
    height: 56px;
    border-radius: 50%;
    background: rgba(255,255,255,0.15);
    border: 2px solid rgba(255,255,255,0.4);
    color: white;
    font-size: 1rem;
    font-weight: 900;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .countdown-number {
    font-size: 5rem;
    font-weight: 900;
    color: white;
    margin: 0;
    line-height: 1;
    animation: count-bounce 1s ease-in-out infinite;
    text-shadow: 0 4px 24px rgba(0,0,0,0.3);
  }

  .countdown-sub {
    color: rgba(255,255,255,0.75);
    font-size: 1rem;
    margin: 0;
  }

  @media (max-width: 600px) {
    .countdown-players { gap: 1.25rem; }
    .countdown-avatar  { width: 76px; height: 76px; font-size: 2.2rem; }
    .countdown-vs      { width: 42px; height: 42px; font-size: 0.85rem; }
    .countdown-number  { font-size: 4rem; }

    .profile-section   { flex-direction: column; align-items: flex-start; }
    .profile-form-row  { flex-direction: column; }
    .profile-form-row input  { width: 100%; }
    .profile-form-row button { width: 100%; }
  }
</style>

<!-- ══════════════════════════════════════
     LOGIN
═══════════════════════════════════════ -->
{#if page === "login" && !isWaiting && !gameStarted}
  <div class="auth-container">
    <div class="card">
      <p class="title">Word Guessing Game</p>
      <p class="subtitle">Sign in to play with friends</p>

      <div class="field-wrapper">
        <div class="input-group">
          <span class="input-label">Username</span>
          <input type="text" bind:value={loginUsername} placeholder="Enter username" />
        </div>
        <div class="input-group">
          <span class="input-label">Password</span>
          <input type="password" bind:value={loginPassword} placeholder="Enter password"
            on:keydown={(e) => e.key === 'Enter' && handleLogin()} />
        </div>

        <label class="remember-row">
          <input type="checkbox" bind:checked={rememberMe} />
          Remember me
        </label>

        {#if loginError}<p class="error-text">{loginError}</p>{/if}

        <button on:click={handleLogin} disabled={loginLoading}>
          {loginLoading ? "Logging in..." : "Log In"}
        </button>
      </div>

      <div class="divider">or</div>
      <button class="btn-ghost" on:click={handleGuest}>Play as Guest</button>

      <div class="link-row">
        Don't have an account?
        <button class="link-btn" on:click={() => { page = 'register'; regError = ''; regSuccess = false; }}>
          Register
        </button>
      </div>
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     REGISTER
═══════════════════════════════════════ -->
{#if page === "register"}
  <div class="auth-container">
    <div class="card">
      <p class="title">Create Account</p>
      <p class="subtitle">Join and start guessing!</p>

      <div class="field-wrapper">
        <div class="input-group">
          <span class="input-label">Username</span>
          <input type="text" bind:value={regUsername} placeholder="Choose a username" />
        </div>
        <div class="input-group">
          <span class="input-label">Email</span>
          <input type="email" bind:value={regEmail} placeholder="your@email.com" />
        </div>
        <div class="input-group">
          <span class="input-label">Password</span>
          <input type="password" bind:value={regPassword} placeholder="Create password" />
        </div>
        <div class="input-group">
          <span class="input-label">Confirm Password</span>
          <input type="password" bind:value={regConfirm} placeholder="Repeat password"
            on:keydown={(e) => e.key === 'Enter' && handleRegister()} />
        </div>

        {#if regError}<p class="error-text">{regError}</p>{/if}
        {#if regSuccess}<p class="success-text">✔ Account created! Redirecting...</p>{/if}

        <button on:click={handleRegister} disabled={regLoading}>
          {regLoading ? "Creating account..." : "Create Account"}
        </button>
      </div>

      <div class="link-row">
        Already have an account?
        <button class="link-btn" on:click={() => { page = 'login'; regError = ''; }}>Log In</button>
      </div>
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     DASHBOARD
═══════════════════════════════════════ -->
{#if page === "dashboard" && !isWaiting && !gameStarted}
  <div class="dashboard-layout">

    <!-- Navbar -->
    <nav class="navbar">
      <span class="navbar-brand">Word Guessing Game</span>
      <div class="navbar-right">
        <div class="user-badge">
          <div class="avatar">
            {#if profilePicUrl}
              <img class="avatar-img" src={profilePicUrl} alt={text} />
            {:else}
              {text.charAt(0).toUpperCase()}
            {/if}
          </div>
          <span class="user-name">{text}</span>
        </div>
        <button class="btn-navbar btn-sm" on:click={handleLogout}>Log Out</button>
      </div>
    </nav>

    <!-- Hero banner -->
    <div class="hero-banner">
      <div class="hero-avatar-wrap">
        {#if profilePicUrl}
          <img class="hero-avatar-img" src={profilePicUrl} alt={text} />
        {:else}
          <div class="hero-avatar-ring">{text.charAt(0).toUpperCase()}</div>
        {/if}
        <div class="online-dot"></div>
      </div>
      <div class="hero-text">
        <h1 class="hero-title">Hey, {text}!</h1>
        <p class="hero-subtitle">Ready to guess some words today?</p>
        <span class="rank-badge">🌱 Rookie</span>
      </div>
      <div class="hero-stats">
        <div class="hero-stat">
          <span class="hero-stat-icon">🎮</span>
          <span class="hero-stat-value">0</span>
          <span class="hero-stat-label">Games</span>
        </div>
        <div class="hero-stat">
          <span class="hero-stat-icon">🏆</span>
          <span class="hero-stat-value">0</span>
          <span class="hero-stat-label">Wins</span>
        </div>
        <div class="hero-stat">
          <span class="hero-stat-icon">📊</span>
          <span class="hero-stat-value">0%</span>
          <span class="hero-stat-label">Win Rate</span>
        </div>
        <div class="hero-stat">
          <span class="hero-stat-icon">🔥</span>
          <span class="hero-stat-value">0</span>
          <span class="hero-stat-label">Streak</span>
        </div>
      </div>
    </div>

    <!-- Main content -->
    <main class="dashboard-main">

      <!-- Play section -->
      <p class="section-title">Play</p>
      <div class="play-grid">

        <!-- Quick match -->
        <div class="play-card play-card-primary" role="button" tabindex="0"
          on:click={() => (page = 'lobby')}
          on:keydown={(e) => e.key === 'Enter' && (page = 'lobby')}>
          <div class="play-card-watermark">⚡</div>
          <div class="play-card-icon">⚡</div>
          <p class="play-card-title">Quick Match</p>
          <p class="play-card-desc">Get matched with a random opponent instantly and start guessing.</p>
          <button class="play-card-cta">Play Now →</button>
        </div>

        <!-- Private game -->
        <div class="play-card play-card-secondary" role="button" tabindex="0"
          on:click={() => { connectMode = 'create-private'; page = 'private'; }}
          on:keydown={(e) => e.key === 'Enter' && (connectMode = 'create-private', page = 'private')}>
          <div class="play-card-watermark">🔒</div>
          <div class="play-card-icon">🔒</div>
          <p class="play-card-title">Private Game</p>
          <p class="play-card-desc">Invite a specific friend using a one-time invite link.</p>
          <button class="play-card-cta">Create Lobby →</button>
        </div>

      </div>

      <!-- Profile section -->
      <p class="section-title">Profile</p>
      <div class="profile-section">
        <div class="profile-pfp-preview">
          {#if profilePicUrl}
            <img src={profilePicUrl} alt={text} />
          {:else}
            {text.charAt(0).toUpperCase()}
          {/if}
        </div>
        <div class="profile-form">
          <span class="input-label">Profile Picture URL</span>
          <div class="profile-form-row">
            <input type="url" bind:value={avatarInput} placeholder="https://example.com/my-photo.jpg" />
            <button on:click={saveAvatar} disabled={avatarSaving}>
              {avatarSaving ? "Saving..." : "Save"}
            </button>
          </div>
          {#if avatarSaved}<p class="success-text" style="font-size:0.85rem">✔ Avatar updated!</p>{/if}
        </div>
      </div>

      <!-- Coming soon -->
      <p class="section-title">Coming Soon</p>
      <div class="feature-grid">

        <div class="feature-card dimmed">
          <span class="badge-soon">Soon</span>
          <div class="card-icon">🏆</div>
          <p class="card-title">Leaderboard</p>
          <p class="card-desc">See who's on top of the global rankings.</p>
        </div>

        <div class="feature-card dimmed">
          <span class="badge-soon">Soon</span>
          <div class="card-icon">📜</div>
          <p class="card-title">Game History</p>
          <p class="card-desc">Review your past matches and results.</p>
        </div>

        <div class="feature-card dimmed">
          <span class="badge-soon">Soon</span>
          <div class="card-icon">👥</div>
          <p class="card-title">Friends</p>
          <p class="card-desc">Add friends and challenge them directly.</p>
        </div>

      </div>
    </main>

  </div>
{/if}

<!-- ══════════════════════════════════════
     LOBBY (find game)
═══════════════════════════════════════ -->
{#if page === "lobby" && !isWaiting && !gameStarted}
  <div class="lobby-layout">

    <!-- Header -->
    <div class="lobby-header">
      <button class="lobby-back" on:click={() => isGuest ? (page = 'login', isGuest = false) : (page = 'dashboard')}>
        ← Back
      </button>
      <span class="lobby-header-title">Quick Match</span>
      <span class="lobby-mode-badge">⚡ Public</span>
    </div>

    <!-- Arena -->
    <div class="lobby-arena">

      <!-- Player card -->
      <div class="player-card player-card-you">
        <div class="player-card-avatar">
          {#if profilePicUrl && !isGuest}
            <img class="player-card-avatar-img" src={profilePicUrl} alt={text} />
          {:else if isGuest}
            {guestName ? guestName.charAt(0).toUpperCase() : '?'}
          {:else}
            {text.charAt(0).toUpperCase()}
          {/if}
        </div>
        <p class="player-card-name">{isGuest ? (guestName || 'Guest') : text}</p>
        <span class="player-card-tag">{isGuest ? '👤 Guest' : '🌱 Rookie'}</span>
      </div>

      <!-- VS divider -->
      <div class="vs-divider">
        <div class="vs-ring">VS</div>
        <div class="vs-line"></div>
      </div>

      <!-- Opponent card -->
      <div class="player-card player-card-opponent">
        <div class="player-card-avatar avatar-unknown">?</div>
        <p class="player-card-name">???</p>
        <span class="player-card-tag">Waiting...</span>
      </div>

    </div>

    <!-- Guest name input -->
    {#if isGuest}
      <div class="lobby-name-input">
        <input type="text" bind:value={guestName} placeholder="Enter your display name..."
          on:keydown={(e) => e.key === 'Enter' && connectToHub()} />
      </div>
    {/if}

    <!-- CTA -->
    <div class="lobby-cta">
      <button class="find-match-btn" on:click={connectToHub}>
        <span class="find-match-pulse"></span>
        Find Match
      </button>
      <p class="lobby-hint">You'll be matched with a random opponent</p>
    </div>

    <!-- Rules strip -->
    <div class="lobby-rules">
      <div class="rule-item"><span class="rule-icon">💡</span><span>Guess letters or the full word</span></div>
      <div class="rule-item"><span class="rule-icon">🔄</span><span>Players take turns guessing</span></div>
      <div class="rule-item"><span class="rule-icon">🏆</span><span>First to guess the word wins</span></div>
    </div>

  </div>
{/if}

<!-- ══════════════════════════════════════
     PRIVATE LOBBY (creator)
═══════════════════════════════════════ -->
{#if page === "private" && !isWaiting && !gameStarted}
  <div class="game-container">
    <div class="card">
      <p class="title">Private Game</p>
      <p class="subtitle">Create an invite link and share it with a friend.</p>
      <button on:click={connectToHub}>Generate Invite Link</button>
      <button class="btn-ghost btn-sm" on:click={() => { page = 'dashboard'; connectMode = 'public'; }}>← Back</button>
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     JOIN PRIVATE LOBBY (joiner via link)
═══════════════════════════════════════ -->
{#if page === "join-private" && !isWaiting && !gameStarted}
  <div class="auth-container">
    <div class="card">
      <p class="title">You're Invited!</p>
      <p class="subtitle">Join a private Word Guessing Game</p>

      {#if text && !isGuest}
        <!-- Already logged in -->
        <div class="user-badge">
          <div class="avatar">{text.charAt(0).toUpperCase()}</div>
          <span>{text}</span>
        </div>
        {#if privateLobbyError}<p class="error-text">{privateLobbyError}</p>{/if}
        <button on:click={() => { connectMode = 'join-private'; connectToHub(); }}>Join Game</button>
        <div class="divider">or</div>
        <button class="btn-ghost" on:click={() => {
          localStorage.removeItem("token");
          localStorage.removeItem("username");
          localStorage.removeItem("refreshToken");
          text = ""; isGuest = false;
        }}>Use a different account</button>

      {:else if isGuest}
        <!-- Guest mode — show name input -->
        <p class="subtitle">Playing as Guest</p>
        <div class="field-wrapper">
          <div class="input-group">
            <span class="input-label">Display Name</span>
            <input type="text" bind:value={guestName} placeholder="Enter your name..."
              on:keydown={(e) => e.key === 'Enter' && connectMode === 'join-private' && connectToHub()} />
          </div>
        </div>
        {#if privateLobbyError}<p class="error-text">{privateLobbyError}</p>{/if}
        <button on:click={() => { connectMode = 'join-private'; connectToHub(); }}>Join Game</button>
        <button class="btn-ghost btn-sm" on:click={() => { isGuest = false; }}>← Back</button>

      {:else}
        <!-- Not logged in — show options -->
        <button on:click={() => { page = 'login'; }}>Log In to Join</button>
        <button on:click={() => { page = 'register'; }}>Create Account</button>
        <div class="divider">or</div>
        <button class="btn-ghost" on:click={() => { isGuest = true; }}>Play as Guest</button>
      {/if}
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     MATCH FOUND COUNTDOWN
═══════════════════════════════════════ -->
{#if page === "countdown" && matchData}
  <div class="countdown-layout">
    <p class="countdown-title">Match Found!</p>

    <div class="countdown-players">
      <!-- You -->
      <div class="countdown-player">
        <div class="countdown-avatar you">
          {#if matchData.player1Pfp}
            <img src={matchData.player1Pfp} alt={matchData.player1} />
          {:else}
            {matchData.player1?.charAt(0).toUpperCase()}
          {/if}
        </div>
        <p class="countdown-name">{matchData.player1}</p>
        {#if matchData.player1 === text}<p class="countdown-you-tag">You</p>{/if}
      </div>

      <div class="countdown-vs">VS</div>

      <!-- Opponent -->
      <div class="countdown-player">
        <div class="countdown-avatar opponent">
          {#if matchData.player2Pfp}
            <img src={matchData.player2Pfp} alt={matchData.player2} />
          {:else}
            {matchData.player2?.charAt(0).toUpperCase()}
          {/if}
        </div>
        <p class="countdown-name">{matchData.player2}</p>
        {#if matchData.player2 === text}<p class="countdown-you-tag">You</p>{/if}
      </div>
    </div>

    <p class="countdown-number">{matchCountdown > 0 ? matchCountdown : '🎮'}</p>
    <p class="countdown-sub">Game starts in {matchCountdown > 0 ? matchCountdown + ' second' + (matchCountdown !== 1 ? 's' : '') : 'now'}...</p>
  </div>
{/if}

<!-- ══════════════════════════════════════
     WAITING FOR OPPONENT
═══════════════════════════════════════ -->
{#if isWaiting && !gameStarted}
  <div class="game-container">
    <div class="card">
      {#if privateLobbyLink}
        <p class="status-text">🔒 Waiting for your friend...</p>
        <p class="subtitle">Playing as <strong>{text}</strong></p>
        <p class="input-label" style="text-align:center">Share this link:</p>
        <div class="invite-link-row">
          <span class="invite-link-text">{privateLobbyLink}</span>
          <button class="btn-sm btn-icon" on:click={() => {
            navigator.clipboard.writeText(privateLobbyLink);
            linkCopied = true;
            setTimeout(() => linkCopied = false, 2000);
          }}>{linkCopied ? '✔ Copied!' : 'Copy'}</button>
        </div>
        {#if linkExpiresIn > 0}
          <p class="subtitle" style="color: {linkExpiresIn < 60 ? '#dc2626' : '#9ca3af'}">
            Link expires in {formatCountdown(linkExpiresIn)}
          </p>
        {:else}
          <p class="error-text">Link expired — cancel and create a new one.</p>
        {/if}
        <button class="btn-ghost btn-sm" on:click={cancelWaiting}>Cancel</button>
      {:else}
        <p class="status-text">🔍 Looking for an opponent...</p>
        <p class="subtitle">Playing as <strong>{text}</strong></p>
        <button class="btn-ghost btn-sm" on:click={cancelWaiting}>Cancel</button>
      {/if}
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     GAME IN PROGRESS
═══════════════════════════════════════ -->
{#if gameStarted && page === "game"}
  <div class="game-container">

    {#if !isWon}
      <p class="status-text">🎮 Game Started!</p>

      {#if gameInformation}
        <div class="word-container">
          {#each letters as letter}
            <div class="letter-box">{letter}</div>
          {/each}
        </div>
      {/if}

      <div class="log-box">
        {#each logMessages as msg}
          <div class="log-line">{msg}</div>
        {/each}
      </div>

      {#if currentTurn === text}
        <div class="chat-wrapper">
          <input type="text" bind:value={chatMessage} placeholder="Guess a letter..."
            on:keydown={(e) => e.key === 'Enter' && sendChat()} />
          <button on:click={sendChat}>Send</button>
        </div>
      {:else}
        <p class="status-text">✋ Waiting for opponent to guess...</p>
      {/if}

    {:else}
      <!-- WINNER SCREEN -->
      <div class="card">
        <p class="winner-text">{winnerMessage}</p>
        <p class="rematch-info">{rematchCount}/2 players want a rematch</p>

        {#if !hasVotedRematch}
          <button on:click={sendRematch}>🔁 Rematch</button>
        {:else}
          <p class="voted-text">✔ You voted for a rematch — waiting for opponent...</p>
        {/if}
      </div>
    {/if}

  </div>
{/if}

<div class="version-label">{version}</div>
