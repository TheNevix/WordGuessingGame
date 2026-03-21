<script>
  import { page, username, profilePicUrl } from '../stores.js';
  import { saveAvatar, saveLanguage } from '../api.js';
  import { t, locale } from '../i18n.js';

  let avatarInput  = $profilePicUrl;
  let avatarSaved  = false;
  let avatarSaving = false;

  let selectedLocale  = $locale;
  let langSaved       = false;
  let langSaving      = false;

  async function doSaveAvatar() {
    avatarSaving = true;
    try {
      await saveAvatar(avatarInput.trim());
      avatarSaved = true;
      setTimeout(() => (avatarSaved = false), 2500);
    } catch { /* ignore */ } finally {
      avatarSaving = false;
    }
  }

  async function doSaveLanguage() {
    langSaving = true;
    try {
      await saveLanguage(selectedLocale);
      langSaved = true;
      setTimeout(() => (langSaved = false), 2500);
    } catch { /* ignore */ } finally {
      langSaving = false;
    }
  }
</script>

<div class="dashboard-layout">

  <nav class="navbar">
    <span class="navbar-brand">{$t('nav.brand')}</span>
    <div class="navbar-right">
      <button class="btn-navbar btn-sm" on:click={() => page.set('dashboard')}>{$t('profile.back')}</button>
    </div>
  </nav>

  <main class="dashboard-main">
    <p class="section-title">{$t('profile.title')}</p>

    <!-- Avatar -->
    <div class="profile-section">
      <div class="profile-pfp-preview">
        {#if $profilePicUrl}
          <img src={$profilePicUrl} alt={$username} />
        {:else}
          {$username.charAt(0).toUpperCase()}
        {/if}
      </div>
      <div class="profile-form">
        <span class="input-label">{$t('profile.avatar_label')}</span>
        <div class="profile-form-row">
          <input type="url" bind:value={avatarInput} placeholder={$t('profile.avatar_placeholder')} />
          <button on:click={doSaveAvatar} disabled={avatarSaving}>
            {avatarSaving ? $t('profile.saving') : $t('profile.save')}
          </button>
        </div>
        {#if avatarSaved}<p class="success-text" style="font-size:0.85rem">{$t('profile.saved')}</p>{/if}
      </div>
    </div>

    <!-- Language -->
    <div class="profile-section">
      <div class="profile-form" style="width:100%">
        <span class="input-label">{$t('profile.language_label')}</span>
        <div class="profile-form-row">
          <select bind:value={selectedLocale}>
            <option value="en">{$t('profile.language_en')}</option>
            <option value="nl">{$t('profile.language_nl')}</option>
          </select>
          <button on:click={doSaveLanguage} disabled={langSaving}>
            {langSaving ? $t('profile.language_saving') : $t('profile.save')}
          </button>
        </div>
        {#if langSaved}<p class="success-text" style="font-size:0.85rem">{$t('profile.language_saved')}</p>{/if}
      </div>
    </div>

  </main>
</div>
