<script>
  import { onMount } from 'svelte';
  import { page, username, profilePicUrl, activeTag, rankedStats } from '../stores.js';
  import { fetchRankedStats } from '../api.js';
  import { t } from '../i18n.js';

  const modes = [
    {
      id: 'ranked',
      icon: '⚔️',
      nameKey: 'dashboard.ranked_title',
      descKey: 'dashboard.ranked_desc',
      bg: 'linear-gradient(160deg, #1a0535 0%, #2d0f5e 50%, #0d0820 100%)',
      accent: '#f59e0b',
      route: 'ranked',
    },
    {
      id: 'quick',
      icon: '⚡',
      nameKey: 'dashboard.quick_title',
      descKey: 'dashboard.quick_desc',
      bg: 'linear-gradient(160deg, #051628 0%, #0c2a4a 50%, #020d1a 100%)',
      accent: '#06b6d4',
      route: 'lobby',
    },
    {
      id: 'private',
      icon: '🔒',
      nameKey: 'dashboard.private_title',
      descKey: 'dashboard.private_desc',
      bg: 'linear-gradient(160deg, #0e0a30 0%, #1a1060 50%, #080520 100%)',
      accent: '#818cf8',
      route: 'private',
    },
  ];

  let current = 0;
  let touchStartX = 0;
  let touchStartY = 0;

  onMount(async () => {
    try { const rs = await fetchRankedStats(); if (rs) rankedStats.set(rs); } catch {}
  });

  function navigate(index) { if (index !== current) current = index; }
  function next() { navigate((current + 1) % modes.length); }
  function prev() { navigate((current - 1 + modes.length) % modes.length); }

  function onTouchStart(e) {
    touchStartX = e.touches[0].clientX;
    touchStartY = e.touches[0].clientY;
  }
  function onTouchEnd(e) {
    const dx = touchStartX - e.changedTouches[0].clientX;
    const dy = touchStartY - e.changedTouches[0].clientY;
    if (Math.abs(dx) > Math.abs(dy) && Math.abs(dx) > 40) dx > 0 ? next() : prev();
  }
</script>

<div class="play-screen" on:touchstart={onTouchStart} on:touchend={onTouchEnd}>

  <!-- Background layers -->
  {#each modes as m, i}
    <div class="mode-bg" class:mode-bg-active={i === current} style="background: {m.bg}"></div>
  {/each}

  <!-- Top strip -->
  <div class="play-header">
    <div class="play-user">
      <div class="play-avatar-wrap">
        {#if $profilePicUrl}
          <img class="play-avatar-img" src={$profilePicUrl} alt={$username} />
        {:else}
          <div class="play-avatar-initials">{$username.charAt(0).toUpperCase()}</div>
        {/if}
        <div class="play-online-dot"></div>
      </div>
      <div class="play-user-text">
        <span class="play-username">{$username}</span>
        {#if $activeTag}<span class="play-tag-pill">{$activeTag}</span>{/if}
      </div>
    </div>
    {#if $rankedStats}
      <div class="play-rank-pill">
        <span>{$t(`ranked.tier_icon.${$rankedStats.tier.toLowerCase()}`)}</span>
        <span class="pill-tier-name">{$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}</span>
        <span class="pill-sep">·</span>
        <span class="pill-rp">{$rankedStats.rp} RP</span>
      </div>
    {/if}
  </div>

  <!-- Desktop arrows -->
  <button class="mode-arrow-btn mode-arrow-prev" on:click={prev}>‹</button>
  <button class="mode-arrow-btn mode-arrow-next" on:click={next}>›</button>

  <!-- Carousel track -->
  <div class="mode-track">
    {#each modes as m, i}
      {@const offset = i - current}
      <div
        class="mode-slide"
        class:mode-slide-active={i === current}
        style="
          transform: translateX({offset * 100}%) rotateY({offset * 12}deg) scale({i === current ? 1 : 0.82});
          opacity: {i === current ? 1 : 0};
          filter: blur({i === current ? 0 : 20}px);
        "
      >
        <div class="mode-big-icon" style="filter: drop-shadow(0 0 48px {m.accent}99)">{m.icon}</div>
        <h2 class="mode-big-name">{$t(m.nameKey)}</h2>
        {#if m.id === 'ranked' && $rankedStats}
          <p class="mode-ranked-badge" style="color:{m.accent}; border-color:{m.accent}44; background:{m.accent}11">
            {$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)} · {$rankedStats.rp} RP
          </p>
        {/if}
        <p class="mode-big-desc">{$t(m.descKey)}</p>
        <button
          class="mode-play-btn"
          style="background:{m.accent}; box-shadow: 0 0 48px {m.accent}55"
          on:click={() => page.set(m.route)}
        >{$t('play.play_btn')}</button>
      </div>
    {/each}
  </div>

  <!-- Dots -->
  <div class="mode-dots">
    {#each modes as m, i}
      <button
        class="mode-dot"
        class:mode-dot-active={i === current}
        style="--dot-accent: {m.accent}"
        on:click={() => navigate(i)}
      ></button>
    {/each}
  </div>

</div>
