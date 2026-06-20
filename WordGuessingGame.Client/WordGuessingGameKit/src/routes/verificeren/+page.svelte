<script>
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';

  let state = 'loading'; // loading | success | error

  onMount(async () => {
    const token = new URLSearchParams(window.location.search).get('token');
    if (!token) { state = 'error'; return; }

    try {
      const res = await fetch(`/api/auth/verify-email?token=${encodeURIComponent(token)}`);
      state = res.ok ? 'success' : 'error';
      if (res.ok) setTimeout(() => goto('/inloggen?verified=1'), 2500);
    } catch {
      state = 'error';
    }
  });
</script>

<div class="page">
  <div class="card">

    {#if state === 'loading'}
      <div class="spinner"></div>
      <p class="title">Bevestigen...</p>
      <p class="sub">Even geduld.</p>

    {:else if state === 'success'}
      <div class="icon success-icon">✓</div>
      <p class="title">E-mailadres bevestigd!</p>
      <p class="sub">Je wordt doorgestuurd naar de inlogpagina.</p>

    {:else}
      <div class="icon error-icon">✕</div>
      <p class="title">Link ongeldig of verlopen</p>
      <p class="sub">Registreer opnieuw of neem contact op.</p>
      <button class="btn" on:click={() => goto('/registreren')}>Opnieuw registreren</button>
    {/if}

  </div>
</div>

<style>
  .page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #0d0820;
  }

  .card {
    background: #100d26;
    border: 1px solid rgba(255,255,255,0.07);
    border-radius: 16px;
    padding: 3rem 3.5rem;
    text-align: center;
    max-width: 360px;
    width: 100%;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
  }

  .spinner {
    width: 40px;
    height: 40px;
    border: 3px solid rgba(124,58,237,0.2);
    border-top-color: #7c3aed;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
    margin-bottom: 0.5rem;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  .icon {
    width: 52px;
    height: 52px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.4rem;
    font-weight: 800;
    margin-bottom: 0.5rem;
  }
  .success-icon { background: rgba(74,222,128,0.12); color: #4ade80; }
  .error-icon   { background: rgba(248,113,113,0.12); color: #f87171; }

  .title {
    font-size: 1.2rem;
    font-weight: 800;
    color: #f1f5f9;
    margin: 0.25rem 0 0;
  }

  .sub {
    font-size: 0.85rem;
    color: rgba(255,255,255,0.4);
    margin: 0;
  }

  .btn {
    margin-top: 1rem;
    padding: 0.75rem 1.75rem;
    background: #7c3aed;
    border: none;
    border-radius: 10px;
    color: #fff;
    font-size: 0.9rem;
    font-weight: 700;
    cursor: pointer;
    font-family: inherit;
  }
  .btn:hover { background: #6d28d9; }
</style>
