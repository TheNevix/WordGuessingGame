<script>
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { username, profilePicUrl, bannerColor, activeTag, userTags, privateLobbyLink, linkExpiresIn, isWaiting } from '$lib/stores.js';
  import { cancelWaiting } from '$lib/hub.js';
  import { formatCountdown } from '$lib/stores.js';
  import { t } from '$lib/i18n.js';
  import Banner from '$lib/components/Banner.svelte';

  let linkCopied = false;

  onMount(() => {
    // Redirect only if we're not actually in queue and no lobby link arrives.
    // Use a longer window so slow connections aren't kicked prematurely.
    const timeout = setTimeout(() => {
      if (!$isWaiting && !$privateLobbyLink) goto('/home');
    }, 8000);
    return () => clearTimeout(timeout);
  });

  function copyLink() {
    navigator.clipboard.writeText($privateLobbyLink);
    linkCopied = true;
    setTimeout(() => (linkCopied = false), 2000);
  }
</script>

<div class="auth-container">
  <div class="card">

    <div class="waiting-banner-wrap">
      <Banner
        username={$username}
        pfp={$profilePicUrl}
        color={$bannerColor}
        tags={$activeTag ? [$userTags.find(t => t.name === $activeTag) ?? {name: $activeTag}] : []}
        isYou={true}
        size="md"
      />
    </div>

    {#if $privateLobbyLink}
      <p class="status-text">{$t('waiting.waiting')}</p>
      <p class="subtitle">{$t('waiting.playing_as', { name: $username })}</p>
      <p class="input-label" style="text-align:center">{$t('waiting.share_link')}</p>
      <div class="invite-link-row">
        <span class="invite-link-text">{$privateLobbyLink}</span>
        <button class="btn-sm btn-icon" on:click={copyLink}>
          {linkCopied ? $t('waiting.copied') : $t('waiting.copy')}
        </button>
      </div>
      {#if $linkExpiresIn > 0}
        <p class="subtitle" style="color: {$linkExpiresIn < 60 ? '#dc2626' : '#9ca3af'}">
          {$t('waiting.expires', { time: formatCountdown($linkExpiresIn) })}
        </p>
      {:else}
        <p class="error-text">{$t('waiting.expired')}</p>
      {/if}
    {:else}
      <p class="status-text">{$t('waiting.looking')}</p>
      <p class="subtitle">{$t('waiting.playing_as', { name: $username })}</p>
    {/if}

    <button class="btn-ghost btn-sm" on:click={cancelWaiting}>{$t('waiting.cancel')}</button>
  </div>
</div>
