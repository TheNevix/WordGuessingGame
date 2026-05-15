<script>
  import { onMount } from 'svelte';
  import { username, profilePicUrl, bannerColor, activeTag, userTags, rankedStats } from '../stores.js';
  import { fetchChallenges, claimChallenge, setActiveTag, fetchRankedStats } from '../api.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  let challenges = [];
  let claiming = null;
  let claimModal = null;

  const challengeIcons = { win_5_games: '🏆', win_streak_5: '🔥' };

  onMount(async () => {
    try { challenges = await fetchChallenges(); } catch { /* ignore */ }
    try { const rs = await fetchRankedStats(); if (rs) rankedStats.set(rs); } catch { /* ignore */ }
  });

  async function handleClaim(challenge) {
    claiming = challenge.challengeId;
    try {
      const result = await claimChallenge(challenge.challengeId);
      challenges = challenges.map(c =>
        c.challengeId === challenge.challengeId ? { ...c, isClaimed: true } : c
      );
      if (result.tags) {
        userTags.set(result.tags);
        localStorage.setItem("userTags", JSON.stringify(result.tags));
      }
      const previewTags = result.rewardType === 'Tag' ? [result.rewardValue] : ($activeTag ? [$activeTag] : []);
      claimModal = { rewardType: result.rewardType, rewardValue: result.rewardValue, previewTags };
    } catch { /* silent */ } finally {
      claiming = null;
    }
  }

  function equipReward() {
    if (claimModal?.rewardType === 'BannerColor') {
      bannerColor.set(claimModal.rewardValue);
      localStorage.setItem("bannerColor", claimModal.rewardValue);
    } else if (claimModal?.rewardType === 'Tag') {
      setActiveTag(claimModal.rewardValue);
    }
    claimModal = null;
  }

  $: claimable = challenges.filter(c => c.isCompleted && !c.isClaimed).length;
</script>

<div class="tab-screen">

  <div class="tab-header">
    <h1 class="tab-header-title">{$t('nav.challenges_tab')}</h1>
    {#if claimable > 0}
      <span class="tab-header-badge">{claimable} {$t('challenges.claimable')}</span>
    {/if}
  </div>

  <div class="tab-content">
    {#if challenges.length === 0}
      <p class="tab-empty">{$t('challenges.empty')}</p>
    {:else}
      <div class="challenge-grid">
        {#each challenges as c}
          <div class="challenge-card {c.isCompleted && !c.isClaimed ? 'challenge-ready' : ''} {c.isClaimed ? 'challenge-claimed' : ''}">
            <div class="challenge-header">
              <span class="challenge-icon">{challengeIcons[c.key] ?? '🎯'}</span>
              <div class="challenge-title-wrap">
                <span class="challenge-name">{$t(`challenge.${c.key}.name`)}</span>
                <span class="challenge-desc">{$t(`challenge.${c.key}.desc`)}</span>
              </div>
              {#if c.isClaimed}
                <span class="challenge-badge challenge-badge-claimed">{$t('challenge.claimed')}</span>
              {:else if c.isCompleted}
                <span class="challenge-badge challenge-badge-done">{$t('challenge.completed')}</span>
              {/if}
            </div>
            <div class="challenge-progress-bar">
              <div class="challenge-progress-fill" style="width: {Math.min(100, (c.progress / c.target) * 100)}%"></div>
            </div>
            <div class="challenge-footer">
              <span class="challenge-progress-text">{c.progress} / {c.target}</span>
              {#if c.isCompleted && !c.isClaimed}
                <button class="challenge-claim-btn" on:click={() => handleClaim(c)} disabled={claiming === c.challengeId}>
                  {claiming === c.challengeId ? $t('dashboard.challenge_claiming') : $t('dashboard.challenge_claim')}
                </button>
              {/if}
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>

</div>

{#if claimModal}
  <!-- svelte-ignore a11y-click-events-have-key-events a11y-no-static-element-interactions -->
  <div class="claim-overlay" on:click={() => claimModal = null}>
    <!-- svelte-ignore a11y-click-events-have-key-events a11y-no-static-element-interactions -->
    <div class="claim-modal" on:click|stopPropagation>
      <div class="claim-sparkle">✨</div>
      <h2 class="claim-modal-title">{$t('challenge.modal_title')}</h2>
      <p class="claim-modal-desc">
        {#if claimModal.rewardType === 'Tag'}
          {$t('challenge.modal_tag_desc', { tag: claimModal.rewardValue })}
        {:else}
          {$t('challenge.modal_color_desc')}
        {/if}
      </p>
      <div class="claim-banner-preview">
        <Banner
          username={$username}
          pfp={$profilePicUrl}
          color={claimModal.rewardType === 'BannerColor' ? claimModal.rewardValue : $bannerColor}
          tags={claimModal.previewTags}
          size="md"
        />
      </div>
      <div class="claim-actions">
        <button class="claim-equip-btn" on:click={equipReward}>{$t('challenge.equip_now')}</button>
        <button class="claim-ok-btn" on:click={() => claimModal = null}>{$t('challenge.ok')}</button>
      </div>
    </div>
  </div>
{/if}
