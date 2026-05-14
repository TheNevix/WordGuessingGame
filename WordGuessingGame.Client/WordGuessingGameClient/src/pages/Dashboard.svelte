<script>
  import { onMount } from 'svelte';
  import { page, username, profilePicUrl, bannerColor, userTags, activeTag, opponentLeft, rankedStats, leaderboard } from '../stores.js';
  import { handleLogout, fetchStats, fetchChallenges, claimChallenge, setActiveTag, fetchRankedStats, fetchLeaderboard } from '../api.js';
  import { connectToRanked } from '../hub.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  let dropdownOpen = false;
  let stats = { gamesPlayed: 0, wins: 0, winRate: 0, streak: 0 };
  let challenges = [];
  let claiming = null;   // challengeId currently being claimed
  let claimModal = null; // { rewardType, rewardValue, previewTags }

  const challengeIcons = {
    win_5_games:   '🏆',
    win_streak_5:  '🔥',
  };

  onMount(async () => {
    try { stats = await fetchStats(); } catch { /* keep defaults */ }
    try { challenges = await fetchChallenges(); } catch { /* keep defaults */ }
    try { const rs = await fetchRankedStats(); if (rs) rankedStats.set(rs); } catch { /* ignore */ }
    try { const lb = await fetchLeaderboard(); leaderboard.set(lb); } catch { /* ignore */ }

    if ($opponentLeft) {
      setTimeout(() => opponentLeft.set(false), 4000);
    }
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

  function toggleDropdown() { dropdownOpen = !dropdownOpen; }
  function closeDropdown()  { dropdownOpen = false; }

  function clickOutside(node) {
    const handle = (e) => { if (!node.contains(e.target)) dropdownOpen = false; };
    document.addEventListener('click', handle, true);
    return { destroy() { document.removeEventListener('click', handle, true); } };
  }
</script>

<div class="dashboard-layout">

  {#if $opponentLeft}
    <div class="toast toast-warning">{$t('game.opponent_left')}</div>
  {/if}

  <!-- Navbar -->
  <nav class="navbar">
    <span class="navbar-brand">{$t('nav.brand')}</span>
    <div class="navbar-right">
      <div class="nav-dropdown" use:clickOutside>
        <button class="user-badge" on:click={toggleDropdown}>
          <div class="avatar">
            {#if $profilePicUrl}
              <img class="avatar-img" src={$profilePicUrl} alt={$username} />
            {:else}
              {$username.charAt(0).toUpperCase()}
            {/if}
          </div>
          <span class="user-name">{$username}</span>
          <span class="dropdown-caret">{dropdownOpen ? '▴' : '▾'}</span>
        </button>

        {#if dropdownOpen}
          <div class="dropdown-menu">
            <button class="dropdown-item" on:click={() => { page.set('profile'); closeDropdown(); }}>
              {$t('nav.profile')}
            </button>
            <div class="dropdown-divider"></div>
            <button class="dropdown-item dropdown-item-danger" on:click={handleLogout}>
              {$t('nav.logout')}
            </button>
          </div>
        {/if}
      </div>
    </div>
  </nav>

  <!-- Hero banner -->
  <div class="hero-banner">
    <div class="hero-avatar-wrap">
      {#if $profilePicUrl}
        <img class="hero-avatar-img" src={$profilePicUrl} alt={$username} />
      {:else}
        <div class="hero-avatar-ring">{$username.charAt(0).toUpperCase()}</div>
      {/if}
      <div class="online-dot"></div>
    </div>
    <div class="hero-text">
      <h1 class="hero-title">{$t('dashboard.hello', { name: $username })}</h1>
      <p class="hero-subtitle">{$t('dashboard.subtitle')}</p>
      {#if $rankedStats}
        <span class="rank-badge">{$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}</span>
      {:else}
        <span class="rank-badge">{$t('dashboard.rank')}</span>
      {/if}
    </div>
    <div class="hero-stats">
      <div class="hero-stat">
        <span class="hero-stat-icon">🎮</span>
        <span class="hero-stat-value">{stats.gamesPlayed}</span>
        <span class="hero-stat-label">{$t('dashboard.stat_games')}</span>
      </div>
      <div class="hero-stat">
        <span class="hero-stat-icon">🏆</span>
        <span class="hero-stat-value">{stats.wins}</span>
        <span class="hero-stat-label">{$t('dashboard.stat_wins')}</span>
      </div>
      <div class="hero-stat">
        <span class="hero-stat-icon">📊</span>
        <span class="hero-stat-value">{stats.winRate}%</span>
        <span class="hero-stat-label">{$t('dashboard.stat_winrate')}</span>
      </div>
      <div class="hero-stat">
        <span class="hero-stat-icon">🔥</span>
        <span class="hero-stat-value">{stats.streak}</span>
        <span class="hero-stat-label">{$t('dashboard.stat_streak')}</span>
      </div>
    </div>
  </div>

  <!-- Main content -->
  <main class="dashboard-main">

    <p class="section-title">{$t('dashboard.section_play')}</p>
    <div class="play-grid">

      <div class="play-card play-card-primary" role="button" tabindex="0"
        on:click={() => page.set('lobby')}
        on:keydown={(e) => e.key === 'Enter' && page.set('lobby')}>
        <div class="play-card-watermark">⚡</div>
        <div class="play-card-icon">⚡</div>
        <p class="play-card-title">{$t('dashboard.quick_title')}</p>
        <p class="play-card-desc">{$t('dashboard.quick_desc')}</p>
        <button class="play-card-cta">{$t('dashboard.quick_btn')}</button>
      </div>

      <div class="play-card play-card-ranked" role="button" tabindex="0"
        on:click={() => page.set('ranked')}
        on:keydown={(e) => e.key === 'Enter' && page.set('ranked')}>
        <div class="play-card-watermark">⚔️</div>
        <div class="play-card-icon">⚔️</div>
        <p class="play-card-title">{$t('dashboard.ranked_title')}</p>
        <p class="play-card-desc">
          {#if $rankedStats}
            {$t('ranked.tier_label')}: <strong>{$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}</strong> · {$rankedStats.rp} RP
          {:else}
            {$t('dashboard.ranked_desc')}
          {/if}
        </p>
        <button class="play-card-cta">{$t('dashboard.ranked_btn')}</button>
      </div>

      <div class="play-card play-card-secondary" role="button" tabindex="0"
        on:click={() => page.set('private')}
        on:keydown={(e) => e.key === 'Enter' && page.set('private')}>
        <div class="play-card-watermark">🔒</div>
        <div class="play-card-icon">🔒</div>
        <p class="play-card-title">{$t('dashboard.private_title')}</p>
        <p class="play-card-desc">{$t('dashboard.private_desc')}</p>
        <button class="play-card-cta">{$t('dashboard.private_btn')}</button>
      </div>

    </div>

    {#if challenges.length > 0}
      <p class="section-title">{$t('dashboard.section_challenges')}</p>
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

    {#if $leaderboard.length > 0}
      <p class="section-title">{$t('dashboard.leaderboard_title')}</p>
      <div class="leaderboard-card">
        <div class="leaderboard-season">{$rankedStats?.seasonName ?? 'Season 1'}</div>
        {#each $leaderboard as entry}
          <div class="lb-row {entry.username === $username ? 'lb-row-me' : ''}">
            <span class="lb-rank">#{entry.rank}</span>
            <div class="lb-avatar">
              {#if entry.profilePictureUrl}
                <img src={entry.profilePictureUrl} alt={entry.username} />
              {:else}
                {entry.username.charAt(0).toUpperCase()}
              {/if}
            </div>
            <span class="lb-name">{entry.username}</span>
            <span class="lb-tier">{$t(`ranked.tier_icon.${entry.tier.toLowerCase()}`)}</span>
            <span class="lb-rp">{entry.rp} RP</span>
          </div>
        {/each}
      </div>
    {/if}
  </main>

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
        <button class="claim-equip-btn" on:click={equipReward}>
          {$t('challenge.equip_now')}
        </button>
        <button class="claim-ok-btn" on:click={() => claimModal = null}>
          {$t('challenge.ok')}
        </button>
      </div>
    </div>
  </div>
{/if}
