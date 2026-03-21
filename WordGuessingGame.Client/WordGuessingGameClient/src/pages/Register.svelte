<script>
  import { page, inviteCode } from '../stores.js';
  import { handleRegister } from '../api.js';
  import { t } from '../i18n.js';

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
        page.set($inviteCode ? "join-private" : "login");
      }, 1500);
    } catch (e) {
      regError = e.message;
    } finally {
      regLoading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="card">
    <p class="title">{$t('register.title')}</p>
    <p class="subtitle">{$t('register.subtitle')}</p>

    <div class="field-wrapper">
      <div class="input-group">
        <span class="input-label">{$t('register.username')}</span>
        <input type="text" bind:value={regUsername} placeholder={$t('register.username_placeholder')} />
      </div>
      <div class="input-group">
        <span class="input-label">{$t('register.email')}</span>
        <input type="email" bind:value={regEmail} placeholder={$t('register.email_placeholder')} />
      </div>
      <div class="input-group">
        <span class="input-label">{$t('register.password')}</span>
        <input type="password" bind:value={regPassword} placeholder={$t('register.password_placeholder')} />
      </div>
      <div class="input-group">
        <span class="input-label">{$t('register.confirm_password')}</span>
        <input type="password" bind:value={regConfirm} placeholder={$t('register.confirm_placeholder')}
          on:keydown={(e) => e.key === 'Enter' && doRegister()} />
      </div>

      {#if regError}<p class="error-text">{regError}</p>{/if}
      {#if regSuccess}<p class="success-text">{$t('register.success')}</p>{/if}

      <button on:click={doRegister} disabled={regLoading}>
        {regLoading ? $t('register.btn_loading') : $t('register.btn')}
      </button>
    </div>

    <div class="link-row">
      {$t('register.has_account')}
      <button class="link-btn" on:click={() => page.set('login')}>{$t('register.login')}</button>
    </div>
  </div>
</div>
