<script>
  import { page, username } from '../stores.js';
  import { connectToHub } from '../hub.js';
  import { t } from '../i18n.js';
</script>

<div class="auth-container">
  <div class="card">
    <p class="title">{$t('private_lobby.title')}</p>
    <p class="subtitle">{$t('private_lobby.subtitle')}</p>
    <button on:click={() => connectToHub('create-private', null, $username)}>
      {$t('private_lobby.generate')}
    </button>
    <button class="btn-ghost btn-sm" on:click={() => page.set('dashboard')}>{$t('private_lobby.back')}</button>
  </div>
</div>
