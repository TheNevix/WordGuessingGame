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
  let page = (_storedToken && !isTokenExpired(_storedToken)) ? "dashboard" : "login";
  let text = localStorage.getItem("username") ?? "";

  let rememberMe = false;

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
      page = "dashboard";
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
        page = "login";
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

    connection.on("Rematch", () => { rematchCount++; });

    connection.on("Disconnected", () => {
      isWaiting = true;
      gameStarted = false;
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
      await connection.start();
      connection.invoke("RegisterName", text);
      isWaiting = true;
    } catch (err) {
      console.error("Connection failed:", err);
      alert("Could not connect to hub.");
    }
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
</script>

<style>
  :global(body) {
    margin: 0;
    background: linear-gradient(135deg, #f8f6ff 0%, #ede8ff 50%, #e0d4ff 100%);
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
  .dashboard-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
  }

  /* Navbar */
  .navbar {
    background: white;
    border-bottom: 1px solid #ede9fe;
    padding: 0 2rem;
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    box-shadow: 0 2px 12px rgba(120, 60, 220, 0.07);
    position: sticky;
    top: 0;
    z-index: 10;
  }

  .navbar-brand {
    font-size: 1.2rem;
    font-weight: 700;
    background: linear-gradient(135deg, #7c3aed, #a855f7);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
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
    background: #ede9fe;
    border-radius: 999px;
    padding: 0.35rem 0.9rem;
    font-size: 0.9rem;
    font-weight: 600;
    color: #6d28d9;
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
  }

  /* Dashboard main area */
  .dashboard-main {
    flex: 1;
    padding: 2.5rem 2rem;
    max-width: 960px;
    width: 100%;
    margin: 0 auto;
    box-sizing: border-box;
  }

  .dashboard-greeting {
    margin: 0 0 0.4rem;
    font-size: 1.7rem;
    font-weight: 700;
    color: #3b0764;
  }

  .dashboard-sub {
    margin: 0 0 2rem;
    color: #9ca3af;
    font-size: 1rem;
  }

  .section-title {
    font-size: 0.8rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: #a78bfa;
    margin: 0 0 1rem;
  }

  /* Feature card grid */
  .feature-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
    gap: 1.25rem;
  }

  .feature-card {
    background: white;
    border-radius: 16px;
    padding: 1.5rem;
    box-shadow: 0 4px 16px rgba(120, 60, 220, 0.08);
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    position: relative;
    border: 2px solid transparent;
    transition: border-color 0.2s, box-shadow 0.2s, transform 0.15s;
  }

  .feature-card.active {
    cursor: pointer;
    border-color: #d8b4fe;
  }

  .feature-card.active:hover {
    border-color: #7c3aed;
    box-shadow: 0 8px 28px rgba(120, 60, 220, 0.18);
    transform: translateY(-2px);
  }

  .feature-card.dimmed {
    opacity: 0.6;
  }

  .card-icon {
    font-size: 2rem;
    line-height: 1;
  }

  .card-title {
    font-size: 1rem;
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
    button:hover          { opacity: 1; transform: none; }
    .btn-ghost:hover      { background: transparent; }
    .feature-card.active:hover {
      transform: none;
      box-shadow: 0 4px 16px rgba(120, 60, 220, 0.08);
      border-color: #d8b4fe;
    }
  }

  /* ════════════════════════════════
     Mobile — layout fixes ≤ 480px
  ════════════════════════════════ */
  @media (max-width: 480px) {
    /* Navbar */
    .navbar        { padding: 0 1rem; gap: 0.5rem; }
    .navbar-brand  { font-size: 0.95rem; }
    .user-name     { display: none; }          /* show avatar only */
    .user-badge    { padding: 0.3rem 0.5rem; }

    /* Auth / game cards */
    .card { padding: 1.5rem 1.25rem; border-radius: 14px; }

    /* Titles */
    .title       { font-size: 1.5rem; }
    .winner-text { font-size: 1.4rem; }

    /* Dashboard */
    .dashboard-main     { padding: 1.25rem 1rem; }
    .dashboard-greeting { font-size: 1.3rem; }
    .feature-grid       { grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .feature-card       { padding: 1rem 0.85rem; }
    .card-desc          { font-size: 0.8rem; }

    /* Game container */
    .game-container { padding: 1rem; gap: 1rem; }

    /* Letter boxes — smaller so long words fit */
    .letter-box {
      width: 42px;
      height: 42px;
      font-size: 1.35rem;
      border-radius: 8px;
    }

    /* Log box */
    .log-box { height: 140px; font-size: 0.9rem; }

    /* Chat */
    .chat-wrapper button { padding: 0.8rem 1rem; }
  }

  /* Very small phones ≤ 360px */
  @media (max-width: 360px) {
    .feature-grid { grid-template-columns: 1fr; }
    .letter-box   { width: 38px; height: 38px; font-size: 1.2rem; }
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
          <div class="avatar">{text.charAt(0).toUpperCase()}</div>
          <span class="user-name">{text}</span>
        </div>
        <button class="btn-ghost btn-sm btn-icon" on:click={handleLogout}>Log Out</button>
      </div>
    </nav>

    <!-- Main content -->
    <main class="dashboard-main">
      <h1 class="dashboard-greeting">Hey, {text}! 👋</h1>
      <p class="dashboard-sub">What do you want to do today?</p>

      <p class="section-title">Features</p>
      <div class="feature-grid">

        <!-- Play — active -->
        <div class="feature-card active" on:click={() => (page = 'lobby')} role="button" tabindex="0"
          on:keydown={(e) => e.key === 'Enter' && (page = 'lobby')}>
          <div class="card-icon">🎮</div>
          <p class="card-title">Play a Game</p>
          <p class="card-desc">Find an opponent and start guessing words.</p>
        </div>

        <!-- Leaderboard — coming soon -->
        <div class="feature-card dimmed">
          <span class="badge-soon">Soon</span>
          <div class="card-icon">🏆</div>
          <p class="card-title">Leaderboard</p>
          <p class="card-desc">See who's on top of the rankings.</p>
        </div>

        <!-- Game History — coming soon -->
        <div class="feature-card dimmed">
          <span class="badge-soon">Soon</span>
          <div class="card-icon">📜</div>
          <p class="card-title">Game History</p>
          <p class="card-desc">Review your past matches and results.</p>
        </div>

        <!-- Profile — coming soon -->
        <div class="feature-card dimmed">
          <span class="badge-soon">Soon</span>
          <div class="card-icon">👤</div>
          <p class="card-title">Profile</p>
          <p class="card-desc">Edit your username, avatar, and settings.</p>
        </div>

      </div>
    </main>

  </div>
{/if}

<!-- ══════════════════════════════════════
     LOBBY (find game)
═══════════════════════════════════════ -->
{#if page === "lobby" && !isWaiting && !gameStarted}
  <div class="game-container">
    <div class="card">
      <p class="title">Game Lobby</p>

      {#if !isGuest}
        <div class="user-badge">
          <div class="avatar">{text.charAt(0).toUpperCase()}</div>
          {text}
        </div>
        <p class="subtitle">Ready to play, {text}?</p>
        <button on:click={connectToHub}>Find a Game</button>
        <button class="btn-ghost btn-sm" on:click={() => (page = 'dashboard')}>← Back to Dashboard</button>
      {:else}
        <p class="subtitle">Playing as Guest — pick a display name</p>
        <div class="field-wrapper">
          <input type="text" bind:value={guestName} placeholder="Display name..."
            on:keydown={(e) => e.key === 'Enter' && connectToHub()} />
          <button on:click={connectToHub}>Find a Game</button>
        </div>
        <button class="btn-ghost btn-sm" on:click={() => { page = 'login'; isGuest = false; }}>
          ← Back to Login
        </button>
      {/if}
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     WAITING FOR OPPONENT
═══════════════════════════════════════ -->
{#if isWaiting && !gameStarted}
  <div class="game-container">
    <div class="card">
      <p class="status-text">🔍 Looking for an opponent...</p>
      <p class="subtitle">Playing as <strong>{text}</strong></p>
    </div>
  </div>
{/if}

<!-- ══════════════════════════════════════
     GAME IN PROGRESS
═══════════════════════════════════════ -->
{#if gameStarted}
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
