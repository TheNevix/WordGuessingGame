<script>
  import { page, username, isGuest, inviteCode, privateLobbyError } from '../stores.js';
  import { connectToHub } from '../hub.js';
  import { t } from '../i18n.js';

  let guestName = "";

  function doJoin() {
    const name = $isGuest ? guestName.trim() : $username;
    connectToHub('join-private', $inviteCode, name);
  }

  function clearAuth() {
    localStorage.removeItem("token");
    localStorage.removeItem("username");
    localStorage.removeItem("refreshToken");
    username.set("");
    isGuest.set(false);
  }
</script>

<div class="auth-container">
  <div class="card">
    <p class="title">{$t('join.title')}</p>
    <p class="subtitle">{$t('join.subtitle')}</p>

    {#if $username && !$isGuest}
      <div class="user-badge">
        <div class="avatar">{$username.charAt(0).toUpperCase()}</div>
        <span>{$username}</span>
      </div>
      {#if $privateLobbyError}<p class="error-text">{$privateLobbyError}</p>{/if}
      <button on:click={doJoin}>{$t('join.join_btn')}</button>
      <div class="divider">{$t('join.or')}</div>
      <button class="btn-ghost" on:click={clearAuth}>{$t('join.diff_account')}</button>

    {:else if $isGuest}
      <p class="subtitle">{$t('join.guest_subtitle')}</p>
      <div class="field-wrapper">
        <div class="input-group">
          <span class="input-label">{$t('join.display_name')}</span>
          <input type="text" bind:value={guestName} placeholder={$t('join.name_placeholder')}
            on:keydown={(e) => e.key === 'Enter' && doJoin()} />
        </div>
      </div>
      {#if $privateLobbyError}<p class="error-text">{$privateLobbyError}</p>{/if}
      <button on:click={doJoin}>{$t('join.join_btn')}</button>
      <button class="btn-ghost btn-sm" on:click={() => isGuest.set(false)}>{$t('join.back')}</button>

    {:else}
      <button on:click={() => page.set('login')}>{$t('join.login')}</button>
      <button on:click={() => page.set('register')}>{$t('join.register')}</button>
      <div class="divider">{$t('join.or')}</div>
      <button class="btn-ghost" on:click={() => isGuest.set(true)}>{$t('join.guest')}</button>
    {/if}
  </div>
</div>
