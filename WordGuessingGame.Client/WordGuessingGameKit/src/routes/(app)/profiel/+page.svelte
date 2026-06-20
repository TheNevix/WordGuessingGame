<script>
  import { goto } from '$app/navigation';
  import { username, profilePicUrl, bannerColor, userTags, activeTag } from '$lib/stores.js';
  import { saveAvatar, saveLanguage, saveBannerColor, setActiveTag } from '$lib/api.js';
  import { t, locale } from '$lib/i18n.js';
  import Banner from '$lib/components/Banner.svelte';

  let avatarInput  = $profilePicUrl;
  let avatarSaved  = false;
  let avatarSaving = false;

  let selectedLocale  = $locale;
  let langSaved       = false;
  let langSaving      = false;

  let selectedColor  = $bannerColor;

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

  async function pickColor(color) {
    selectedColor = color;
    await saveBannerColor(color);
  }
</script>

<div class="tab-screen">

  <div class="tab-header">
    <h1 class="tab-header-title">{$t('profile.title')}</h1>
    <button class="btn-navbar btn-sm" on:click={() => goto('/home')}>{$t('profile.back')}</button>
  </div>

  <div class="tab-content">

    <!-- Avatar -->
    <div class="pcard">
      <div class="pcard-row">
        <div class="pfp-circle">
          {#if $profilePicUrl}
            <img src={$profilePicUrl} alt={$username} />
          {:else}
            {$username.charAt(0).toUpperCase()}
          {/if}
        </div>
        <div class="pcard-fields">
          <span class="plabel">{$t('profile.avatar_label')}</span>
          <div class="pinput-row">
            <input class="pinput" type="url" bind:value={avatarInput} placeholder={$t('profile.avatar_placeholder')} />
            <button class="pbtn" on:click={doSaveAvatar} disabled={avatarSaving}>
              {avatarSaving ? $t('profile.saving') : $t('profile.save')}
            </button>
          </div>
          {#if avatarSaved}<p class="psuccess">{$t('profile.saved')}</p>{/if}
        </div>
      </div>
    </div>

    <!-- Banner color + Tags -->
    <div class="pcard">
      <div class="pcard-two-col">

        <div class="pcard-col">
          <span class="plabel">{$t('profile.banner_label')}</span>
          <p class="phint">{$t('profile.banner_pick')}</p>
          <div class="color-swatch-grid">
            {#each PALETTE as color}
              <button
                class="color-swatch {selectedColor === color ? 'swatch-active' : ''}"
                style="background:{color}"
                on:click={() => pickColor(color)}
                title={color}
              ></button>
            {/each}
          </div>

          {#if $userTags.length > 0}
            <span class="plabel" style="margin-top:1.25rem">{$t('profile.tags_label')}</span>
            <p class="phint">{$t('profile.tags_hint')}</p>
            <div class="profile-tag-list">
              <button
                class="ptag-btn {$activeTag === null ? 'ptag-active' : ''}"
                on:click={() => setActiveTag(null)}
              >{$t('profile.tag_none')}</button>
              {#each $userTags as tag}
                <button
                  class="ptag-btn {$activeTag === tag.name ? 'ptag-active' : ''}"
                  on:click={() => setActiveTag(tag.name)}
                >{tag.name}</button>
              {/each}
            </div>
          {/if}
        </div>

        <div class="pcard-preview">
          <p class="phint">{$t('profile.banner_preview')}</p>
          <Banner
            username={$username}
            pfp={$profilePicUrl}
            color={selectedColor}
            tags={$activeTag ? [$userTags.find(t => t.name === $activeTag) ?? {name: $activeTag}] : []}
            isYou={true}
            size="lg"
          />
        </div>

      </div>
    </div>

    <!-- Language -->
    <div class="pcard">
      <span class="plabel">{$t('profile.language_label')}</span>
      <div class="pinput-row" style="margin-top:0.6rem">
        <select class="pinput" bind:value={selectedLocale}>
          <option value="en">{$t('profile.language_en')}</option>
          <option value="nl">{$t('profile.language_nl')}</option>
        </select>
        <button class="pbtn" on:click={doSaveLanguage} disabled={langSaving}>
          {langSaving ? $t('profile.language_saving') : $t('profile.save')}
        </button>
      </div>
      {#if langSaved}<p class="psuccess">{$t('profile.language_saved')}</p>{/if}
    </div>

  </div>
</div>

<style>
  .pcard {
    background: rgba(255,255,255,0.06);
    border: 1px solid rgba(255,255,255,0.09);
    border-radius: 18px;
    padding: 1.5rem;
    margin-bottom: 1rem;
  }

  .pcard-row {
    display: flex;
    align-items: flex-start;
    gap: 1.25rem;
    flex-wrap: wrap;
  }

  .pcard-fields {
    flex: 1;
    min-width: 200px;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .pcard-two-col {
    display: flex;
    gap: 2.5rem;
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .pcard-col {
    flex: 1;
    min-width: 220px;
    display: flex;
    flex-direction: column;
  }

  .pcard-preview {
    flex-shrink: 0;
  }

  .pfp-circle {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    background: rgba(124,58,237,0.3);
    border: 2px solid rgba(167,139,250,0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.5rem;
    font-weight: 700;
    color: #e9d5ff;
    flex-shrink: 0;
    overflow: hidden;
  }

  .pfp-circle img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .plabel {
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: rgba(255,255,255,0.45);
    display: block;
  }

  .phint {
    font-size: 0.78rem;
    color: rgba(255,255,255,0.3);
    margin: 0.25rem 0 0.6rem;
  }

  .pinput-row {
    display: flex;
    gap: 0.5rem;
    align-items: center;
  }

  .pinput {
    flex: 1;
    width: auto;
    background: rgba(255,255,255,0.07);
    border: 1px solid rgba(255,255,255,0.12);
    border-radius: 10px;
    padding: 0.75rem 1rem;
    color: #f1f5f9;
    font-size: 0.95rem;
    outline: none;
    transition: border-color 0.2s;
    box-sizing: border-box;
  }

  .pinput:focus {
    border-color: rgba(167,139,250,0.6);
  }

  .pinput::placeholder {
    color: rgba(255,255,255,0.25);
  }

  select.pinput option {
    background: #1a0535;
    color: #f1f5f9;
  }

  .pbtn {
    width: auto;
    padding: 0.75rem 1.25rem;
    flex-shrink: 0;
    background: #7c3aed;
    border: none;
    border-radius: 10px;
    color: white;
    font-weight: 700;
    font-size: 0.9rem;
    cursor: pointer;
    transition: background 0.15s;
  }

  .pbtn:hover   { background: #6d28d9; opacity: 1; }
  .pbtn:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }

  .psuccess {
    color: #4ade80;
    font-size: 0.85rem;
    font-weight: 600;
    margin: 0.25rem 0 0;
  }

  .ptag-btn {
    background: rgba(255,255,255,0.07);
    color: rgba(255,255,255,0.7);
    border: 1px solid rgba(255,255,255,0.12);
    border-radius: 999px;
    padding: 0.35rem 0.85rem;
    font-size: 0.82rem;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;
    width: auto;
    transition: background 0.15s, border-color 0.15s;
  }

  .ptag-btn:hover {
    background: rgba(255,255,255,0.12);
    border-color: rgba(255,255,255,0.2);
    opacity: 1;
  }

  .ptag-btn.ptag-active {
    background: #7c3aed;
    color: white;
    border-color: #7c3aed;
  }

  @media (max-width: 600px) {
    .pcard-two-col { flex-direction: column; }
    .pinput-row    { flex-direction: column; }
    .pinput-row :global(.pinput),
    .pinput-row .pbtn { width: 100%; }
  }
</style>
