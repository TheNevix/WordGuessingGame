<script>
  import { goto } from '$app/navigation';
  import { username, isWaiting } from '$lib/stores.js';
  import { connectToHub } from '$lib/hub.js';
  import { t } from '$lib/i18n.js';

  async function generate() {
    await connectToHub('create-private', null, $username);
    goto('/wachten');
  }
</script>

<div class="auth-container">
  <div class="card">
    <p class="title">{$t('private_lobby.title')}</p>
    <p class="subtitle">{$t('private_lobby.subtitle')}</p>
    <button on:click={generate}>
      {$t('private_lobby.generate')}
    </button>
    <button class="btn-ghost btn-sm" on:click={() => goto('/home')}>{$t('private_lobby.back')}</button>
  </div>
</div>
