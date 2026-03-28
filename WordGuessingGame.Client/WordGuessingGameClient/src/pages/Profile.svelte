<script>
  import { page, username, profilePicUrl, bannerColor, userTags, activeTag } from '../stores.js';
  import { saveAvatar, saveLanguage, saveBannerColor, setActiveTag } from '../api.js';
  import { t, locale } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  let avatarInput  = $profilePicUrl;
  let avatarSaved  = false;
  let avatarSaving = false;

  let selectedLocale  = $locale;
  let langSaved       = false;
  let langSaving      = false;

  let selectedColor  = $bannerColor;
  let colorSaved     = false;
  let colorSaving    = false;

  const PALETTE = [
    '#5b21b6', '#7c3aed', '#6d28d9',
    '#1d4ed8', '#0369a1', '#0891b2',
    '#059669', '#16a34a', '#ca8a04',
    '#ea580c', '#dc2626', '#be185d',
    '#374151', '#111827',
  ];

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

  async function doSaveBannerColor() {
    colorSaving = true;
    try {
      await saveBannerColor(selectedColor);
      colorSaved = true;
      setTimeout(() => (colorSaved = false), 2500);
    } catch { /* ignore */ } finally {
      colorSaving = false;
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

    <!-- Banner color + Tags (merged) -->
    <div class="profile-section" style="flex-direction:column;align-items:flex-start;gap:1.25rem">

      <div style="display:flex;gap:2.5rem;align-items:flex-start;flex-wrap:wrap;width:100%">

        <!-- Left: controls -->
        <div style="display:flex;flex-direction:column;gap:1.1rem;flex:1;min-width:220px">

          <!-- Color picker -->
          <div>
            <span class="input-label">{$t('profile.banner_label')}</span>
            <p style="font-size:0.78rem;color:#9ca3af;margin:0.25rem 0 0.6rem">{$t('profile.banner_pick')}</p>
            <div class="color-swatch-grid">
              {#each PALETTE as color}
                <button
                  class="color-swatch {selectedColor === color ? 'swatch-active' : ''}"
                  style="background:{color}"
                  on:click={() => (selectedColor = color)}
                  title={color}
                ></button>
              {/each}
            </div>
            <div style="margin-top:0.85rem;display:flex;align-items:center;gap:0.75rem">
              <button class="btn-sm" style="width:auto" on:click={doSaveBannerColor} disabled={colorSaving}>
                {colorSaving ? $t('profile.saving') : $t('profile.save')}
              </button>
              {#if colorSaved}<p class="success-text" style="font-size:0.85rem;margin:0">{$t('profile.banner_saved')}</p>{/if}
            </div>
          </div>

          <!-- Tag picker -->
          {#if $userTags.length > 0}
            <div>
              <span class="input-label">{$t('profile.tags_label')}</span>
              <p style="font-size:0.78rem;color:#9ca3af;margin:0.25rem 0 0.6rem">{$t('profile.tags_hint')}</p>
              <div class="profile-tag-list">
                <button
                  class="profile-tag-btn {$activeTag === null ? 'profile-tag-active' : ''}"
                  on:click={() => setActiveTag(null)}
                >{$t('profile.tag_none')}</button>
                {#each $userTags as tag}
                  <button
                    class="profile-tag-btn {$activeTag === tag ? 'profile-tag-active' : ''}"
                    on:click={() => setActiveTag(tag)}
                  >{tag}</button>
                {/each}
              </div>
            </div>
          {/if}

        </div>

        <!-- Right: preview -->
        <div style="flex-shrink:0">
          <p style="font-size:0.78rem;color:#9ca3af;margin:0 0 0.6rem">{$t('profile.banner_preview')}</p>
          <Banner
            username={$username}
            pfp={$profilePicUrl}
            color={selectedColor}
            tags={$activeTag ? [$activeTag] : []}
            isYou={true}
            size="lg"
          />
        </div>

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
