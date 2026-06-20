<script>
  import { onMount, onDestroy } from 'svelte';
  import { goto } from '$app/navigation';
  import { username, isGuest } from '$lib/stores.js';
  import { handleLogin } from '$lib/api.js';
  import { t } from '$lib/i18n.js';

  let loginUsername = "";
  let loginPassword = "";
  let loginError    = "";
  let loginNotice   = "";
  let loginLoading  = false;
  let rememberMe    = false;

  // Forgot password state
  let showForgot    = false;
  let forgotEmail   = "";
  let forgotError   = "";
  let forgotSuccess = false;
  let forgotLoading = false;

  async function doForgot() {
    if (!forgotEmail.trim()) { forgotError = "Vul je e-mailadres in."; return; }
    forgotError = "";
    forgotLoading = true;
    try {
      const res = await fetch('/api/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: forgotEmail.trim() })
      });
      forgotSuccess = true;
    } catch {
      forgotError = "Er ging iets mis. Probeer opnieuw.";
    } finally {
      forgotLoading = false;
    }
  }

  function openForgot() { showForgot = true; forgotEmail = ""; forgotError = ""; forgotSuccess = false; }
  function closeForgot() { showForgot = false; }

  async function doLogin() {
    if (!loginUsername.trim() || !loginPassword.trim()) {
      loginError = $t('login.error_fields');
      return;
    }
    loginError = "";
    loginNotice = "";
    loginLoading = true;
    try {
      await handleLogin(loginUsername, loginPassword, rememberMe);
    } catch (e) {
      // Unverified account: the backend just (re)sent a verification mail — show it as a
      // friendly notice rather than a hard error.
      if (e.code === 'email_not_verified') loginNotice = e.message;
      else loginError = e.message;
    } finally {
      loginLoading = false;
    }
  }

  function goGuest() {
    isGuest.set(true);
    username.set("");
    goto('/lobby');
  }

  // Word reveal animation
  const WORDS = ['KASTEEL', 'SPELEN', 'WINNEN', 'WOORDEN', 'KAMPEN', 'RAADSEL'];
  let wordIdx       = 0;
  let revealedCount = 0;
  let showGuessed   = false;
  let interval;

  onMount(() => {
    interval = setInterval(() => {
      if (showGuessed) {
        showGuessed   = false;
        wordIdx       = (wordIdx + 1) % WORDS.length;
        revealedCount = 0;
      } else if (revealedCount < WORDS[wordIdx].length) {
        revealedCount++;
      } else {
        showGuessed = true;
      }
    }, 480);
  });
  onDestroy(() => clearInterval(interval));

  $: demoWord    = WORDS[wordIdx];
  $: demoLetters = demoWord.split('').map((l, i) => ({ l, revealed: i < revealedCount }));

  const recentMatches = [
    { name: 'Woordenjager42', tier: '🥉', rp: '+18', won: true  },
    { name: 'Taalmeester21',  tier: '🥇', rp: '+25', won: true  },
    { name: 'VanDenBerg99',   tier: '🥈', rp: '−8',  won: false },
    { name: 'Raadselheld',    tier: '💎', rp: '+31', won: true  },
  ];
</script>

<div class="split">

  <!-- ── Left hero ── -->
  <div class="hero">
    <span class="fl" style="left:6%;top:8%;font-size:9rem;animation-duration:19s">W</span>
    <span class="fl" style="left:74%;top:4%;font-size:7rem;animation-duration:15s;animation-delay:5s">R</span>
    <span class="fl" style="left:50%;top:71%;font-size:12rem;animation-duration:23s;animation-delay:9s">A</span>
    <span class="fl" style="left:18%;top:78%;font-size:6rem;animation-duration:17s;animation-delay:3s">D</span>
    <span class="fl" style="left:88%;top:46%;font-size:8rem;animation-duration:21s;animation-delay:7s">W</span>

    <div class="hero-body">

      <div>
        <div class="brand-name">Raad het woord</div>
        <p class="brand-tag">Speel. Raad. Domineer.</p>
      </div>

      <div class="demo-box">
        <div class="demo-header">
          <span class="live-dot"></span>
          <span class="live-label">LIVE SPEL</span>
        </div>
        <div class="tiles">
          {#each demoLetters as { l, revealed }, i}
            <div class="tile {revealed ? 'on' : ''}" style="animation-delay:{i * 0.05}s">
              {revealed ? l : ''}
            </div>
          {/each}
        </div>
        {#if showGuessed}
          <p class="demo-result">GERADEN! ✓</p>
        {:else}
          <p class="demo-hint">
            {revealedCount === 0 ? 'Nieuw woord...' : `${revealedCount} / ${demoWord.length} letters gevonden`}
          </p>
        {/if}
      </div>


    </div>
  </div>

  <!-- ── Right form panel ── -->
  <div class="panel">
    <div class="panel-inner">

      {#if showForgot}

        <!-- ── Forgot password view ── -->
        <button class="back-btn" on:click={closeForgot}>← Terug</button>

        <h1 class="form-title">Wachtwoord vergeten</h1>
        <p class="form-sub">Vul je e-mailadres in en we sturen je een resetlink.</p>

        {#if forgotSuccess}
          <p class="form-success" style="margin-top:1.5rem">
            Als dit e-mailadres bekend is, ontvang je een e-mail met een resetlink.
          </p>
        {:else}
          <div class="fields" style="margin-top:1.75rem">
            <div class="field">
              <label class="flabel">E-mailadres</label>
              <input class="finput" type="email" bind:value={forgotEmail}
                placeholder="jouw@email.be"
                on:keydown={(e) => e.key === 'Enter' && doForgot()} />
            </div>
          </div>

          {#if forgotError}<p class="form-error">{forgotError}</p>{/if}

          <button class="btn-primary" on:click={doForgot} disabled={forgotLoading}>
            {forgotLoading ? 'Bezig...' : 'Resetlink sturen'}
          </button>
        {/if}

      {:else}

        <!-- ── Login view ── -->
        <h1 class="form-title">{$t('login.title')}</h1>
        <p class="form-sub">{$t('login.subtitle')}</p>

        <div class="fields">
          <div class="field">
            <label class="flabel">{$t('login.username')}</label>
            <input class="finput" type="text" bind:value={loginUsername}
              placeholder={$t('login.username_placeholder')}
              on:keydown={(e) => e.key === 'Enter' && doLogin()} />
          </div>
          <div class="field">
            <label class="flabel">{$t('login.password')}</label>
            <input class="finput" type="password" bind:value={loginPassword}
              placeholder={$t('login.password_placeholder')}
              on:keydown={(e) => e.key === 'Enter' && doLogin()} />
          </div>
        </div>

        <div class="remember-row">
          <label class="remember">
            <input type="checkbox" bind:checked={rememberMe} />
            <span>{$t('login.remember_me')}</span>
          </label>
          <button class="link-btn" on:click={openForgot}>Wachtwoord vergeten?</button>
        </div>

        {#if loginNotice}<p class="form-notice">📧 {loginNotice}</p>{/if}
        {#if loginError}<p class="form-error">{loginError}</p>{/if}

        <button class="btn-primary" on:click={doLogin} disabled={loginLoading}>
          {loginLoading ? $t('login.btn_loading') : $t('login.btn')}
        </button>

        <div class="sep"><span>of</span></div>

        <button class="btn-outline" on:click={goGuest}>{$t('login.guest')}</button>

        <p class="switch-row">
          {$t('login.no_account')}
          <button class="link-btn" on:click={() => goto('/registreren')}>{$t('login.register')} →</button>
        </p>

      {/if}

    </div>
  </div>

</div>

<style>
  .split {
    display: flex;
    min-height: 100vh;
    background: #0d0820;
  }

  /* ── Hero ── */
  .hero {
    flex: 1;
    position: relative;
    overflow: hidden;
    background: linear-gradient(150deg, #0d0820 0%, #1a0535 55%, #0a1428 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 3rem 2.5rem;
  }

  .fl {
    position: absolute;
    font-weight: 900;
    color: rgba(167,139,250,0.05);
    animation: floatBob linear infinite;
    user-select: none;
    pointer-events: none;
    line-height: 1;
  }
  @keyframes floatBob {
    0%,100% { transform: translateY(0) rotate(0deg); }
    30%      { transform: translateY(-28px) rotate(4deg); }
    70%      { transform: translateY(18px) rotate(-3deg); }
  }

  .hero-body {
    position: relative;
    z-index: 1;
    display: flex;
    flex-direction: column;
    gap: 2rem;
    max-width: 420px;
    width: 100%;
  }

  .brand-name {
    font-size: 2.8rem;
    font-weight: 900;
    letter-spacing: -0.01em;
    color: #fff;
    text-shadow: 0 0 50px rgba(124,58,237,0.55);
  }
  .brand-tag {
    font-size: 0.98rem;
    color: rgba(167,139,250,0.65);
    margin: 0.3rem 0 0;
    letter-spacing: 0.04em;
  }

  /* Demo box */
  .demo-box {
    background: rgba(255,255,255,0.04);
    border: 1px solid rgba(167,139,250,0.18);
    border-radius: 16px;
    padding: 1.25rem 1.5rem;
  }
  .demo-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1rem;
  }
  .live-dot {
    width: 8px; height: 8px;
    border-radius: 50%;
    background: #4ade80;
    box-shadow: 0 0 8px #4ade80;
    animation: blink 1.4s ease-in-out infinite;
  }
  @keyframes blink {
    0%,100% { opacity: 1; }
    50%      { opacity: 0.3; }
  }
  .live-label {
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.12em;
    color: rgba(255,255,255,0.35);
  }
  .tiles {
    display: flex;
    gap: 0.4rem;
    flex-wrap: nowrap;
  }
  .tile {
    width: 42px; height: 42px;
    border: 2px solid rgba(255,255,255,0.1);
    border-radius: 8px;
    background: rgba(255,255,255,0.03);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.1rem;
    font-weight: 800;
    color: transparent;
    transition: all 0.18s;
  }
  .tile.on {
    background: rgba(124,58,237,0.35);
    border-color: rgba(167,139,250,0.55);
    color: #e9d5ff;
    transform: scale(1.07);
    animation: tileFlip 0.28s ease-out;
  }
  @keyframes tileFlip {
    0%   { transform: scale(0.6) rotateY(90deg); opacity: 0.3; }
    100% { transform: scale(1.07) rotateY(0deg); opacity: 1; }
  }
  .demo-result {
    margin: 0.8rem 0 0;
    font-size: 0.9rem;
    font-weight: 700;
    color: #4ade80;
    animation: popIn 0.3s ease-out;
  }
  .demo-hint {
    margin: 0.8rem 0 0;
    font-size: 0.76rem;
    color: rgba(255,255,255,0.28);
  }
  @keyframes popIn {
    0%   { transform: scale(0.8); opacity: 0; }
    60%  { transform: scale(1.05); }
    100% { transform: scale(1); opacity: 1; }
  }

  /* Feed */
  .feed-label {
    font-size: 0.63rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    color: rgba(255,255,255,0.28);
    margin: 0 0 0.55rem;
  }
  .feed-row {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(255,255,255,0.05);
    animation: slideIn 0.4s ease-out both;
  }
  .feed-row:last-child { border-bottom: none; }
  @keyframes slideIn {
    from { transform: translateX(-14px); opacity: 0; }
    to   { transform: translateX(0); opacity: 1; }
  }
  .feed-tier { font-size: 1rem; }
  .feed-name { flex: 1; font-size: 0.84rem; font-weight: 600; color: rgba(255,255,255,0.75); }
  .feed-badge {
    font-size: 0.68rem; font-weight: 800;
    padding: 0.15rem 0.5rem;
    border-radius: 999px;
  }
  .feed-badge.win  { background: rgba(74,222,128,0.15); color: #4ade80; }
  .feed-badge.loss { background: rgba(248,113,113,0.15); color: #f87171; }
  .feed-rp { font-size: 0.8rem; font-weight: 700; min-width: 50px; text-align: right; }
  .feed-rp.pos { color: #4ade80; }
  .feed-rp.neg { color: #f87171; }

  /* ── Panel ── */
  .panel {
    width: 520px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(255,255,255,0.015);
    border-left: 1px solid rgba(255,255,255,0.07);
    padding: 2.5rem 2.5rem;
    overflow-y: auto;
  }
  .panel-inner {
    width: 100%;
    max-width: 420px;
    display: flex;
    flex-direction: column;
    align-items: stretch;
  }

  .form-title {
    font-size: 1.85rem;
    font-weight: 800;
    color: #f1f5f9;
    margin: 0 0 0.3rem;
  }
  .form-sub {
    font-size: 0.88rem;
    color: rgba(255,255,255,0.38);
    margin: 0 0 1.75rem;
  }

  .fields { display: flex; flex-direction: column; gap: 1rem; }
  .field  { display: flex; flex-direction: column; gap: 0.35rem; }
  .flabel {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.07em;
    color: rgba(255,255,255,0.42);
  }
  .finput {
    background: rgba(255,255,255,0.06);
    border: 1px solid rgba(255,255,255,0.1);
    border-radius: 10px;
    padding: 0.8rem 1rem;
    color: #f1f5f9;
    font-size: 0.95rem;
    outline: none;
    transition: border-color 0.2s;
    width: 100%;
    box-sizing: border-box;
    font-family: inherit;
  }
  .finput:focus { border-color: rgba(124,58,237,0.6); }
  .finput::placeholder { color: rgba(255,255,255,0.18); }

  .remember-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-top: 1rem;
  }

  .remember {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.84rem;
    color: rgba(255,255,255,0.45);
    cursor: pointer;
    white-space: nowrap;
  }

  .back-btn {
    background: none;
    border: none;
    color: rgba(255,255,255,0.35);
    font-size: 0.82rem;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
    margin-bottom: 1.75rem;
    font-family: inherit;
    text-align: left;
  }
  .back-btn:hover { color: rgba(255,255,255,0.65); }

  .form-error {
    font-size: 0.84rem;
    color: #f87171;
    margin: 0.8rem 0 0;
  }

  .form-notice {
    font-size: 0.84rem;
    color: #4ade80;
    background: rgba(74,222,128,0.08);
    border: 1px solid rgba(74,222,128,0.2);
    border-radius: 10px;
    padding: 0.7rem 0.9rem;
    margin: 0.8rem 0 0;
    line-height: 1.4;
  }

  .btn-primary {
    width: 100%;
    margin-top: 1.25rem;
    padding: 0.9rem;
    background: #7c3aed;
    border: none;
    border-radius: 12px;
    color: white;
    font-size: 1rem;
    font-weight: 700;
    cursor: pointer;
    transition: background 0.15s, transform 0.1s, box-shadow 0.15s;
    font-family: inherit;
    letter-spacing: 0.02em;
  }
  .btn-primary:hover:not(:disabled) {
    background: #6d28d9;
    transform: translateY(-1px);
    box-shadow: 0 6px 24px rgba(124,58,237,0.4);
  }
  .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }

  .sep {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin: 1.25rem 0;
    color: rgba(255,255,255,0.2);
    font-size: 0.78rem;
  }
  .sep::before, .sep::after {
    content: '';
    flex: 1;
    height: 1px;
    background: rgba(255,255,255,0.07);
  }

  .btn-outline {
    width: 100%;
    padding: 0.8rem;
    background: transparent;
    border: 1px solid rgba(255,255,255,0.12);
    border-radius: 12px;
    color: rgba(255,255,255,0.55);
    font-size: 0.9rem;
    font-weight: 600;
    cursor: pointer;
    transition: border-color 0.15s, color 0.15s;
    font-family: inherit;
  }
  .btn-outline:hover { border-color: rgba(255,255,255,0.25); color: rgba(255,255,255,0.85); }

  .switch-row {
    margin: 1.5rem 0 0;
    font-size: 0.84rem;
    color: rgba(255,255,255,0.38);
    text-align: center;
  }
  .link-btn {
    background: none;
    border: none;
    color: #a78bfa;
    font-size: 0.84rem;
    font-weight: 600;
    cursor: pointer;
    padding: 0;
    margin-left: 0.25rem;
    font-family: inherit;
  }
  .link-btn:hover { color: #c4b5fd; }

  @media (max-width: 800px) {
    .hero   { display: none; }
    .panel  { width: 100%; border-left: none; padding: 2.5rem 1.5rem; min-height: 100vh; }
    .panel-inner { max-width: 100%; }
  }
</style>
