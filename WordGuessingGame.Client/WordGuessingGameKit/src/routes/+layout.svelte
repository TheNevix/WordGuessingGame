<script>
  import { onMount } from 'svelte';
  import { matchFoundToast, opponentLeft, hydrateFromStorage } from '$lib/stores.js';
  import { tryRefreshToken } from '$lib/api.js';
  import { initHub } from '$lib/hub.js';
  import { t } from '$lib/i18n.js';
  import '../app.css';
  import '../play-screen.css';
  import '../ranked-lobby.css';
  import '../countdown.css';

  const version = "v1.5.1";

  onMount(async () => {
    hydrateFromStorage();
    await tryRefreshToken();
    initHub();
  });

  $: if ($opponentLeft) setTimeout(() => opponentLeft.set(false), 4000);
</script>

<slot />

<div class="version-label">{version}</div>

{#if $opponentLeft}
  <div class="toast toast-warning">{$t('game.opponent_left')}</div>
{/if}

{#if $matchFoundToast}
  <div class="match-found-toast">⚔️ {$t('ranked.opponent_found')}</div>
{/if}
