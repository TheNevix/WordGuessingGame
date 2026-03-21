<script>
  import { page, username, isGuest, inviteCode } from '../stores.js';
  import { handleLogin } from '../api.js';
  import { t } from '../i18n.js';

  let loginUsername = "";
  let loginPassword = "";
  let loginError    = "";
  let loginLoading  = false;
  let rememberMe    = false;

  async function doLogin() {
    if (!loginUsername.trim() || !loginPassword.trim()) {
      loginError = $t('login.error_fields');
      return;
    }
    loginError = "";
    loginLoading = true;
    try {
      await handleLogin(loginUsername, loginPassword, rememberMe);
    } catch (e) {
      loginError = e.message;
    } finally {
      loginLoading = false;
    }
  }

  function goGuest() {
    isGuest.set(true);
    username.set("");
    page.set("lobby");
  }
</script>

<div class="auth-container">
  <div class="card">
    <p class="title">{$t('login.title')}</p>
    <p class="subtitle">{$t('login.subtitle')}</p>

    <div class="field-wrapper">
      <div class="input-group">
        <span class="input-label">{$t('login.username')}</span>
        <input type="text" bind:value={loginUsername} placeholder={$t('login.username_placeholder')} />
      </div>
      <div class="input-group">
        <span class="input-label">{$t('login.password')}</span>
        <input type="password" bind:value={loginPassword} placeholder={$t('login.password_placeholder')}
          on:keydown={(e) => e.key === 'Enter' && doLogin()} />
      </div>

      <label class="remember-row">
        <input type="checkbox" bind:checked={rememberMe} />
        {$t('login.remember_me')}
      </label>

      {#if loginError}<p class="error-text">{loginError}</p>{/if}

      <button on:click={doLogin} disabled={loginLoading}>
        {loginLoading ? $t('login.btn_loading') : $t('login.btn')}
      </button>
    </div>

    <div class="divider">or</div>
    <button class="btn-ghost" on:click={goGuest}>{$t('login.guest')}</button>

    <div class="link-row">
      {$t('login.no_account')}
      <button class="link-btn" on:click={() => page.set('register')}>{$t('login.register')}</button>
    </div>
  </div>
</div>
