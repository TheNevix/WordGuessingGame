<script>
  import { goto } from '$app/navigation';
  import { handleRegister } from '$lib/api.js';
  import { t } from '$lib/i18n.js';

  let regUsername = "";
  let regEmail    = "";
  let regPassword = "";
  let regConfirm  = "";
  let regError    = "";
  let regSuccess  = false;
  let regLoading  = false;

  async function doRegister() {
    if (!regUsername.trim() || !regEmail.trim() || !regPassword.trim()) {
      regError = $t('register.error_fields');
      return;
    }
    if (regPassword !== regConfirm) {
      regError = $t('register.error_passwords');
      return;
    }
    regError = "";
    regLoading = true;
    try {
      await handleRegister(regUsername, regEmail, regPassword);
      regSuccess = true;
      setTimeout(() => {
        regSuccess = false;
        regUsername = regEmail = regPassword = regConfirm = "";
        const inviteCode = new URLSearchParams(window.location.search).get('invite') ?? '';
        goto(inviteCode ? '/join' : '/inloggen');
      }, 1500);
    } catch (e) {
      regError = e.message;
    } finally {
      regLoading = false;
    }
  }

  const tiers = [
    { name: 'Kampioen', icon: '👑', color: '#f59e0b', rp: '2000+',     top: true  },
    { name: 'Diamant',  icon: '💠', color: '#818cf8', rp: '1500–1999'              },
    { name: 'Platina',  icon: '💎', color: '#67e8f9', rp: '1000–1499'              },
    { name: 'Goud',     icon: '🥇', color: '#f59e0b', rp: '500–999'                },
    { name: 'Zilver',   icon: '🥈', color: '#9ca3af', rp: '200–499'                },
    { name: 'Brons',    icon: '🥉', color: '#cd7f32', rp: '0–199',      bottom: true },
  ];

  const perks = [
    'Gratis te spelen, altijd',
    'Stijg van Brons naar Kampioen',
    'Verdien exclusieve seizoenstitels',
  ];
</script>

<div class="split">

  <!-- ── Left hero ── -->
  <div class="hero">
    <span class="fl" style="left:5%;top:9%;font-size:9rem;animation-duration:20s">K</span>
    <span class="fl" style="left:76%;top:5%;font-size:7rem;animation-duration:16s;animation-delay:5s">W</span>
    <span class="fl" style="left:55%;top:68%;font-size:12rem;animation-duration:24s;animation-delay:9s">R</span>
    <span class="fl" style="left:20%;top:76%;font-size:6rem;animation-duration:17s;animation-delay:3s">A</span>
    <span class="fl" style="left:87%;top:44%;font-size:8rem;animation-duration:22s;animation-delay:7s">D</span>

    <div class="hero-body">

      <div>
        <div class="brand-name">Raad het woord</div>
        <p class="brand-tag">Word de beste speler van België.</p>
      </div>

      <div class="ladder">
        {#each tiers as tier, i}
          <div class="ladder-row {tier.top ? 'tier-top' : ''} {tier.bottom ? 'tier-bot' : ''}"
               style="animation-delay:{i * 0.09}s">
            <span class="tier-icon {tier.top ? 'icon-glow' : ''}">{tier.icon}</span>
            <div class="tier-info">
              <span class="tier-name" style="color:{tier.color}">{tier.name}</span>
              <span class="tier-rp">{tier.rp} RP</span>
            </div>
            {#if tier.top}
              <span class="badge-top">← word dit</span>
            {/if}
            {#if tier.bottom}
              <span class="badge-bot">← start hier</span>
            {/if}
          </div>
        {/each}
      </div>

      <div class="perks">
        {#each perks as perk}
          <div class="perk">
            <span class="perk-check">✓</span>
            <span class="perk-text">{perk}</span>
          </div>
        {/each}
      </div>

    </div>
  </div>

  <!-- ── Right form panel ── -->
  <div class="panel">
    <div class="panel-inner">

      <h1 class="form-title">{$t('register.title')}</h1>
      <p class="form-sub">{$t('register.subtitle')}</p>

      <div class="fields">
        <div class="field">
          <label class="flabel">{$t('register.username')}</label>
          <input class="finput" type="text" bind:value={regUsername}
            placeholder={$t('register.username_placeholder')} />
        </div>
        <div class="field">
          <label class="flabel">{$t('register.email')}</label>
          <input class="finput" type="email" bind:value={regEmail}
            placeholder={$t('register.email_placeholder')} />
        </div>
        <div class="field">
          <label class="flabel">{$t('register.password')}</label>
          <input class="finput" type="password" bind:value={regPassword}
            placeholder={$t('register.password_placeholder')} />
        </div>
        <div class="field">
          <label class="flabel">{$t('register.confirm_password')}</label>
          <input class="finput" type="password" bind:value={regConfirm}
            placeholder={$t('register.confirm_placeholder')}
            on:keydown={(e) => e.key === 'Enter' && doRegister()} />
        </div>
      </div>

      {#if regError}<p class="form-error">{regError}</p>{/if}
      {#if regSuccess}<p class="form-success">Verificatiemail verstuurd! Controleer je inbox.</p>{/if}

      <button class="btn-primary" on:click={doRegister} disabled={regLoading}>
        {regLoading ? $t('register.btn_loading') : $t('register.btn')}
      </button>

      <p class="switch-row">
        {$t('register.has_account')}
        <button class="link-btn" on:click={() => goto('/inloggen')}>{$t('register.login')} →</button>
      </p>

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

  /* Tier ladder */
  .ladder {
    background: rgba(255,255,255,0.03);
    border: 1px solid rgba(255,255,255,0.08);
    border-radius: 16px;
    overflow: hidden;
  }
  .ladder-row {
    display: flex;
    align-items: center;
    gap: 0.8rem;
    padding: 0.7rem 1.2rem;
    border-bottom: 1px solid rgba(255,255,255,0.05);
    animation: slideIn 0.4s ease-out both;
    transition: background 0.15s;
  }
  .ladder-row:last-child { border-bottom: none; }
  .ladder-row:hover { background: rgba(255,255,255,0.03); }

  .tier-top {
    background: rgba(245,158,11,0.07);
    border-bottom-color: rgba(245,158,11,0.12) !important;
  }

  @keyframes slideIn {
    from { transform: translateX(-14px); opacity: 0; }
    to   { transform: translateX(0); opacity: 1; }
  }

  .tier-icon { font-size: 1.25rem; }
  .icon-glow { filter: drop-shadow(0 0 8px rgba(245,158,11,0.8)); }

  .tier-info { flex: 1; display: flex; flex-direction: column; gap: 0.1rem; }
  .tier-name { font-size: 0.88rem; font-weight: 700; }
  .tier-rp   { font-size: 0.68rem; color: rgba(255,255,255,0.3); }

  .badge-top {
    font-size: 0.66rem;
    font-weight: 700;
    color: #f59e0b;
    background: rgba(245,158,11,0.14);
    border-radius: 999px;
    padding: 0.2rem 0.65rem;
    animation: glow 2s ease-in-out infinite alternate;
    white-space: nowrap;
  }
  @keyframes glow {
    from { box-shadow: 0 0 4px rgba(245,158,11,0.2); }
    to   { box-shadow: 0 0 14px rgba(245,158,11,0.55); }
  }
  .badge-bot {
    font-size: 0.66rem;
    font-weight: 600;
    color: rgba(255,255,255,0.35);
    background: rgba(255,255,255,0.06);
    border-radius: 999px;
    padding: 0.2rem 0.65rem;
    white-space: nowrap;
  }

  /* Perks */
  .perks { display: flex; flex-direction: column; gap: 0.6rem; }
  .perk  { display: flex; align-items: center; gap: 0.6rem; }
  .perk-check { color: #4ade80; font-weight: 700; font-size: 1rem; }
  .perk-text  { font-size: 0.87rem; color: rgba(255,255,255,0.6); }

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

  .form-error {
    font-size: 0.84rem;
    color: #f87171;
    margin: 0.8rem 0 0;
  }
  .form-success {
    font-size: 0.84rem;
    color: #4ade80;
    font-weight: 600;
    margin: 0.8rem 0 0;
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
